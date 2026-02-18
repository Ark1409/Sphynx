// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Microsoft.Win32.SafeHandles;
using Sphynx.Storage;
using Sphynx.Utils;

namespace Sphynx.Client.Tui
{
    // NOTE: You could even just cache some properties so that flushing will no longer be required when fetching them.
    public class HandleWindowsTerminal : WindowsTerminal
    {
        protected internal SafeFileHandle Input { get; }
        protected internal SafeFileHandle Output { get; }

        private readonly TerminalColorSupport _colorSupportCache;
        public override TerminalColorSupport ColorSupport => _colorSupportCache;

        private readonly bool _hasMouseSupportCache;
        public override bool HasMouseSupport => _hasMouseSupportCache;

        private readonly IHandleWindowsTerminalStrategy _strat;

        public sealed override int Lines
        {
            get
            {
                Flush();
                WindowsTerminalInterop.CheckWin32Return(WindowsTerminalInterop.GetConsoleScreenBufferInfo(Output.DangerousGetHandle(), out var info));
                return info.dwSize.Y;
            }
        }

        public sealed override int Columns
        {
            get
            {
                Flush();
                WindowsTerminalInterop.CheckWin32Return(WindowsTerminalInterop.GetConsoleScreenBufferInfo(Output.DangerousGetHandle(), out var info));
                return info.dwSize.X;
            }
        }

        public sealed override (int x, int y) CursorPosition
        {
            get
            {
                Flush();
                WindowsTerminalInterop.CheckWin32Return(WindowsTerminalInterop.GetConsoleScreenBufferInfo(Output.DangerousGetHandle(), out var info));
                return (info.dwCursorPosition.X, info.dwCursorPosition.Y);
            }
            set => _strat.CursorPosition = value;
        }

        // Caller must make sure stream is same as output
        public HandleWindowsTerminal(SafeFileHandle input, SafeFileHandle output)
        {
            Input = input;
            Output = output;

            if (!WindowsTerminalInterop.SetupConsoleBase(Input.DangerousGetHandle(), Output.DangerousGetHandle()))
            {
                throw new Win32Exception("Failed to setup input and output on HandleWindowsTerminal");
            }

            // TODO: For now, window size polling from console is the only thing we require.
            // Maybe later add ability to switch to polling method if can't get window rezizes from console
            if (!WindowsTerminalInterop.AddWindowInput(Input.DangerousGetHandle()))
            {
                throw new Win32Exception("Failed to setup window input on HandleWindowsTerminal");
            }

            if (WindowsTerminalInterop.AddAnsiEscapeOutput(Output.DangerousGetHandle()))
            {
                _colorSupportCache = TerminalColorSupport.TrueColor | TerminalColorSupport.Ansi8 | TerminalColorSupport.Ansi16 | TerminalColorSupport.XTerm256;
                _strat = new StreamHandleWindowsTerminalStrategy(this);
            }
            else
            {
                _colorSupportCache = TerminalColorSupport.Ansi8 | TerminalColorSupport.Ansi16;
                _strat = null; // TODO: Implement
            }

            _hasMouseSupportCache = WindowsTerminalInterop.AddMouseInput(Input.DangerousGetHandle());
            _oldTermSize = (Lines, Columns);
        }

