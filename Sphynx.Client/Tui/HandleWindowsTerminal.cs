// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using Sphynx.Client.Utils;
using Sphynx.Utils;
using DWORD = uint;

namespace Sphynx.Client.Tui
{
    // NOTE: You could even just cache some properties so that flushing will no longer be required when fetching them.
    public class HandleWindowsTerminal : WindowsTerminal
    {
        protected internal SafeFileHandle Input { get; }
        protected internal SafeFileHandle Output { get; }
        protected internal Encoding OutputEncoding { get; }

        private readonly SafeWaitHandle _waitHandle;
        private readonly AutoResetEvent _readEvent;
        private TerminalColorSupport _colorSupportCache;
        public override TerminalColorSupport ColorSupport => _colorSupportCache;

        private bool _hasMouseSupportCache;
        public override bool HasMouseSupport => _hasMouseSupportCache;

        private IHandleWindowsTerminalStrategy? _strat;

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

        public sealed override (int X, int Y) CursorPosition
        {
            get
            {
                Flush();
                WindowsTerminalInterop.CheckWin32Return(WindowsTerminalInterop.GetConsoleScreenBufferInfo(Output.DangerousGetHandle(), out var info));
                return (info.dwCursorPosition.X, info.dwCursorPosition.Y);
            }
            set => EnsureStratInit().CursorPosition = value;
        }