        // TODO: Check if this is even possible on Windows...
        public override ITerminalColor CursorColor { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override TerminalCursorShape CursorShape { set => _strat.CursorShape = value; }
        public override TerminalCursorVisibility CursorVisibility { set => _strat.CursorVisibility = value; }

        public sealed override TerminalTrueColor TrueColorFor(TerminalAnsiColor color)
        {
            if (color.Color >= TerminalAnsiColor.AnsiColors.Color16) return base.TrueColorFor(color);

            Flush();
            var ret = WindowsTerminalInterop.GetConsoleScreenBufferInfoEx(Output.DangerousGetHandle(), out var info);
            if (ret == 0) return base.TrueColorFor(color);

            var colors = WindowsTerminalInterop.GetRGB(info.ColorTable[(int)color.Color]);
            return new(colors.R, colors.G, colors.B);
        }

        public sealed override TerminalAnsiColor NearestAnsiColor(TerminalTrueColor color)
        {
            Flush();
            var ret = WindowsTerminalInterop.GetConsoleScreenBufferInfoEx(Output.DangerousGetHandle(), out var info);
            if (ret == 0) return base.NearestAnsiColor(color);

            return color.NearestAnsiColorFrom(info.ColorTable.Select((e, i) =>
            {
                var colors = WindowsTerminalInterop.GetRGB(e);
                return ((TerminalAnsiColor.AnsiColors)i, new TerminalTrueColor(colors.R, colors.G, colors.B));
            }).ToArray());
        }

        public sealed override void Write(ColoredString str) => _strat.Write(str);
        public sealed override void Write(IEnumerable<ColoredString> strs) => _strat.Write(strs);

        public sealed override void Erase(int count = 1) => _strat.Erase(count);

        public sealed override void ClearLine() => _strat.ClearLine();

        public sealed override void Clear() => _strat.Clear();

        public sealed override void MoveCursor(int dx, int dy)
        {
            if (_strat is null)
            {
                base.MoveCursor(dx, dy);
                return;
            }

            _strat.MoveCursor(dx, dy);
        }

        public override void Flush()
        {
            _strat?.Flush();
        }

        private Queue<TerminalEvent> _pendingEvents = new();
        public override TerminalEvent PollEvent()
        {
            if (_pendingEvents.Count > 0)
            {
                return _pendingEvents.Dequeue();
            }

            var rec = ReadInputRecord();
            if (rec is null) return new();
            var realEv = ConsumeEvent(rec.Value);
            return realEv ?? default;
        }

        private WindowsTerminalInterop.INPUT_RECORD? ReadInputRecord()
        {
            const int READ_COUNT = 1;
            using var evs = ArrayPool<WindowsTerminalInterop.INPUT_RECORD>.Shared.AutoRent(READ_COUNT);
            var ret = WindowsTerminalInterop.ReadConsoleInputExW(Input.DangerousGetHandle(), evs, READ_COUNT, out var readCount, 0);
            if (ret == 0) return null;
            if (readCount <= 0) return null;
            if (readCount != READ_COUNT) throw new Exception($"Error while reading from HandleWindowsTerminal: Expected {READ_COUNT} input record read(s), got {readCount}");
            return evs[0];
        }

        private TerminalKeyModifiers _currentMods = TerminalKeyModifiers.None;
        private (int, int)? _oldTermSize;
        private (int, int)? _oldMousePos;

        private char? _highPair;
        private TerminalKeyModifiers _highPairMods = TerminalKeyModifiers.None;
        private TerminalEvent? ConsumeEvent(in WindowsTerminalInterop.INPUT_RECORD ev)
        {
            switch (ev.EventType)
            {
                case WindowsTerminalInterop.InputEventType.KEY_EVENT:
                {
                    var kv = ev.Event.KeyEvent;
                    ConsumeMods(kv.dwControlKeyState);

                    TerminalKeyEvent realEv = default;

                    TerminalKey.SpecialKey? special = null;
                    switch (kv.wVirtualKeyCode)
                    {
                        case 0x5B:
                        case 0x5C:
                            if (kv.bKeyDown != 0)
                                _currentMods |= TerminalKeyModifiers.Super;
                            else
                                _currentMods &= ~TerminalKeyModifiers.Super;
                            return PollEvent();
                        case >= 0x70 and <= 0x87:
                            special = TerminalKey.SpecialKey.F1 + (kv.wVirtualKeyCode - 0x70);
                            break;
                        case 0x23:
                            special = TerminalKey.SpecialKey.End;
                            break;
                        case 0x24:
                            special = TerminalKey.SpecialKey.Home;
                            break;
                        case 0x25:
                            special = TerminalKey.SpecialKey.Left;
                            break;
                        case 0x26:
                            special = TerminalKey.SpecialKey.Up;
                            break;
                        case 0x27:
                            special = TerminalKey.SpecialKey.Right;
                            break;
                        case 0x28:
                            special = TerminalKey.SpecialKey.Down;
                            break;
                        case 0x2D:
                            special = TerminalKey.SpecialKey.Insert;
                            break;
                        case 0x2C:
                            special = TerminalKey.SpecialKey.PrintScreen;
                            break;
                        case 0x21:
                            special = TerminalKey.SpecialKey.PageUp;
                            break;
                        case 0x22:
                            special = TerminalKey.SpecialKey.PageDown;
                            break;
                        default: break;
                    }

                    if (special is not null)
                    {
                        realEv = new TerminalKeyEvent(this, new TerminalKey(special.Value, _currentMods));
                    }
                    else
                    {
                        int ch = 0x0;
                        var cachedMods = _currentMods;
                        if (char.IsHighSurrogate(kv.uChar.UnicodeChar))
                        {
                            var searchingPair = true;
                            while (searchingPair)
                            {
                                var newRec = ReadInputRecord();
                                if (newRec is null) return default;
                                switch (newRec.Value.EventType)
                                {
                                    case WindowsTerminalInterop.InputEventType.KEY_EVENT:
                                    {
                                        var secondPair = newRec.Value.Event.KeyEvent.uChar.UnicodeChar;
                                        if (!char.IsLowSurrogate(secondPair))
                                        {
                                            goto default;
                                        }
                                        ch = (((kv.uChar.UnicodeChar & 0x3FF) << 10) | (secondPair & 0x3FF)) + 0x10000;
                                        searchingPair = false;
                                        break;
                                    }
                                    default:
                                        var cev = ConsumeEvent(newRec.Value);
                                        if (cev is not null)
                                        {
                                            _highPair = kv.uChar.UnicodeChar;
                                            _highPairMods = cachedMods;
                                            return cev;
                                        }
                                        break;
                                }
                            }
                        }
                        else if (char.IsLowSurrogate(kv.uChar.UnicodeChar))
                        {
                            if (_highPair is null)
                                return default;

                            ch = (((_highPair.Value & 0x3FF) << 10) | (kv.uChar.UnicodeChar & 0x3FF)) + 0x10000;
                            cachedMods = _highPairMods;
                            _highPairMods = TerminalKeyModifiers.None;
                            _highPair = null;
                        }
                        else
                        {
                            ch = kv.uChar.UnicodeChar;
                        }
                        realEv = new TerminalKeyEvent(this, new TerminalKey(ch, cachedMods));
                    }

                    for (uint i = 0; i < Math.Max(kv.wRepeatCount - 1, 0u); i++)
                    {
                        _pendingEvents.Enqueue(realEv);
                    }
                    return realEv;
                }
                case WindowsTerminalInterop.InputEventType.WINDOW_BUFFER_SIZE_EVENT:
                {
                    var wv = ev.Event.WindowBufferSizeEvent;
                    var wret = new TerminalWindowEvent(this, _oldTermSize ??= (wv.dwSize.Y, wv.dwSize.X), (wv.dwSize.Y, wv.dwSize.X));
                    _oldTermSize = (wv.dwSize.Y, wv.dwSize.X);
                    return wret;
                }
                case WindowsTerminalInterop.InputEventType.MOUSE_EVENT:
                {
                    const int MOUSE_MOVED = 0x0001;
                    const int DOUBLE_CLICK = 0x0002;
                    const int MOUSE_WHEELED = 0x0004;
                    const int MOUSE_HWHEELED = 0x0008;

                    const int FROM_LEFT_1ST_BUTTON_PRESSED = 0x0001;
                    const int FROM_LEFT_2ND_BUTTON_PRESSED = 0x0004;
                    const int FROM_LEFT_3RD_BUTTON_PRESSED = 0x0008;
                    const int FROM_LEFT_4TH_BUTTON_PRESSED = 0x0010;
                    const int RIGHTMOST_BUTTON_PRESSED = 0x0002;

                    var mv = ev.Event.MouseEvent;
                    var newPos = (mv.dwMousePosition.X, mv.dwMousePosition.Y);
                    ConsumeMods(mv.dwControlKeyState);
                    _oldMousePos ??= newPos;

                    var eventList = new List<TerminalEvent>(4);
                    if ((mv.dwEventFlags & MOUSE_MOVED) == MOUSE_MOVED)
                    {
                        eventList.Add(new TerminalMouseMoveEvent(this, _oldMousePos.Value, newPos, _currentMods));
                    }

                    if ((mv.dwEventFlags & MOUSE_WHEELED) == MOUSE_WHEELED)
                    {
                        eventList.Add(new TerminalMouseScrollEvent(this, newPos, mv.dwButtonState.HighUShort(), TerminalScrollDirection.Vertical, _currentMods));
                    }
                    else if ((mv.dwEventFlags & MOUSE_HWHEELED) == MOUSE_HWHEELED)
                    {
                        eventList.Add(new TerminalMouseScrollEvent(this, newPos, mv.dwButtonState.HighUShort(), TerminalScrollDirection.Horizontal, _currentMods));
                    }

                    if ((mv.dwEventFlags & DOUBLE_CLICK) == DOUBLE_CLICK || mv.dwEventFlags == 0)
                    {
                        var buttons = TerminalMouseButtons.None;
                        if ((mv.dwButtonState & FROM_LEFT_1ST_BUTTON_PRESSED) == FROM_LEFT_1ST_BUTTON_PRESSED)
                        {
                            buttons |= TerminalMouseButtons.MouseLeft;
                        }
                        if ((mv.dwButtonState & FROM_LEFT_2ND_BUTTON_PRESSED) == FROM_LEFT_2ND_BUTTON_PRESSED)
                        {
                            buttons |= TerminalMouseButtons.MouseMiddle;
                        }
                        if ((mv.dwButtonState & RIGHTMOST_BUTTON_PRESSED) == RIGHTMOST_BUTTON_PRESSED)
                        {
                            buttons |= TerminalMouseButtons.MouseRight;
                        }
                        if ((mv.dwButtonState & FROM_LEFT_3RD_BUTTON_PRESSED) == FROM_LEFT_3RD_BUTTON_PRESSED)
                        {
                            buttons |= TerminalMouseButtons.Mouse4;
                        }
                        if ((mv.dwButtonState & FROM_LEFT_4TH_BUTTON_PRESSED) == FROM_LEFT_4TH_BUTTON_PRESSED)
                        {
                            buttons |= TerminalMouseButtons.Mouse5;
                        }
                        if (buttons != TerminalMouseButtons.None)
                        {
                            int clickCount = (mv.dwEventFlags & DOUBLE_CLICK) == DOUBLE_CLICK ? 2 : 1;
                            eventList.Add(new TerminalMouseClickEvent(this, newPos, buttons, _currentMods, clickCount));
                        }
                    }

                    _oldMousePos = newPos;
                    Debug.Assert(eventList.Count > 0);
                    for (int i = 1; i < eventList.Count; i++)
                        _pendingEvents.Enqueue(eventList[i]);
                    return eventList[0];
                }
                default:
                    return default;
            }
        }

        private void ConsumeMods(uint dwControlKeyState)
        {
            const int CAPSLOCK_ON = 0x0080;
            const int ENHANCED_KEY = 0x0100;
            const int LEFT_ALT_PRESSED = 0x0002;
            const int LEFT_CTRL_PRESSED = 0x0008;
            const int NUMLOCK_ON = 0x0020;
            const int RIGHT_ALT_PRESSED = 0x0001;
            const int RIGHT_CTRL_PRESSED = 0x0004;
            const int SCROLLLOCK_ON = 0x0040;
            const int SHIFT_PRESSED = 0x0010;

            var mods = TerminalKeyModifiers.None;

            if ((dwControlKeyState & LEFT_CTRL_PRESSED) == LEFT_CTRL_PRESSED || (dwControlKeyState & RIGHT_CTRL_PRESSED) == RIGHT_CTRL_PRESSED)
            {
                mods |= TerminalKeyModifiers.Control;
            }

            if ((dwControlKeyState & LEFT_ALT_PRESSED) == LEFT_ALT_PRESSED || (dwControlKeyState & RIGHT_ALT_PRESSED) == RIGHT_ALT_PRESSED)
            {
                mods |= TerminalKeyModifiers.Alt;
            }

            if ((dwControlKeyState & SHIFT_PRESSED) == SHIFT_PRESSED)
            {
                mods |= TerminalKeyModifiers.Shift;
            }

            _currentMods &= TerminalKeyModifiers.Super;
            _currentMods |= mods;
        }

        protected internal override bool CanPollEvent(Type t)
        {
            Debug.Assert(t.IsAssignableTo(typeof(ITerminalEvent)));

            if (t == typeof(TerminalKeyEvent))
            {
                return true;
            }
            else if (t == typeof(TerminalMouseMoveEvent) || t == typeof(TerminalMouseClickEvent) || t == typeof(TerminalMouseScrollEvent))
            {
                return HasMouseSupport;
            }
            else if (t == typeof(TerminalWindowEvent))
            {
                // TODO: For now, window size polling from console is the only thing we require.
                // Change later along with code in constructor when alternative implemented.
                return true;
            }

            return false;
        }
    }

    internal interface IHandleWindowsTerminalStrategy
    {
        void Write(ColoredString str) => Write(str.Yield());
        void Write(IEnumerable<ColoredString> s);
        (int x, int y) CursorPosition { set; }
        TerminalCursorShape CursorShape { set; }
        TerminalCursorVisibility CursorVisibility { set; }
        void Clear();
        void ClearLine();
        void Erase(int count = 1);
        void MoveCursor(int dx, int dy);
        void Flush();
    }

    internal sealed class StreamHandleWindowsTerminalStrategy : IHandleWindowsTerminalStrategy
    {
        private readonly Encoding _outputEncoding;
        private readonly Stream _outputStream;
        private readonly Terminal _term;

        private readonly StringBuilder _escapeBuilder = new(32);

        public StreamHandleWindowsTerminalStrategy(HandleWindowsTerminal terminal) : this(terminal, Encoding.UTF8) { }
        public StreamHandleWindowsTerminalStrategy(HandleWindowsTerminal terminal, Encoding encoding)
        {
            _term = terminal;
            _outputStream = new FileStream(terminal.Output, FileAccess.Write);
            _outputEncoding = encoding;
        }

        public (int x, int y) CursorPosition
        {
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value.x, nameof(value.x));
                ArgumentOutOfRangeException.ThrowIfNegative(value.y, nameof(value.y));