        // TODO: Check if this is even possible on Windows...
        public override ITerminalColor CursorColor { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override TerminalCursorShape CursorShape { set => EnsureStratInit().CursorShape = value; }
        public override TerminalCursorVisibility CursorVisibility { set => EnsureStratInit().CursorVisibility = value; }

        public HandleWindowsTerminal(SafeFileHandle input, SafeFileHandle output) : this(input, output, Encodings.UTF8)
        {
        }

        // Caller must make sure stream is same as output
        public HandleWindowsTerminal(SafeFileHandle input, SafeFileHandle output, Encoding outputEncoding)
        {
            Input = input;
            Output = output;
            OutputEncoding = outputEncoding;
            _waitHandle = new SafeWaitHandle(input.DangerousGetHandle(), false);
            _readEvent = new AutoResetEvent(false) { SafeWaitHandle = _waitHandle };
        }

        private DWORD? _oldInputConsoleMode, _oldOutputConsoleMode;
        public override void Init()
        {
            base.Init();

            WindowsTerminalInterop.CheckWin32Return(WindowsTerminalInterop.GetConsoleMode(Input.DangerousGetHandle(), out var currentInputMode));
            WindowsTerminalInterop.CheckWin32Return(WindowsTerminalInterop.GetConsoleMode(Output.DangerousGetHandle(), out var currentOutputMode));

            _oldInputConsoleMode = currentInputMode;
            _oldOutputConsoleMode = currentOutputMode;

            if (!WindowsTerminalInterop.SetupConsoleBase(Input.DangerousGetHandle(), Output.DangerousGetHandle()))
            {
                throw new Win32Exception("Failed to setup input and output on HandleWindowsTerminal");
            }

            // FIXME: For now, window size polling from console is the only thing we require.
            // Maybe later add ability to switch to polling method if can't get window rezizes from console
            if (!WindowsTerminalInterop.AddWindowInput(Input.DangerousGetHandle()))
            {
                throw new Win32Exception("Failed to setup window input on HandleWindowsTerminal");
            }

            _hasMouseSupportCache = WindowsTerminalInterop.AddMouseInput(Input.DangerousGetHandle());

            EnsureStratInit().Init();

            _oldTermSize = (Lines, Columns);
        }

        public override ValueTask DisposeAsync()
        {
            _strat?.Dispose();
            _strat = null;

            if (_oldInputConsoleMode is not null)
            {
                WindowsTerminalInterop.CheckWin32Return(WindowsTerminalInterop.SetConsoleMode(Input.DangerousGetHandle(), _oldInputConsoleMode.Value));
                _oldInputConsoleMode = null;
            }

            if (_oldOutputConsoleMode is not null)
            {
                WindowsTerminalInterop.CheckWin32Return(WindowsTerminalInterop.SetConsoleMode(Output.DangerousGetHandle(), _oldOutputConsoleMode.Value));
                _oldOutputConsoleMode = null;
            }

            return base.DisposeAsync();
        }

        private uint[]? _colorTable;
        private void LoadColorTable()
        {
            if (_colorTable is not null) return;

            var ret = WindowsTerminalInterop.GetConsoleScreenBufferInfoEx(Output.DangerousGetHandle(), out var info);
            if (ret == 0) return;
            unsafe
            {
                var sp = new Span<uint>(info.ColorTable, WindowsTerminalInterop.CONSOLE_SCREEN_BUFFER_INFOEX.ColorTableLength);
                _colorTable = sp.ToArray();
            }
        }
        public sealed override TerminalTrueColor TrueColorFor(TerminalAnsiColor color)
        {
            if (color.Color >= TerminalAnsiColor.AnsiColors.Color16) return base.TrueColorFor(color);

            Flush();
            LoadColorTable();

            if (_colorTable is null) return base.TrueColorFor(color);

            var colors = WindowsTerminalInterop.GetRGB(_colorTable[(int)color.Color]);
            return new(colors.R, colors.G, colors.B);
        }

        public sealed override TerminalAnsiColor NearestAnsiColor(TerminalTrueColor color)
        {
            Flush();
            LoadColorTable();

            if (_colorTable is null) return base.NearestAnsiColor(color);

            return color.NearestAnsiColorFrom(_colorTable.Select((e, i) =>
                            {
                                var colors = WindowsTerminalInterop.GetRGB(e);
                                return ((TerminalAnsiColor.AnsiColors)i, new TerminalTrueColor(colors.R, colors.G, colors.B));
                            }).ToArray());
        }

        public sealed override void Write(ColoredString str) => EnsureStratInit().Write(str);
        public sealed override void Write(IEnumerable<ColoredString> strs) => EnsureStratInit().Write(strs);

        public sealed override void Erase(int count = 1) => EnsureStratInit().Erase(count);

        public sealed override void ClearLine() => EnsureStratInit().ClearLine();

        public sealed override void Clear() => EnsureStratInit().Clear();

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

        private readonly Queue<TerminalEvent> _pendingEvents = new();
        public override TerminalEvent PollEvent(TimeSpan timeout)
        {
            if (_pendingEvents.TryDequeue(out var item)) return item;

            WindowsTerminalInterop.INPUT_RECORD? rec = null;
            do
            {
                rec = ReadInputRecord(ref timeout);
            } while (rec is { } rc
                    && (rc.EventType & (WindowsTerminalInterop.InputEventType.FOCUS_EVENT | WindowsTerminalInterop.InputEventType.MENU_EVENT)) != 0);

            if (rec is null)
            {
                if (_graphemeCharStorage.Count > 0)
                {
                    var graphemeStorageSp = CollectionsMarshal.AsSpan(_graphemeCharStorage);
                    var graphemeLen = StringInfo.GetNextTextElementLength(graphemeStorageSp);
                    if (graphemeLen < graphemeStorageSp.Length || IsReaderEof)
                    {
                        AddGraphemeEvent(graphemeLen);
                        return PollEvent(timeout);
                    }
                    if (timeout == TimeSpan.Zero)
                    {
                        ReadOnlySpan<char> sp = graphemeStorageSp[..graphemeLen];
                        sp.ToCodePoints(_graphemeCodePoints);

                        var graphemeCodePointsSp = CollectionsMarshal.AsSpan(_graphemeCodePoints);
                        bool incomplete = GraphemeUtils.IsIncomplete(graphemeCodePointsSp, true);
                        _graphemeCodePoints.Clear();
                        if (!incomplete)
                        {
                            AddGraphemeEvent(graphemeLen);
                            return PollEvent(timeout);
                        }
                    }
                }
                if (IsReaderEof) throw new EndOfStreamException("Reached end of terminal stream");
                return new();
            }
            var realEv = ConsumeEvent(rec.Value, ref timeout);
            return realEv ?? default;
        }

        private readonly Stopwatch _readTimer = new();
        private readonly WindowsTerminalInterop.INPUT_RECORD[] _recordBuffer = new WindowsTerminalInterop.INPUT_RECORD[1];
        private bool IsReaderEof { get; set; } = false;

        private WindowsTerminalInterop.INPUT_RECORD? ReadInputRecord(ref TimeSpan timeout)
        {
            if (IsReaderEof) return null;

            if (timeout != Timeout.InfiniteTimeSpan)
            {
                timeout = timeout < TimeSpan.Zero ? TimeSpan.Zero : timeout;

                if (!_readEvent.WaitOne(0))
                {
                    _readTimer.Restart();
                    var gotEvent = _readEvent.WaitOne(timeout);
                    _readTimer.Stop();
                    var elapsed = _readTimer.Elapsed;
                    timeout = elapsed >= timeout ? TimeSpan.Zero : timeout - elapsed;
                    if (!gotEvent) return null;
                }
            }

            var ret = WindowsTerminalInterop.ReadConsoleInputEx(Input.DangerousGetHandle(), _recordBuffer, (uint)_recordBuffer.Length, out var readCount, 0);
            if (ret == 0) return null;
            if (readCount <= 0)
            {
                IsReaderEof = true;
                return null;
            }
            if (readCount != _recordBuffer.Length) throw new Exception($"Error while reading from HandleWindowsTerminal: Expected {_recordBuffer.Length} input record read(s), got {readCount}");
            return _recordBuffer[0];
        }

        private TerminalKeyModifiers _currentMods = TerminalKeyModifiers.None;
        private (int, int)? _oldTermSize;
        private (int, int)? _oldMousePos;
        private TerminalMouseButtons _oldMouseState = 0;

        private readonly List<Rune> _graphemeCodePoints = new(8);
        private readonly List<char> _graphemeCharStorage = new(8 * 2);
        private TerminalKeyModifiers _graphemeMods = TerminalKeyModifiers.None;
        private char? _highPair;
        private TerminalKeyModifiers _highPairMods = TerminalKeyModifiers.None;

        private void AddGraphemeEvent(int? graphemeLen = null)
        {
            var graphemeStorageSp = CollectionsMarshal.AsSpan(_graphemeCharStorage);
            graphemeLen ??= StringInfo.GetNextTextElementLength(graphemeStorageSp);

            ReadOnlySpan<char> sp = graphemeStorageSp[..graphemeLen.Value];
            sp.ToCodePoints(_graphemeCodePoints);

            var graphemeCodePointsSp = CollectionsMarshal.AsSpan(_graphemeCodePoints);
            _pendingEvents.Enqueue(new TerminalKeyEvent(this, new TerminalKey(new Grapheme(graphemeCodePointsSp), _graphemeMods)));

            _graphemeCharStorage.RemoveRange(0, graphemeLen.Value);
            _graphemeMods = TerminalKeyModifiers.None;
            _graphemeCodePoints.Clear();
        }
        private TerminalEvent? ConsumeEvent(in WindowsTerminalInterop.INPUT_RECORD ev, ref TimeSpan timeout)
        {
            switch (ev.EventType)
            {
                case WindowsTerminalInterop.InputEventType.KEY_EVENT:
                {
                    var kv = ev.Event.KeyEvent;
                    ConsumeMods(kv.dwControlKeyState);

                    TerminalKeyEvent? realEv = null;

                    TerminalKey.SpecialKey? special = null;
                    switch (kv.wVirtualKeyCode)
                    {
                        case 0x5B:
                        case 0x5C:
                            if (kv.bKeyDown != 0)
                                _currentMods |= TerminalKeyModifiers.Super;
                            else
                                _currentMods &= ~TerminalKeyModifiers.Super;
                            return PollEvent(timeout);
                        case 0x10:
                        case 0x11:
                        case 0x12:
                        case 0x14:
                        case 0xA0:
                        case 0xA1:
                        case 0xA2:
                        case 0xA3:
                        case 0xA4:
                        case 0xA5:
                            // shift, ctrl alt, (L/R)+shift,ctrl,alt
                            return PollEvent(timeout);
                        case >= 0x70 and <= 0x87:
                            special = TerminalKey.SpecialKey.F1 + (kv.wVirtualKeyCode - 0x70);
                            break;
                        case 0x65: // Numpad5
                            special = TerminalKey.SpecialKey.Begin;
                            break;
                        case 0x61: // Numpad1
                        case 0x23:
                            special = TerminalKey.SpecialKey.End;
                            break;
                        case 0x67: // Numpad7
                        case 0x24:
                            special = TerminalKey.SpecialKey.Home;
                            break;
                        case 0x25:
                        case 0x64: // Numpad4
                            special = TerminalKey.SpecialKey.Left;
                            break;
                        case 0x26:
                        case 0x68: // Numpad8
                            special = TerminalKey.SpecialKey.Up;
                            break;
                        case 0x27:
                        case 0x66: // Numpad6
                            special = TerminalKey.SpecialKey.Right;
                            break;
                        case 0x28:
                        case 0x62: // Numpad2
                            special = TerminalKey.SpecialKey.Down;
                            break;
                        case 0x2D:
                        case 0x60: // Numpad0
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
                        case 0x2e:
                            special = TerminalKey.SpecialKey.Delete;
                            break;
                        case 0x20:
                            realEv = new TerminalKeyEvent(this, new TerminalKey(' ', _currentMods));
                            break;
                        case >= 0x30 and <= 0x39:
                            realEv = new TerminalKeyEvent(this, new TerminalKey('0' + (kv.wVirtualKeyCode - 0x30), _currentMods));
                            break;
                        case >= 0x41 and <= 0x5A:
                            char start = _currentMods == TerminalKeyModifiers.Shift ? 'A' : 'a';
                            realEv = new TerminalKeyEvent(this, new TerminalKey(start + (kv.wVirtualKeyCode - 0x41), _currentMods));
                            break;
                        case 0x08:
                            realEv = new TerminalKeyEvent(this, new TerminalKey('\x7f', _currentMods));
                            break;
                        case 0x0D:
                            realEv = new TerminalKeyEvent(this, new TerminalKey('\r', _currentMods));
                            break;
                        case 0x6A:
                            realEv = new TerminalKeyEvent(this, new TerminalKey('*', _currentMods));
                            break;
                        case 0x6B:
                            realEv = new TerminalKeyEvent(this, new TerminalKey('+', _currentMods));
                            break;
                        case 0x6D:
                            realEv = new TerminalKeyEvent(this, new TerminalKey('-', _currentMods));
                            break;
                        case 0x6F:
                            realEv = new TerminalKeyEvent(this, new TerminalKey('/', _currentMods));
                            break;
                        case 0xBB:
                        {
                            var key = _currentMods == TerminalKeyModifiers.Shift ? '+' : '=';
                            realEv = new TerminalKeyEvent(this, new TerminalKey(key, _currentMods));
                            break;
                        }
                        case 0xBC:
                        {
                            var key = _currentMods == TerminalKeyModifiers.Shift ? '<' : ',';
                            realEv = new TerminalKeyEvent(this, new TerminalKey(key, _currentMods));
                            break;
                        }
                        case 0xBD:
                        {
                            var key = _currentMods == TerminalKeyModifiers.Shift ? '_' : '-';
                            realEv = new TerminalKeyEvent(this, new TerminalKey(key, _currentMods));
                            break;
                        }
                        case 0xBE:
                        {
                            var key = _currentMods == TerminalKeyModifiers.Shift ? '>' : '.';
                            realEv = new TerminalKeyEvent(this, new TerminalKey(key, _currentMods));
                            break;
                        }
                        case 0x69: // Numpad9
                            special = TerminalKey.SpecialKey.PageUp;
                            break;
                        case 0x63: // Numpad3
                            special = TerminalKey.SpecialKey.PageDown;
                            break;
                        case 0x05:
                            realEv = new TerminalKeyEvent(this, new TerminalKey('\\', _currentMods));
                            break;
                        default:
                            if (kv.uChar.UnicodeChar == 0x0)
                            {
                                return PollEvent(timeout);
                            }
                            switch (kv.uChar.UnicodeChar)
                            {
                                case '\x1c':
                                    realEv = new TerminalKeyEvent(this, new TerminalKey('\\', _currentMods));
                                    break;
                                case '\x1d':
                                    realEv = new TerminalKeyEvent(this, new TerminalKey(']', _currentMods));
                                    break;
                                case '\x1e':
                                    realEv = new TerminalKeyEvent(this, new TerminalKey('^', _currentMods));
                                    break;
                                case '\x1f':
                                    realEv = new TerminalKeyEvent(this, new TerminalKey('/', _currentMods));
                                    break;
                                default: break;
                            }
                            break;
                    }

                    if (kv.bKeyDown == 0) return PollEvent(timeout);

                    if (realEv is null)
                    {
                        if (special is not null)
                        {
                            realEv = new TerminalKeyEvent(this, new TerminalKey(special.Value, _currentMods));
                        }
                        else
                        {
                            if (char.IsHighSurrogate(kv.uChar.UnicodeChar))
                            {
                                _highPair = kv.uChar.UnicodeChar;
                                _highPairMods = _currentMods;
                                return PollEvent(timeout);
                            }
                            else if (char.IsLowSurrogate(kv.uChar.UnicodeChar))
                            {
                                if (_highPair is null)
                                    return PollEvent(timeout);

                                _graphemeCharStorage.Add((char)_highPair.Value);
                                _graphemeCharStorage.Add((char)kv.uChar.UnicodeChar);

                                _graphemeMods |= _highPairMods;

                                _highPairMods = TerminalKeyModifiers.None;
                                _highPair = null;

                                var graphemeStorageSp = CollectionsMarshal.AsSpan(_graphemeCharStorage);
                                var graphemeLen = StringInfo.GetNextTextElementLength(graphemeStorageSp);

                                if (graphemeLen < graphemeStorageSp.Length || IsReaderEof)
                                {
                                    AddGraphemeEvent(graphemeLen);
                                }
                                return PollEvent(timeout);
                            }
                            else if (_graphemeCharStorage.Count > 0)
                            {
                                int ch = kv.uChar.UnicodeChar;

                                _graphemeCharStorage.Add((char)ch);
                                _graphemeMods |= _currentMods;

                                var graphemeStorageSp = CollectionsMarshal.AsSpan(_graphemeCharStorage);
                                var graphemeLen = StringInfo.GetNextTextElementLength(graphemeStorageSp);

                                if (graphemeLen < graphemeStorageSp.Length || IsReaderEof)
                                {
                                    AddGraphemeEvent(graphemeLen);
                                }
                                return PollEvent(timeout);
                            }
                            else
                            {
                                int ch = kv.uChar.UnicodeChar;
                                realEv = new TerminalKeyEvent(this, new TerminalKey(ch, _currentMods));
                            }
                        }
                    }

                    if (realEv is not null)
                    {
                        for (uint i = 0; i < Math.Max(kv.wRepeatCount, 1u) - 1; i++)
                        {
                            _pendingEvents.Enqueue(realEv.Value);
                        }
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
                    else if ((mv.dwEventFlags & MOUSE_WHEELED) == MOUSE_WHEELED)
                    {
                        var val = (short)mv.dwButtonState.HighUShort() > 0 ? 1 : -1;
                        eventList.Add(new TerminalMouseScrollEvent(this, newPos, val, TerminalScrollDirection.Vertical, _currentMods));
                    }
                    else if ((mv.dwEventFlags & MOUSE_HWHEELED) == MOUSE_HWHEELED)
                    {
                        var val = (short)mv.dwButtonState.HighUShort() > 0 ? 1 : -1;
                        eventList.Add(new TerminalMouseScrollEvent(this, newPos, val, TerminalScrollDirection.Horizontal, _currentMods));
                    }
                    else if ((mv.dwEventFlags & DOUBLE_CLICK) == DOUBLE_CLICK || mv.dwEventFlags == 0)
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
                        if (_oldMouseState != buttons)
                        {
                            int diff = (int)(_oldMouseState ^ buttons);
                            var releaseButtons = TerminalMouseButtons.None;
                            var pressButtons = TerminalMouseButtons.None;
                            while (diff != 0)
                            {
                                int b = diff & (1 << BitOperations.TrailingZeroCount(diff));
                                if ((b & (int)buttons) != 0)
                                {
                                    pressButtons |= (TerminalMouseButtons)b;
                                }
                                else
                                {
                                    releaseButtons |= (TerminalMouseButtons)b;
                                }
                                diff &= ~b;
                            }

                            // TODO: May want to split up here if it makes parsing easier
                            if (releaseButtons != TerminalMouseButtons.None)
                                eventList.Add(new TerminalMouseClickEvent(this, newPos, releaseButtons, _currentMods, -1));

                            if (pressButtons != TerminalMouseButtons.None)
                                eventList.Add(new TerminalMouseClickEvent(this, newPos, pressButtons, _currentMods, 1));
                        }
                        else if (buttons != TerminalMouseButtons.None)
                        {
                            // TODO: May just want to pass on as single click since "The first click is returned as a regular button-press event."
                            int clickCount = (mv.dwEventFlags & DOUBLE_CLICK) == DOUBLE_CLICK ? 2 : 1;
                            eventList.Add(new TerminalMouseClickEvent(this, newPos, buttons, _currentMods, clickCount));
                        }
                        _oldMouseState = buttons;
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

        public override bool CanPollEvent(Type t)
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

        private IHandleWindowsTerminalStrategy EnsureStratInit()
        {
            if (_strat is null)
            {
                if (WindowsTerminalInterop.AddAnsiEscapeOutput(Output.DangerousGetHandle()))
                {
                    _colorSupportCache = TerminalColorSupport.TrueColor | TerminalColorSupport.Ansi8 | TerminalColorSupport.Ansi16 | TerminalColorSupport.XTerm256;
                    _strat = new StreamHandleWindowsTerminalStrategy(this);
                }
                else
                {
                    _colorSupportCache = TerminalColorSupport.Ansi8 | TerminalColorSupport.Ansi16;
                    _strat = null; // TODO: Implement
                    throw new NotImplementedException();
                }
            }
            return _strat;
        }
    }

    internal interface IHandleWindowsTerminalStrategy : IDisposable
    {
        void Init();
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
        private readonly TextWriter _output;
        private readonly Terminal _term;

        private readonly StringBuilder _escapeBuilder = new(32);

        public StreamHandleWindowsTerminalStrategy(HandleWindowsTerminal terminal) : this(terminal, terminal.OutputEncoding) { }
        public StreamHandleWindowsTerminalStrategy(HandleWindowsTerminal terminal, Encoding encoding)
        {
            _term = terminal;
            _output = new StreamWriter(new FileStream(terminal.Output, FileAccess.Write), encoding);
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

        public void Erase(int count = 1)
        {
            if (count == 0) return;
            if (count > 0) MoveCursor(-count, 0);
            WriteOutput($"\x1b[{Math.Abs(count)}P");
        }

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
                    if (dx == -1) _escapeBuilder.Append($"\x1b[D");
                    else _escapeBuilder.Append($"\x1b[{-dx}D");
                    break;
                case 0:
                    break;
            }

            switch (dy)
            {
                case < 0:
                    if (dy == -1) _escapeBuilder.Append($"\x1b[B");
                    else _escapeBuilder.Append($"\x1b[{-dy}B");
                    break;
                case > 0:
                    if (dy == 1) _escapeBuilder.Append("\x1b[A");
                    else _escapeBuilder.Append($"\x1b[{dy}A");
                    break;
                case 0:
                    break;
            }

            WriteOutput(_escapeBuilder.ToString());
        }

        private TerminalCellColor? _currentColor;
        public void Write(ColoredString str)
        {
            if (str.Text.Length <= 0) return;
            WriteOutput(GetEscapeCodeFor(str.Color, ref _currentColor));
            WriteOutput(str.Text);

            // FIXME: Use a cached cursor position to determine if str will overflow onto the next line

            _currentColor = TerminalCellColor.Default;
            WriteOutput("\x1b[0;39;49m");
        }

        public void Write(params ColoredString[] strings) => Write((IEnumerable<ColoredString>)strings);

        public void Write(IEnumerable<ColoredString> strings)
        {
            foreach (var str in strings)
            {
                if (str.Text.Length <= 0) continue;
                WriteOutput(GetEscapeCodeFor(str.Color, ref _currentColor));
                WriteOutput(str.Text);
            }

            _currentColor = TerminalCellColor.Default;
            WriteOutput("\x1b[0;39;49m");
        }

        private string GetEscapeCodeFor(TerminalCellColor cellColor, ref TerminalCellColor? currentColor)
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
            _currentColor = cellColor;
            return _escapeBuilder.Append('m').ToString();
        }

        private void WriteOutput(string s)
        {
            if (s.Length <= 0) return;
            _output.Write(s);
        }

        public void Flush()
        {
            _output.Flush();
        }

        public void Dispose()
        {
            WriteOutput("\x1b[0;39;49m");
            WriteOutput("\x1b[?1049l");
            Flush();
            _output.Dispose();
        }

        public void Init()
        {
            WriteOutput("\x1b[?1049h");
            WriteOutput("\x1b[H");
            WriteOutput("\x1b[0;39;49m");
            _currentColor = TerminalCellColor.Default;
        }
    }

    internal sealed class StandardWindowsTerminal : HandleWindowsTerminal
    {
        private StandardWindowsTerminal() : base(new SafeFileHandle(WindowsTerminalInterop.StdIn, false), new SafeFileHandle(WindowsTerminalInterop.StdOut, false))
        {
            Console.InputEncoding = OutputEncoding;
            Console.OutputEncoding = OutputEncoding;
        }

        private static StandardWindowsTerminal? _instance;
        public static StandardWindowsTerminal Instance => _instance ??= new StandardWindowsTerminal();
    }
}