                ArgumentOutOfRangeException.ThrowIfGreaterThan(value.x, 32767, nameof(value.x));
                ArgumentOutOfRangeException.ThrowIfGreaterThan(value.y, 32767, nameof(value.y));

                // 1-based row / column; but still allow 0 as 1
                _escapeBuilder.Clear();
                _escapeBuilder.Append("\x1b[");
                if (value.y != 1 && value.y != 0) _escapeBuilder.Append(value.y);
                _escapeBuilder.Append(';');
                if (value.x != 1 && value.x != 0) _escapeBuilder.Append(value.x);
                _escapeBuilder.Append('H');
                WriteOutput(_escapeBuilder.ToString());
            }
        }

        public TerminalCursorShape CursorShape
        {
            set
            {
                switch (value)
                {
                    case TerminalCursorShape.DefaultShape:
                        WriteOutput("\x1b[0 q");
                        break;
                    case TerminalCursorShape.BlinkingBlock:
                        WriteOutput("\x1b[1 q");
                        break;
                    case TerminalCursorShape.SteadyBlock:
                        WriteOutput("\x1b[2 q");
                        break;
                    case TerminalCursorShape.BlinkingUnderline:
                        WriteOutput("\x1b[3 q");
                        break;
                    case TerminalCursorShape.SteadyUnderline:
                        WriteOutput("\x1b[4 q");
                        break;
                    case TerminalCursorShape.BlinkingBar:
                        WriteOutput("\x1b[5 q");
                        break;
                    case TerminalCursorShape.SteadyBar:
                        WriteOutput("\x1b[6 q");
                        break;
                    default:
                        throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(TerminalCursorShape));
                }
            }
        }

        public TerminalCursorVisibility CursorVisibility
        {
            set
            {
                switch (value)
                {
                    case TerminalCursorVisibility.Invisible:
                        WriteOutput("\x1b[?25l");
                        break;
                    case TerminalCursorVisibility.Visible:
                        WriteOutput("\x1b[?25h");
                        break;
                    default:
                        throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(TerminalCursorVisibility));
                }
            }
        }

        public void Clear() => WriteOutput("\x1b[2J");
        public void ClearLine() => WriteOutput("\x1b[2K");

        public void Erase(int count = 1) => WriteOutput($"\x1b[{count}X");

        public void MoveCursor(int dx, int dy)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(dx, 32767, nameof(dx));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(dy, 32767, nameof(dy));

            ArgumentOutOfRangeException.ThrowIfLessThan(dx, -32767, nameof(dx));
            ArgumentOutOfRangeException.ThrowIfLessThan(dy, -32767, nameof(dy));

            _escapeBuilder.Clear();

            switch (dx)
            {
                case > 0:
                    if (dx == 1) _escapeBuilder.Append("\x1b[C");
                    else _escapeBuilder.Append($"\x1b[{dx}C");
                    break;
                case < 0:
                    _escapeBuilder.Append($"\x1b[{-dx}D");
                    break;
                case 0:
                    break;
            }

            switch (dy)
            {
                case > 0:
                    if (dy == 1) _escapeBuilder.Append("\x1b[B");
                    else _escapeBuilder.Append($"\x1b[{dy}B");
                    break;
                case < 0:
                    _escapeBuilder.Append($"\x1b[{-dy}A");
                    break;
                case 0:
                    break;
            }

            WriteOutput(_escapeBuilder.ToString());
        }

        public void Write(ColoredString str)
        {
            WriteOutput(GetEscapeCodeFor(str.Color));
            WriteOutput(str.Text);
            WriteOutput("\x1b[0;39;49m");
        }

        public void Write(params ColoredString[] strings) => Write((IEnumerable<ColoredString>)strings);

        public void Write(IEnumerable<ColoredString> strings)
        {
            var currentColor = TerminalCellColor.Default;

            foreach (var str in strings)
            {
                WriteOutput(GetEscapeCodeFor(str.Color, currentColor: currentColor));
                WriteOutput(str.Text);
                currentColor = str.Color;
            }

            WriteOutput("\x1b[0;39;49m");
        }

        private string GetEscapeCodeFor(TerminalCellColor cellColor, TerminalCellColor? currentColor)
        {
            _escapeBuilder.Clear();
            _escapeBuilder.Append("\x1b[");

            if ((cellColor.Attributes & TerminalCellColor.CellAttributes.Bold) == TerminalCellColor.CellAttributes.Bold
                    && (currentColor is null || (currentColor.Value.Attributes & TerminalCellColor.CellAttributes.Bold) != TerminalCellColor.CellAttributes.Bold))
            {
                _escapeBuilder.Append("1;");
            }

            if ((cellColor.Attributes & TerminalCellColor.CellAttributes.Underline) == TerminalCellColor.CellAttributes.Underline
                    && (currentColor is null || (currentColor.Value.Attributes & TerminalCellColor.CellAttributes.Underline) != TerminalCellColor.CellAttributes.Underline))
            {
                _escapeBuilder.Append("4;");
            }

            if ((cellColor.Attributes & TerminalCellColor.CellAttributes.Reverse) == TerminalCellColor.CellAttributes.Reverse
                    && (currentColor is null || ((currentColor.Value.Attributes & TerminalCellColor.CellAttributes.Reverse) != TerminalCellColor.CellAttributes.Reverse)))
            {
                _escapeBuilder.Append("7;");
            }

            if (!_term.HasTrueColor && cellColor.Foreground is TerminalTrueColor fgtc)
            {
                cellColor = cellColor with
                {
                    Foreground = _term.NearestAnsiColor(fgtc),
                };
            }

            if (!_term.HasTrueColor && cellColor.Background is TerminalTrueColor bgtc)
            {
                cellColor = cellColor with
                {
                    Background = _term.NearestAnsiColor(bgtc),
                };
            }

            switch (cellColor.Foreground)
            {
                case TerminalAnsiColor ac:
                    if (currentColor is not null && currentColor.Value.Foreground is TerminalAnsiColor cac && cac.Equals(ac))
                        break;
                    int val = (int)ac.Color;
                    switch (val)
                    {
                        case < 8:
                        {
                            int v = 30 + val;
                            _escapeBuilder.Append(v).Append(';');
                            break;
                        }
                        case < 16:
                        {
                            int v = 90 + val - 8;
                            _escapeBuilder.Append(v).Append(';');
                            break;
                        }
                        case <= 255:
                        {
                            _escapeBuilder.Append("38;5;").Append(val).Append(';');
                            break;
                        }
                    }
                    break;
                case TerminalTrueColor tc:
                    if (currentColor is not null && currentColor.Value.Foreground is TerminalTrueColor ctc && ctc.Equals(tc))
                        break;
                    _escapeBuilder.Append("38;2;").Append(tc.R).Append(';').Append(tc.G).Append(';').Append(tc.B).Append(';');
                    break;
                case ITerminalColor.TerminalDefaultColor:
                    if (currentColor is not null && currentColor.Value.Foreground is ITerminalColor.TerminalDefaultColor)
                        break;
                    _escapeBuilder.Append("39;");
                    break;
                default:
                    throw new ArgumentException("Unsupported terminal color type");
            }

            switch (cellColor.Background)
            {
                case TerminalAnsiColor ac:
                    if (currentColor is not null && currentColor.Value.Background is TerminalAnsiColor cac && cac.Equals(ac))
                        break;
                    int val = (int)ac.Color;
                    switch (val)
                    {
                        case < 8:
                        {
                            int v = 40 + val;
                            _escapeBuilder.Append(v).Append(';');
                            break;
                        }
                        case < 16:
                        {
                            int v = 100 + val - 8;
                            _escapeBuilder.Append(v).Append(';');
                            break;
                        }
                        case <= 255:
                        {
                            _escapeBuilder.Append("48;5;").Append(val).Append(';');
                            break;
                        }
                    }
                    break;
                case TerminalTrueColor tc:
                    if (currentColor is not null && currentColor.Value.Background is TerminalTrueColor ctc && ctc.Equals(tc))
                        break;
                    _escapeBuilder.Append("48;2;").Append(tc.R).Append(';').Append(tc.G).Append(';').Append(tc.B).Append(';');
                    break;
                case ITerminalColor.TerminalDefaultColor:
                    if (currentColor is not null && currentColor.Value.Background is ITerminalColor.TerminalDefaultColor)
                        break;
                    _escapeBuilder.Append("49;");
                    break;
                default:
                    throw new ArgumentException("Unsupported terminal color type");
            }

            Debug.Assert(_escapeBuilder.Length >= 2);
            if (_escapeBuilder[^1] == ';') _ = _escapeBuilder.Remove(_escapeBuilder.Length - 1, 1);
            if (_escapeBuilder.Length == 2)
            {
                Debug.Assert(_escapeBuilder.ToString() == "\x1b[");
                return string.Empty;
            }
            return _escapeBuilder.Append('m').ToString();
        }

        private string GetEscapeCodeFor(TerminalCellColor cellColor) => GetEscapeCodeFor(cellColor, TerminalCellColor.Default);

        private void WriteOutput(string s)
        {
            if (s.Length <= 0) return;
            byte[] bytes = _outputEncoding.GetBytes(s);
            if (bytes.Length <= 0) return;
            _outputStream.Write(bytes);
        }

        public void Flush()
        {
            _outputStream.Flush();
        }
    }

    internal sealed class StandardWindowsTerminal : HandleWindowsTerminal
    {
        internal StandardWindowsTerminal() : base(new SafeFileHandle(WindowsTerminalInterop.StdIn, false), new SafeFileHandle(WindowsTerminalInterop.StdOut, false))
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;
        }
    }
}
