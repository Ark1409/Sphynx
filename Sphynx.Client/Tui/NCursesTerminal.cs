// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using Mindmagma.Curses;
using Sphynx.Client.Utils;
using Sphynx.Collections;
using Sphynx.Utils;

namespace Sphynx.Client.Tui
{
    public abstract class NCursesTerminal : Terminal
    {

        private TerminalColorSupport? _colorSupportCache;
        public override TerminalColorSupport ColorSupport
        {
            get
            {
                if (_colorSupportCache is not null) return _colorSupportCache.Value;
                var sup = TerminalColorSupport.Ansi8;
                if (TermName is not null)
                {
                    string[] _trueColorNames = ["xterm-kitty", "xterm-ghostty", "konsole", "wezterm", "st-256color"];
                    foreach (var name in _trueColorNames)
                    {
                        if (TermName.Contains(name, StringComparison.Ordinal))
                        {
                            _colorSupportCache = TerminalColorSupport.Ansi8 | TerminalColorSupport.Ansi16 | TerminalColorSupport.XTerm256 | TerminalColorSupport.TrueColor;
                            return _colorSupportCache.Value;
                        }
                    }

                    if (TermName.Contains("linux", StringComparison.OrdinalIgnoreCase))
                    {
                        _colorSupportCache = TerminalColorSupport.Ansi8;
                        return _colorSupportCache.Value;
                    }

                    if (TermName.Contains("256color", StringComparison.OrdinalIgnoreCase))
                    {
                        sup |= TerminalColorSupport.Ansi8 | TerminalColorSupport.Ansi16 | TerminalColorSupport.XTerm256;
                    }
                }

                if (GetUserStringCap("setrgbf") is not null && GetUserStringCap("setrgbb") is not null)
                {
                    sup |= TerminalColorSupport.Ansi8 | TerminalColorSupport.Ansi16 | TerminalColorSupport.XTerm256 | TerminalColorSupport.TrueColor;
                }
                else if (HasRGBCap())
                {
                    sup = TerminalColorSupport.TrueColor;
                }

                if (GetNumCap("colors") is var colNum and > 0)
                {
                    if (colNum == 8) sup |= TerminalColorSupport.Ansi8;
                    if (colNum == 16) sup |= TerminalColorSupport.Ansi16 | TerminalColorSupport.Ansi8;
                    if (colNum == 256) sup |= TerminalColorSupport.XTerm256 | TerminalColorSupport.Ansi16 | TerminalColorSupport.Ansi8;
                }

                if (Environment.GetEnvironmentVariable("COLORTERM") is { } colorTerm)
                {
                    if (colorTerm.Trim().Contains("truecolor", StringComparison.OrdinalIgnoreCase)) sup |= TerminalColorSupport.TrueColor;
                }

                _colorSupportCache = sup;
                return _colorSupportCache.Value;
            }
        }

        private bool? _mouseSupportCache;
        public override bool HasMouseSupport
        {
            get
            {
                if (_mouseSupportCache is not null) return _mouseSupportCache.Value;

                if (TermName is not null)
                {
                    if (TermName.Contains("xterm", StringComparison.OrdinalIgnoreCase))
                    {
                        _mouseSupportCache = true;
                        return _mouseSupportCache.Value;
                    }
                }

                if (GetUserStringCap("XM") is not null)
                {
                    _mouseSupportCache = true;
                    return _mouseSupportCache.Value;
                }

                if (GetNumCap("btns") > 0)
                {
                    _mouseSupportCache = true;
                    return _mouseSupportCache.Value;
                }

                if (GetStringCap("kmous") is not null)
                {
                    _mouseSupportCache = true;
                    return _mouseSupportCache.Value;
                }

                _mouseSupportCache = false;
                return _mouseSupportCache.Value;
            }
        }

        // Contains, not equality
        private static readonly string[] _graphicsProtcolNames = ["xterm-kitty", "xterm-ghostty", "konsole", "wezterm"];
        public override bool HasGraphicsProtocol
        {
            get
            {
                if (TermName is null) return false;
                foreach (var name in _graphicsProtcolNames)
                {
                    if (TermName.Contains(name, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        // Contains, not equality
        private static readonly string[] _keyboardProtocolNames = ["kitty", "ghostty", "konsole", "wezterm", "alacritty", "foot"];
        private static readonly string[] _keyboardProtocolProgramNames = ["foot", "ghostty", "Alacritty", "iTerm.app", "WezTerm", "WarpTerminal"];
        private bool? _hasKeyboardProtocolCache;
        public bool HasKeyboardProtocol
        {
            get
            {
                if (_hasKeyboardProtocolCache is not null) return _hasKeyboardProtocolCache.Value;
                if (TermName is null) return false;
                foreach (var name in _keyboardProtocolNames)
                {
                    if (TermName.Contains(name, StringComparison.Ordinal))
                    {
                        _hasKeyboardProtocolCache = true;
                        return _hasKeyboardProtocolCache.Value;
                    }
                }
                if (Environment.GetEnvironmentVariable("TERM_PROGRAM") is { } termProgram)
                {
                    foreach (var name in _keyboardProtocolProgramNames)
                    {
                        if (TermName.Contains(name, StringComparison.Ordinal))
                        {
                            _hasKeyboardProtocolCache = true;
                            return _hasKeyboardProtocolCache.Value;
                        }
                    }
                }

                _hasKeyboardProtocolCache = false;
                return _hasKeyboardProtocolCache.Value;
            }
        }

        private readonly List<int> _readItems = new();
        protected (int X, int Y)? RawCursorPositionQuery()
        {
            // Do not try on unsupported terminals
            if (!CanRawCursorPositionQuery()) return null;


            // NOTE: Unsure of general 'CSI ? 6 n' support (kitty doesn't support it)
            // Fallback to (ambiguous) 'CSI 6 n'
            WriteRaw("\x1b[6n");
            Flush();

            // Pray we don't get a <Modifier>+F3
            // See https://en.wikipedia.org/wiki/ANSI_escape_code#Terminal_input_sequences:~:text=xterm%20replies
            // Maybe look into somehow reading underneath the `_reader` (directly from the stream, no buffering, since
            // we know its gonna come after what's in the buffer)
            // In this implementation, we require all the characters for the mouse response to appear in sequence

            (int X, int Y)? foundPos = null;

            var index = -1;

            bool shouldShrink = _readItems.Count > 1024;
            _readItems.Clear();
            if (shouldShrink) _readItems.TrimExcess();

            var pending = _reader.MinimumPending;
            var posEsacpeLen = "\x1b[32767;32767R".Length;

            var useLen = 0;
            for (int codePoint = TakeCodePoint(); codePoint != -1 && index < pending + posEsacpeLen + (1 << 16); codePoint = TakeCodePoint())
            {
                int beginIndex = index;

                if (codePoint != '\x1b')
                {
                    continue;
                }

                if (!EnsureOrContinue('[', out _)) continue;

                // Parse row/col numbers
                // These work even for EOF since more characters are going to be needed after them anyways...

                var firstNumRange = ParseNumber();
                // if (firstNumRange.GetOffsetAndLength(readItems.Count).Length <= 0) continue;

                if (!EnsureOrContinue(';', out _)) continue;

                var secondNumRange = ParseNumber();
                // if (secondNumRange.GetOffsetAndLength(readItems.Count).Length <= 0) continue;

                if (!EnsureOrContinue('R', out _)) continue;

                var sp = CollectionsMarshal.AsSpan(_readItems);
                var rowStr = sp[firstNumRange].FromUtf32String();
                var colStr = sp[secondNumRange].FromUtf32String();
                // Sane default?
                int row = rowStr.Length <= 0 ? 1 : int.Parse(rowStr);
                int col = colStr.Length <= 0 ? 1 : int.Parse(colStr);
                foundPos = (col, row);
                useLen = index - beginIndex + 1;
                index = beginIndex - 1;
                break;
            }

            var readItemsSp = CollectionsMarshal.AsSpan(_readItems);
            _reader.UnRead(readItemsSp[..(index + 1)]);
            _reader.UnRead(readItemsSp[(index + 1 + useLen)..]);

            return foundPos;

            Range ParseNumber()
            {
                var r = new Range(index + 1, index + 1);
                for (var numberCp = TakeCodePoint(); numberCp != -1; numberCp = TakeCodePoint())
                {
                    var rune = new Rune(numberCp);
                    if (!rune.IsAscii || !char.IsNumber((char)numberCp))
                    {
                        index--;
                        break;
                    }
                }
                r = new Range(r.Start, index + 1);
                return r;
            }

            bool EnsureOrContinue(int cp, out int got)
            {
                got = TakeCodePoint();
                if (got == cp)
                {
                    return true;
                }
                if (got == '\x1b') index--;
                return false;
            }

            int TakeCodePoint()
            {
                if (index < _readItems.Count - 1)
                {
                    var cp = _readItems[++index];
                    return cp;
                }
                // FIXME: CPR seems to take too long to process and arrives in unpredictable timing. It seems to be
                // dependant on the speed at which keys are being typed. Our oniy choice is to palce an upper bound on
                // how long to read key(s) for in hopes that it'll arrive on that time. If we don't end up getting it,
                // that may be a problem, though we reset it to (1,1) usually in that case.
                var realCp = _reader.ReadCodePoint(TimeSpan.FromSeconds(4));
                if (realCp == -1) return -1;
                _readItems.Add(realCp);
                index++;
                return realCp;
            }
        }

        private bool CanRawCursorPositionQuery()
        {
            if ((ColorSupport & TerminalColorSupport.XTerm256) == TerminalColorSupport.XTerm256) return true;
            if (TermName is not null && TermName.Contains("xterm", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        protected (int X, int Y)? _cursorPosCache;
        public sealed override (int X, int Y) CursorPosition
        {
            get
            {
                if (_cursorPosCache is not null)
                {
                    return _cursorPosCache.Value;
                }

                _cursorPosCache = RawCursorPositionQuery();

                if (_cursorPosCache is null)
                {
                    CursorPosition = (1, 1);
                }

                return _cursorPosCache.Value;
            }

            [MemberNotNull(nameof(_cursorPosCache))]
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value.X, nameof(value.X));
                ArgumentOutOfRangeException.ThrowIfNegative(value.Y, nameof(value.Y));

                ArgumentOutOfRangeException.ThrowIfGreaterThan(value.X, 32767, nameof(value.X));
                ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Y, 32767, nameof(value.Y));

                value.X = Math.Clamp(value.X, 1, Columns);
                value.Y = Math.Clamp(value.Y, 1, Lines);

                if (value.X == 1)
                {
                    if (value.Y == 1)
                    {
                        if (GetStringCap("home") is { } homeCap)
                        {
                            WriteTermInfoString(homeCap);
                            _cursorPosCache = value;
                            return;
                        }
                    }
                    if (value.Y == Lines)
                    {
                        // TODO: check if place cursor at beg of line as well
                        if (GetStringCap("ll") is { } lastLineCap)
                        {
                            WriteTermInfoString(lastLineCap);
                            _cursorPosCache = value;
                            return;
                        }
                    }
                }

                if (GetStringCap("cup") is { } cap)
                {
                    WriteTermInfoString(NCurses.Tiparm(cap, value.Y, value.X));
                    _cursorPosCache = value;
                    return;
                }

                var hcap = RequireStringCap("hpa");
                var vcap = RequireStringCap("vpa");
                if (hcap is not null && vcap is not null)
                {
                    WriteTermInfoString(NCurses.Tiparm(vcap, value.Y));
                    WriteTermInfoString(NCurses.Tiparm(hcap, value.X));
                    _cursorPosCache = value;
                    return;
                }

                throw new InvalidOperationException($"Cannot find suitable capability string for cursor movement ({value.X}, {value.Y})");
            }
        }

        public sealed override ITerminalColor CursorColor { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public sealed override TerminalCursorShape CursorShape
        {
            set
            {
                if (value == TerminalCursorShape.DefaultShape)
                {
                    if (GetUserStringCap("Se") is { } Se) WriteTermInfoString(Se);
                    return;
                }

                if (GetUserStringCap("Ss") is not {} Ss)
                {
                    return;
                }

                switch (value)
                {
                    case TerminalCursorShape.BlinkingBlock:
                        WriteTermInfoString(NCurses.Tiparm(Ss, 1));
                        break;
                    case TerminalCursorShape.SteadyBlock:
                        WriteTermInfoString(NCurses.Tiparm(Ss, 2));
                        break;
                    case TerminalCursorShape.BlinkingUnderline:
                        WriteTermInfoString(NCurses.Tiparm(Ss, 3));
                        break;
                    case TerminalCursorShape.SteadyUnderline:
                        WriteTermInfoString(NCurses.Tiparm(Ss, 4));
                        break;
                    case TerminalCursorShape.BlinkingBar:
                        WriteTermInfoString(NCurses.Tiparm(Ss, 5));
                        break;
                    case TerminalCursorShape.SteadyBar:
                        WriteTermInfoString(NCurses.Tiparm(Ss, 6));
                        break;
                    default: break;
                }
            }
        }

        public sealed override TerminalCursorVisibility CursorVisibility
        {
            set
            {
                switch (value)
                {
                    case TerminalCursorVisibility.Visible:
                        if (GetStringCap("cnorm") is { } cnorm)
                            WriteTermInfoString(cnorm);
                        break;
                    case TerminalCursorVisibility.Invisible:
                        if (GetStringCap("civis") is { } civis)
                            WriteTermInfoString(civis);
                        break;
                    default: break;
                }
            }
        }

        protected (int Lines, int Columns)? OldWindowSize;
        protected nint _screenPtr = nint.Zero;

        private readonly TerminalReader _reader;
        private readonly NCursesTerminalWriter _writer;

        protected string? TermName;

        private readonly StringBuilder _scratchSb = new(32);


        public NCursesTerminal(Stream input, Stream output, string term)
            : this(input, Encodings.UTF8, output, Encodings.UTF8, term)
        {
        }

        public NCursesTerminal(Stream input, Encoding inputEncoding, Stream output, Encoding outputEncoding, string term)
        {
            _reader = new TerminalReader(input, inputEncoding) { ThrowOnTimeout = false };
            _writer = new NCursesTerminalWriter(output, outputEncoding);
            TermName = term.Trim();
        }

        public override void Init()
        {
            base.Init();
            NCurses.UseExtendedNames(true);

            LoadKeys();

            if (HasMouseSupport)
            {
                if (GetUserStringCap("XM") is { } XM)
                {
                    WriteTermInfoString(NCurses.Tiparm(XM, 1));
                }
                else
                {
                    WriteRaw("\x1b[?1003h\x1b[?1015h\x1b[?1006h");
                }
            }

            WriteTermInfoString(RequireStringCap("smcup")); // enter_ca_mode
            WriteTermInfoString(GetStringCap("smkx") ?? string.Empty); // keypad_xmit
            WriteTermInfoString(RequireStringCap("clear"));

            if (HasKeyboardProtocol)
            {
                // We can either use 23 or 31 (31 = 23 + 8 = Report all keys as escape codes)
                // Since "there are likely to be some terminal implementations that do not [implement the full spec],"
                // we'll use 23 as long as possible
                // WriteRaw("\x1b[>31u");
                WriteRaw("\x1b[>23u");
            }

            Flush();
        }

        public override async ValueTask DisposeAsync()
        {
            if (HasKeyboardProtocol)
            {
                WriteRaw("\x1b[<u");
            }

            WriteTermInfoString(GetStringCap("rmkx") ?? string.Empty); // keypad_local
            WriteTermInfoString(RequireStringCap("rmcup")); // exit_ca_mode

            if (HasMouseSupport)
            {
                if (GetUserStringCap("XM") is { } XM)
                {
                    WriteTermInfoString(NCurses.Tiparm(XM, 0));
                }
                else
                {
                    WriteRaw("\x1b[?1006l\x1b[?1015l\x1b[?1003l");
                }
            }

            Flush();

            await _writer.DisposeAsync();
            await _reader.DisposeAsync();
            _defaultColors = null;
            _mouseKeysCache = null;
            _pendingEvents.Clear();
            _termInfoKeys.Clear();
            _pendingEvents.TrimExcess();
            _termInfoKeys.TrimExcess();
            await base.DisposeAsync();
        }

        public override void Clear()
        {
            WriteTermInfoString(RequireStringCap("clear"));
        }

        public override void ClearLine()
        {
            // NOTE: Should maybe restore cursor pos?
            var cap = GetStringCap("hpa");
            if (cap is not null)
            {
                cap = NCurses.Tiparm(cap, 1);
            }
            else
            {
                cap = RequireStringCap("cr");
            }

            Debug.Assert(cap is not null);
            WriteTermInfoString(cap);
            WriteTermInfoString(RequireStringCap("el"));
        }

        public override void Erase(int count = 1)
        {
            if (count == 0) return;
            if (GetStringCap("dch") is { } dch)
            {
                if (count > 0)
                    MoveCursor(-count, 0);
                WriteTermInfoString(NCurses.Tiparm(dch, Math.Abs(count)));
            }
            else
            {
                var dch1 = RequireStringCap("dch1");
                if (count > 0)
                    MoveCursor(-count, 0);
                WriteTermInfoString(GetStringCap("smdc") ?? string.Empty);
                WriteTermInfoString(dch1.Repeat(Math.Abs(count)));
                WriteTermInfoString(GetStringCap("rmdc") ?? string.Empty);
            }
        }

        public override void Flush()
        {
            _writer.Flush();
        }

        private (TerminalAnsiColor.AnsiColors, TerminalTrueColor)[]? _defaultColors = null;
        public override TerminalAnsiColor NearestAnsiColor(TerminalTrueColor color)
        {
            if (HasXtermOSC(4))
            {
                if (_defaultColors is null) QueryAnsiColors();
                if (_defaultColors.Length > 0)
                    return color.NearestAnsiColorFrom(_defaultColors);
            }
            return color.NearestAnsiColor;
        }

        public override TerminalTrueColor TrueColorFor(TerminalAnsiColor color)
        {
            if (HasXtermOSC(4))
            {
                if (_defaultColors is null) QueryAnsiColors();

                for (int i = 0; i < _defaultColors.Length; i++)
                {
                    if (_defaultColors[i].Item1 == color)
                    {
                        return _defaultColors[i].Item2;
                    }
                }
            }

            return color.NearestTrueColor;
        }

        private bool HasXtermOSC(int osc)
        {
            switch (osc)
            {
                case 4:
                    if (TermName is null) return false;
                    return TermName.Contains("xterm", StringComparison.OrdinalIgnoreCase);
                default: return false;
            }
        }

        [MemberNotNull(nameof(_defaultColors))]
        private void QueryAnsiColors()
        {
            for (int i = 0; i < 256; i++)
            {
                WriteRaw($"\x1b]4;{i};?\x07");
            }
            Flush();

            var pending = _reader.MinimumPending;
            var singleColorLength = "\x1b]4;255;rgb:ffff/ffff/ffff\x07".Length;

            var readItems = new List<int>(pending + singleColorLength * 256);
            var index = -1;

            var list = new List<(TerminalAnsiColor.AnsiColors, TerminalTrueColor)>(256);
            for (int codePoint = TakeCodePoint(); codePoint != -1 && index < pending + singleColorLength * (256 - list.Count) + (1 << 24); codePoint = TakeCodePoint())
            {
                int beginIndex = index;

                if (codePoint != '\x1b') continue;

                if (!EnsureOrContinue(']', out _)) continue;
                if (!EnsureOrContinue('4', out _)) continue;
                if (!EnsureOrContinue(';', out _)) continue;
                var colNumRange = ParseNumber();
                if (colNumRange.GetOffsetAndLength(readItems.Count).Length <= 0) continue;
                if (!EnsureOrContinue(';', out _)) continue;

                var col = ParseColor();
                if (col is null) continue;

                if (!EnsureOrContinue('\x07', out _)) continue;

                var colNum = int.Parse(readItems[colNumRange].FromUtf32String());
                if (colNum < 0 || colNum > 255) continue;
                list.Add(((TerminalAnsiColor.AnsiColors)colNum, col.Value));
                readItems.RemoveRange(beginIndex, index - beginIndex + 1);
                index -= index - beginIndex + 1;
                if (list.Count >= 256) break;
            }

            _defaultColors = list.ToArray();

            _reader.UnRead(CollectionsMarshal.AsSpan(readItems)[..(index + 1)]);

            TerminalTrueColor? ParseColor()
            {
                // TODO: Implement full color spec?
                // https://linux.die.net/man/3/xparsecolor
                Span<byte> nums = stackalloc byte[3];
                for (int i = 0; i < nums.Length; i++)
                {
                    if (i > 0)
                    {
                        if (!EnsureOrContinue('/', out _)) return null;
                    }
                    var r = new Range(index + 1, index + 1);
                    for (var numberCp = TakeCodePoint(); numberCp != -1; numberCp = TakeCodePoint())
                    {
                        var numberString = char.ConvertFromUtf32(numberCp);
                        if (!char.TryParse(numberString, out var numCh) || !char.IsAsciiHexDigit(numCh))
                        {
                            index--;
                            break;
                        }
                    }
                    r = new Range(r.Start, index + 1);
                    if (r.GetOffsetAndLength(readItems.Count).Length <= 0) return null;
                    var numStr = readItems[r].FromUtf32String();
                    if (ulong.TryParse(numStr, NumberStyles.HexNumber, null, out var res))
                    {
                        nums[i] = (byte)(res * 255 / ((1UL << (4 * numStr.Length)) - 1));
                    }
                    else return null;
                }
                return new TerminalTrueColor(nums[0], nums[1], nums[2]);
            }

            Range ParseNumber()
            {
                var r = new Range(index + 1, index + 1);
                for (var numberCp = TakeCodePoint(); numberCp != -1; numberCp = TakeCodePoint())
                {
                    var numberString = char.ConvertFromUtf32(numberCp);
                    if (!char.TryParse(numberString, out var numCh) || !char.IsNumber(numCh))
                    {
                        index--;
                        break;
                    }
                }
                r = new Range(r.Start, index + 1);
                return r;
            }

            bool EnsureOrContinue(int cp, out int got)
            {
                got = TakeCodePoint();
                if (got == cp)
                {
                    return true;
                }
                if (got == '\x1b') index--;
                return false;
            }

            int TakeCodePoint()
            {
                if (index < readItems.Count - 1)
                {
                    var cp = readItems[++index];
                    return cp;
                }
                var realCp = _reader.ReadCodePoint(TimeSpan.FromMilliseconds(1));
                if (realCp == -1) return -1;
                readItems.Add(realCp);
                index++;
                return realCp;
            }
        }

        private string[]? _mouseKeysCache;
        private string[] GetMouseKeysSorted()
        {
            if (_mouseKeysCache is not null) return _mouseKeysCache;

            string[] defaultKeys = ["\x1b[<", "\x1b[M", "\x1b["];
            _mouseKeysCache = defaultKeys;

            if (GetStringCap("kmous") is { } kmous)
            {
                if (!_mouseKeysCache.Contains(kmous))
                {
                    _mouseKeysCache = [.. _mouseKeysCache, kmous];
                }
            }

            Array.Sort(_mouseKeysCache, static (a, b) => a.Length - b.Length);

            return _mouseKeysCache;
        }

        private readonly Queue<TerminalEvent> _pendingEvents = new();
        protected volatile bool ShouldPollWindowSize = false;

        protected void EnqueueEvent<T>(T ev) where T : ITerminalEvent
        {
            _pendingEvents.Enqueue(ev.AsEvent);
        }

        protected virtual void PollWindowSize()
        {
            var newSize = (Lines, Columns);
            var wasNull = OldWindowSize is null;
            OldWindowSize ??= newSize;
            if (wasNull || OldWindowSize != newSize)
                EnqueueEvent(new TerminalWindowEvent(this, OldWindowSize.Value, newSize));
            OldWindowSize = newSize;
        }

        public sealed override TerminalEvent PollEvent(TimeSpan timeout)
        {
            Grapheme? grapheme = null;
            var spin = new SpinWait();
            while (timeout == Timeout.InfiniteTimeSpan || timeout >= TimeSpan.Zero)
            {
                if (ShouldPollWindowSize)
                {
                    ShouldPollWindowSize = false;
                    PollWindowSize();
                }

                if (_pendingEvents.TryDequeue(out var pendingEvent))
                {
                    return pendingEvent;
                }

                var waitTime = TimeSpan.FromMilliseconds(2);
                if (timeout != Timeout.InfiniteTimeSpan)
                    waitTime = waitTime < timeout ? waitTime : timeout;
                try
                {
                    grapheme = _reader.ReadGrapheme(waitTime);
                    if (grapheme is not null) break;
                    if (_reader.IsEof) break;
                }
                catch (TimeoutException) { _reader.ThrowOnTimeout = false; }
                catch (IOException) { break; }

                if (timeout != Timeout.InfiniteTimeSpan) timeout -= waitTime;
                if (timeout == TimeSpan.Zero) break;
                spin.SpinOnce();
            }

            // FIXME: Wait for window events only
            if (grapheme is null)
            {
                if (timeout == Timeout.InfiniteTimeSpan || _reader.IsEof) throw new EndOfStreamException("Reached end of terminal stream");
                return default;
            }

            if (grapheme.Value.Length > 1)
            {
                // Regular character
                EnqueueEvent(new TerminalKeyEvent(this, new TerminalKey(grapheme.Value)));

                return PollEvent(timeout);
            }

            var codePoint = grapheme.Value[0].Value;
            var codePointString = char.ConvertFromUtf32(codePoint);

            {
                var mouseKeys = GetMouseKeysSorted();
                var possibleMouseKey = false;
                if (HasMouseSupport)
                {
                    foreach (var s in mouseKeys)
                    {
                        if (s.StartsWith(codePointString))
                        {
                            possibleMouseKey = true;
                            break;
                        }
                    }
                }

                if (possibleMouseKey)
                {
                    var longestCodePointCount = mouseKeys.Max(static s => s.CodePointCount());

                    Span<int> full = stackalloc int[longestCodePointCount];
                    full[0] = codePoint;

                    // Scratch
                    Span<int> codePointStorage = stackalloc int[longestCodePointCount];

                    int codePointsRead = 1;
                    for (int mouseKeyIndex = 0; possibleMouseKey; possibleMouseKey = mouseKeyIndex < mouseKeys.Length)
                    {
                        var c = _reader.ReadCodePoint(TimeSpan.FromMilliseconds(1));
                        if (c == -1) break;
                        full[codePointsRead++] = c;

                        for (; mouseKeyIndex < mouseKeys.Length; mouseKeyIndex++)
                        {
                            var currentMouseKey = mouseKeys[mouseKeyIndex];
                            var currentMouseCpCount = currentMouseKey.ToCodePoints(codePointStorage);
                            var currentMouseCodePoints = codePointStorage[..currentMouseCpCount];
                            if (codePointsRead < currentMouseCpCount) break;
                            if (!full.StartsWith(currentMouseCodePoints)) continue;

                            // Must be a mouse key
                            _reader.UnRead(full[currentMouseKey.Length..codePointsRead]);
                            int eventCount = ConsumeMouseEventParams(currentMouseKey);
                            // May return 0 if EOF or not enough chars after this...
                            // Debug.Assert(eventCount > 0);
                            if (eventCount > 0)
                                return PollEvent(timeout);
                        }
                    }

                    // Rollback
                    _reader.UnRead(full[1..codePointsRead]);
                }
            }

            Debug.Assert(_maxTermInfoKeyLength <= 1024 / 2);
            {
                Span<int> full = stackalloc int[_maxTermInfoKeyLength];
                full[0] = codePoint;
                var fullCount = _reader.ReadCodePoints(full[1..], TimeSpan.FromMilliseconds(1));
                full = full[..(1 + fullCount)];
                if (full.Length >= _minTermInfoKeyLength)
                {
                    var fullStr = full.FromUtf32String();
                    for (int useLen = Math.Min(fullStr.Length, _maxTermInfoKeyLength); useLen >= _minTermInfoKeyLength && useLen > 0; useLen--)
                    {
                        if (_termInfoKeys.TryGetValue(fullStr[..useLen], out var val))
                        {
                            EnqueueEvent(new TerminalKeyEvent(this, val));
                            _reader.UnRead(full[useLen..]);
                            return PollEvent(timeout);
                        }
                    }
                }

                _reader.UnRead(full[1..]);
            }

            if (HasKeyboardProtocol)
            {
                if (codePoint == '\x1b')
                {
                    var keyTimeout = TimeSpan.FromMilliseconds(1);
                    var nextCp = _reader.ReadCodePoint(keyTimeout);

                    if (nextCp is '[')
                    {
                        if (TryParseKeyboardProtocolKey(keyTimeout, out var parsedKey))
                        {
                            if (parsedKey is not null)
                            {
                                EnqueueEvent(new TerminalKeyEvent(this, parsedKey.Value));
                            }
                            return PollEvent(timeout);
                        }
                    }
                    _reader.UnRead(nextCp);
                }
            }

            if (codePoint == '\x1b')
            {
                Span<int> full = stackalloc int[_maxTermInfoKeyLength];
                var fullCount = _reader.ReadCodePoints(full, TimeSpan.Zero);
                full = full[..fullCount];
                if (full.Length >= _minTermInfoKeyLength)
                {
                    var fullStr = full.FromUtf32String();
                    for (int useLen = Math.Min(fullStr.Length, _maxTermInfoKeyLength); useLen >= _minTermInfoKeyLength && useLen > 0; useLen--)
                    {
                        if (_termInfoKeys.TryGetValue(fullStr[..useLen], out var val))
                        {
                            var oldMods = val.Mods;
                            EnqueueEvent(new TerminalKeyEvent(this, val with { Mods = oldMods | TerminalKeyModifiers.Alt }));
                            _reader.UnRead(full[useLen..]);
                            return PollEvent(timeout);
                        }
                    }
                }
                _reader.UnRead(full);
            }

            if (codePoint == '\x1b')
            {
                // oop, might be a special key
                var nextCp = _reader.ReadCodePoint(TimeSpan.FromMilliseconds(1));

                if (nextCp is '[' or 'O')
                {
                    Span<int> scratch = stackalloc int[8]; // 8 should be enough to hold it...

                    // https://en.wikipedia.org/wiki/ANSI_escape_code#Terminal_input_sequences
                    var count = _reader.ReadCodePoints(scratch, TimeSpan.FromMilliseconds(1));
                    scratch = scratch[..count];

                    if (scratch.Length > 0)
                    {
                        // TODO: Fix alloc by making FromUtf32String take in string
                        var str = scratch.FromUtf32String();
                        ReadOnlySpan<char> item = [];
                        char mod = '1';
                        bool vt = false;
                        var useLen = 0;
                        {
                            int firstNumberEnd = 0;
                            for (; firstNumberEnd < scratch.Length; firstNumberEnd++)
                            {
                                if (!char.IsNumber(str[firstNumberEnd])) break;
                            }
                            useLen = firstNumberEnd;
                            if (firstNumberEnd < str.Length)
                            {
                                if (str[firstNumberEnd] == ';')
                                {
                                    var secondNumberEnd = firstNumberEnd + 2;
                                    mod = secondNumberEnd <= str.Length ? str[secondNumberEnd - 1] : '1';
                                    useLen = secondNumberEnd;
                                    if (secondNumberEnd < str.Length)
                                    {
                                        if (str[secondNumberEnd] == '~')
                                        {
                                            item = str.AsSpan()[..firstNumberEnd];
                                            vt = true;
                                            useLen++;
                                        }
                                        else if(char.IsLetter(str[secondNumberEnd]))
                                        {
                                            item = str.AsSpan().Slice(secondNumberEnd, 1);
                                            useLen++;
                                        }
                                    }
                                }
                                else if (str[firstNumberEnd] == '~')
                                {
                                    vt = true;
                                    item = str.AsSpan()[..firstNumberEnd];
                                    useLen = firstNumberEnd + 1;
                                }
                                else if (firstNumberEnd == 1)
                                {
                                    // Assume xterm
                                    mod = str[0];
                                    if (char.IsLetter(str[1]))
                                    {
                                        item = str.AsSpan()[1..2];
                                        useLen = 2;
                                    }
                                }
                                else
                                {
                                    // Assume xterm
                                    if (char.IsLetter(str[0]))
                                    {
                                        item = str.AsSpan()[0..1];
                                        useLen = 1;
                                    }
                                }
                            }
                            else if (firstNumberEnd == 1 && char.IsLetter(str[0]))
                            {
                                item = str.AsSpan()[..1];
                                useLen = 1;
                            }
                        }
                        if (item.Length > 0)
                        {
                            var modNum = int.Parse([mod]);
                            var keyMods = TerminalKeyModifiers.None;
                            if (((modNum - 1) & 0x1) == 0x1) keyMods |= TerminalKeyModifiers.Shift;
                            if (((modNum - 1) & 0x2) == 0x2) keyMods |= TerminalKeyModifiers.Alt;
                            if (((modNum - 1) & 0x4) == 0x4) keyMods |= TerminalKeyModifiers.Control;
                            if (((modNum - 1) & 0x8) == 0x8) keyMods |= TerminalKeyModifiers.Meta;

                            var itemString = new string(item);
                            var nonModStr = $"\x1b{(char)nextCp}{itemString}" + (vt ? string.Empty : '~');

                            TerminalKey val = default;
                            bool foundInTermInfo = false;
                            foundInTermInfo |= _termInfoKeys.TryGetValue(nonModStr, out val);
                            if (!foundInTermInfo)
                            {
                                if (!vt)
                                {
                                    nonModStr = $"\x1bO{itemString}" + (vt ? string.Empty : '~');
                                    foundInTermInfo |= _termInfoKeys.TryGetValue(nonModStr, out val);
                                }
                            }
                            if (foundInTermInfo)
                            {
                                EnqueueEvent(new TerminalKeyEvent(this, val with { Mods = keyMods }));
                                _reader.UnRead(scratch[useLen..]);
                                return PollEvent(timeout);
                            }

                            // See https://en.wikipedia.org/wiki/ANSI_escape_code#Terminal_input_sequences
                            if (vt)
                            {
                                var itemNum = int.Parse(item);
                                if (itemNum >= 1 && itemNum < 36)
                                {
                                    TerminalKey? key = null;
                                    switch (itemNum)
                                    {
                                        case 1:
                                        case 7:
                                            key = new TerminalKey(TerminalKey.SpecialKey.Home, keyMods);
                                            break;
                                        case 2:
                                            key = new TerminalKey(TerminalKey.SpecialKey.Insert, keyMods);
                                            break;
                                        case 3:
                                            key = new TerminalKey(TerminalKey.SpecialKey.Delete, keyMods);
                                            break;
                                        case 4:
                                        case 8:
                                            key = new TerminalKey(TerminalKey.SpecialKey.End, keyMods);
                                            break;
                                        case 5:
                                            key = new TerminalKey(TerminalKey.SpecialKey.PageUp, keyMods);
                                            break;
                                        case 6:
                                            key = new TerminalKey(TerminalKey.SpecialKey.PageDown, keyMods);
                                            break;
                                        case 9:
                                        case 16:
                                        case 22:
                                        case 27:
                                        case 30:
                                        case 35:
                                            // Do nothing on unmapped?
                                            _reader.UnRead(scratch[useLen..]);
                                            return PollEvent(timeout);
                                        case 10:
                                        case 11:
                                        case 12:
                                        case 13:
                                        case 14:
                                        case 15:
                                            key = new TerminalKey(TerminalKey.SpecialKey.F0 + (itemNum - 10), keyMods);
                                            break;
                                        case 17:
                                        case 18:
                                        case 19:
                                        case 20:
                                        case 21:
                                            key = new TerminalKey(TerminalKey.SpecialKey.F6 + (itemNum - 17), keyMods);
                                            break;
                                        case 23:
                                        case 24:
                                        case 25:
                                        case 26:
                                            key = new TerminalKey(TerminalKey.SpecialKey.F11 + (itemNum - 23), keyMods);
                                            break;
                                        case 28:
                                        case 29:
                                            key = new TerminalKey(TerminalKey.SpecialKey.F15 + (itemNum - 28), keyMods);
                                            break;
                                        case 31:
                                        case 32:
                                        case 33:
                                        case 34:
                                            key = new TerminalKey(TerminalKey.SpecialKey.F17 + (itemNum - 31), keyMods);
                                            break;
                                        default:
                                            break;
                                    }
                                    if (key is not null)
                                    {
                                        EnqueueEvent(new TerminalKeyEvent(this, key.Value));
                                        _reader.UnRead(scratch[useLen..]);
                                        return PollEvent(timeout);
                                    }
                                }
                            }
                            else
                            {
                                if (item.Length == 1)
                                {
                                    TerminalKey? key = null;
                                    switch (item[0])
                                    {
                                        case 'A':
                                            key = new TerminalKey(TerminalKey.SpecialKey.Up, keyMods);
                                            break;
                                        case 'B':
                                            key = new TerminalKey(TerminalKey.SpecialKey.Down, keyMods);
                                            break;
                                        case 'C':
                                            key = new TerminalKey(TerminalKey.SpecialKey.Right, keyMods);
                                            break;
                                        case 'D':
                                            key = new TerminalKey(TerminalKey.SpecialKey.Left, keyMods);
                                            break;
                                        case 'E':
                                            key = new TerminalKey(TerminalKey.SpecialKey.Begin, keyMods);
                                            break;
                                        case 'F':
                                            key = new TerminalKey(TerminalKey.SpecialKey.End, keyMods);
                                            break;
                                        case 'H':
                                            key = new TerminalKey(TerminalKey.SpecialKey.Home, keyMods);
                                            break;
                                        case 'I':
                                            key = new TerminalKey('\t', keyMods);
                                            break;
                                        case 'P':
                                            key = new TerminalKey(TerminalKey.SpecialKey.F1, keyMods);
                                            break;
                                        case 'Q':
                                            key = new TerminalKey(TerminalKey.SpecialKey.F2, keyMods);
                                            break;
                                        case 'R':
                                            key = new TerminalKey(TerminalKey.SpecialKey.F3, keyMods);
                                            break;
                                        case 'S':
                                            key = new TerminalKey(TerminalKey.SpecialKey.F4, keyMods);
                                            break;
                                        case 'X':
                                            key = new TerminalKey('=', keyMods);
                                            break;
                                        case 'M':
                                            key = new TerminalKey('\r', keyMods);
                                            break;
                                        case 'j':
                                            key = new TerminalKey('*', keyMods);
                                            break;
                                        case 'k':
                                            key = new TerminalKey('+', keyMods);
                                            break;
                                        case 'l':
                                            key = new TerminalKey(',', keyMods);
                                            break;
                                        case 'm':
                                            key = new TerminalKey('-', keyMods);
                                            break;
                                        case 'n':
                                            key = new TerminalKey('.', keyMods);
                                            break;
                                        case 'o':
                                            key = new TerminalKey('/', keyMods);
                                            break;
                                        case 'p':
                                            key = new TerminalKey('0', keyMods);
                                            break;
                                        case 'q':
                                            key = new TerminalKey('1', keyMods);
                                            break;
                                        case 'r':
                                            key = new TerminalKey('2', keyMods);
                                            break;
                                        case 's':
                                            key = new TerminalKey('3', keyMods);
                                            break;
                                        case 't':
                                            key = new TerminalKey('4', keyMods);
                                            break;
                                        case 'u':
                                            key = new TerminalKey('5', keyMods);
                                            break;
                                        case 'v':
                                            key = new TerminalKey('6', keyMods);
                                            break;
                                        case 'w':
                                            key = new TerminalKey('7', keyMods);
                                            break;
                                        case 'x':
                                            key = new TerminalKey('8', keyMods);
                                            break;
                                        case 'y':
                                            key = new TerminalKey('9', keyMods);
                                            break;
                                        default: break;
                                    }
                                    if (key is not null)
                                    {
                                        EnqueueEvent(new TerminalKeyEvent(this, key.Value));
                                        _reader.UnRead(scratch[useLen..]);
                                        return PollEvent(timeout);
                                    }
                                }
                            }
                        }
                    }
                    _reader.UnRead(scratch);
                }
                if (nextCp != -1)
                {
                    var rune = new Rune(nextCp);
                    if (rune.IsAscii)
                    {
                        if (_termInfoKeys.TryGetValue(rune.ToString(), out var v))
                        {
                            var vMods = v.Mods;
                            EnqueueEvent(new TerminalKeyEvent(this, v with { Mods = vMods | TerminalKeyModifiers.Alt }));
                        }
                        else
                        {
                            EnqueueEvent(new TerminalKeyEvent(this, new TerminalKey(nextCp, TerminalKeyModifiers.Alt)));
                        }
                        return PollEvent(timeout);
                    }
                    _reader.UnRead(nextCp);
                }
            }

            // Regular character
            EnqueueEvent(new TerminalKeyEvent(this, new TerminalKey(codePoint)));

            return PollEvent(timeout);
        }

        private TerminalKeyModifiers _keyboardProtocolMods = TerminalKeyModifiers.None;
        private readonly List<int> _keyboardProtocolCodePoints = new(16);
        private readonly List<int?> _keyboardProtocolFields = new(4);
        private readonly List<Range> _keyboardProtocolRanges = new(8);
        private readonly List<int> _keyboardProtocolTextAsCodePoints = new(4);
        private bool TryParseKeyboardProtocolKey(TimeSpan timeout, out TerminalKey? key)
        {
            _keyboardProtocolCodePoints.Clear();
            _keyboardProtocolFields.Clear();
            _keyboardProtocolRanges.Clear();
            _keyboardProtocolTextAsCodePoints.Clear();

            var finalCp = ParseEscapeCode(_keyboardProtocolRanges, _keyboardProtocolFields, timeout);
            if (finalCp == -1 || _keyboardProtocolRanges.Count > 3) return Bail(out key);

            _keyboardProtocolMods = TerminalKeyModifiers.None;

            var fieldsSpan = CollectionsMarshal.AsSpan(_keyboardProtocolFields);

            int? unicodeKeyCode = null, alternateKey = null, baseLayoutKey = null;
            if (_keyboardProtocolRanges.Count > 0)
            {
                var sp = fieldsSpan[_keyboardProtocolRanges[0]];

                if (sp.Length > 0)
                {
                    unicodeKeyCode = sp[0];
                }

                if (sp.Length > 1)
                {
                    alternateKey = sp[1];
                }

                if (sp.Length > 2)
                {
                    baseLayoutKey = sp[2];
                }
            }

            const int SHIFT = 0x1;
            const int ALT = 0x2;
            const int CTRL = 0x4;
            const int SUPER = 0x8;
            const int META = 0x20;

            const int PRESS = 1;
            const int REPEAT = 2;
            const int RELEASE = 3;

            var mods = TerminalKeyModifiers.None;
            var eventType = PRESS;

            _keyboardProtocolMods = mods;

            if (_keyboardProtocolRanges.Count > 1)
            {
                var sp = fieldsSpan[_keyboardProtocolRanges[1]];

                if (sp.Length > 0 && sp[0] is { } mod)
                {
                    mod--;
                    if ((mod & SHIFT) == SHIFT) mods |= TerminalKeyModifiers.Shift;
                    if ((mod & ALT) == ALT) mods |= TerminalKeyModifiers.Alt;
                    if ((mod & META) == META) mods |= TerminalKeyModifiers.Alt;
                    if ((mod & CTRL) == CTRL) mods |= TerminalKeyModifiers.Control;
                    if ((mod & SUPER) == SUPER) mods |= TerminalKeyModifiers.Super;
                }

                if (sp.Length > 1 && sp[1] is { } evType)
                {
                    if (evType is PRESS or REPEAT or RELEASE) eventType = evType;
                    else return Bail(out key);
                }
            }

            if (eventType == RELEASE)
            {
                key = null;
                return true;
            }

            if (_keyboardProtocolRanges.Count > 2)
            {
                var sp = fieldsSpan[_keyboardProtocolRanges[2]];
                foreach (var i in sp)
                {
                    if (i is not null) _keyboardProtocolTextAsCodePoints.Add(i.Value);
                }
            }

            switch (finalCp)
            {
                case 'u':
                    switch (unicodeKeyCode)
                    {
                        case 27:
                            key = new TerminalKey('\x1b', mods);
                            return true;
                        case 13:
                            key = new TerminalKey('\r', mods);
                            return true;
                        case 9:
                            key = new TerminalKey('\t', mods);
                            return true;
                        case 127:
                            key = new TerminalKey('\x7f', mods);
                            return true;
                        case 57361:
                            key = new TerminalKey(TerminalKey.SpecialKey.PrintScreen, mods);
                            return true;
                        case 57362:
                            key = new TerminalKey(TerminalKey.SpecialKey.Pause, mods);
                            return true;
                        case 57376:
                        case 57377:
                        case 57378:
                        case 57379:
                        case 57380:
                        case 57381:
                        case 57382:
                        case 57383:
                        case 57384:
                        case 57385:
                        case 57386:
                        case 57387:
                            key = new TerminalKey(TerminalKey.SpecialKey.F13 + (unicodeKeyCode.Value - 57376), mods);
                            return true;
                        case 57419:
                            key = new TerminalKey(TerminalKey.SpecialKey.Up, mods);
                            return true;
                        case 57420:
                            key = new TerminalKey(TerminalKey.SpecialKey.Down, mods);
                            return true;
                        case 57418:
                            key = new TerminalKey(TerminalKey.SpecialKey.Right, mods);
                            return true;
                        case 57417:
                            key = new TerminalKey(TerminalKey.SpecialKey.Left, mods);
                            return true;
                        case 57421:
                            key = new TerminalKey(TerminalKey.SpecialKey.PageUp, mods);
                            return true;
                        case 57422:
                            key = new TerminalKey(TerminalKey.SpecialKey.PageDown, mods);
                            return true;
                        case 57424:
                            key = new TerminalKey(TerminalKey.SpecialKey.End, mods);
                            return true;
                        case 57426:
                            key = new TerminalKey(TerminalKey.SpecialKey.Delete, mods);
                            return true;
                        case 57425:
                            key = new TerminalKey(TerminalKey.SpecialKey.Insert, mods);
                            return true;
                        case 57442:
                        case 57448:
                        case 57441:
                        case 57447:
                        case 57443:
                        case 57449:
                        case 57444:
                        case 57450:
                        case 57446:
                        case 57452:
                            // Mods should send no (ctrl, shift, alt, super. meta)
                            key = null;
                            return true;
                        case 0:
                        case >= 57344 and <= 63743: // Kitty advertises this range as what's repurposed for some keys
                        case <= 0x7f:
                        default:
                            // regular code point
                            // Check text as codepoint and alternate key (in that order)
                            foreach (var cp in _keyboardProtocolTextAsCodePoints)
                            {
                                key = new TerminalKey(cp, mods);
                                return true;
                            }
                            if (alternateKey is { } altKey)
                            {
                                key = new TerminalKey(altKey, mods);
                                return true;
                            }
                            if (unicodeKeyCode is not null)
                            {
                                if (unicodeKeyCode == 0) key = null;
                                else key = new TerminalKey(unicodeKeyCode.Value, mods);
                                return true;
                            }
                            return Bail(out key);
                    }
                case '~':
                    switch (unicodeKeyCode)
                    {
                        case 2:
                            key = new TerminalKey(TerminalKey.SpecialKey.Insert, mods);
                            return true;
                        case 3:
                            key = new TerminalKey(TerminalKey.SpecialKey.Delete, mods);
                            return true;
                        case 5:
                            key = new TerminalKey(TerminalKey.SpecialKey.PageUp, mods);
                            return true;
                        case 6:
                            key = new TerminalKey(TerminalKey.SpecialKey.PageDown, mods);
                            return true;
                        case 7:
                            key = new TerminalKey(TerminalKey.SpecialKey.Home, mods);
                            return true;
                        case 8:
                            key = new TerminalKey(TerminalKey.SpecialKey.End, mods);
                            return true;
                        case 11:
                        case 12:
                        case 13:
                        case 14:
                            key = new TerminalKey(TerminalKey.SpecialKey.F1 + (unicodeKeyCode.Value - 11), mods);
                            return true;
                        case 15:
                            key = new TerminalKey(TerminalKey.SpecialKey.F5, mods);
                            return true;
                        case 17:
                        case 18:
                        case 19:
                        case 20:
                        case 21:
                            key = new TerminalKey(TerminalKey.SpecialKey.F6 + (unicodeKeyCode.Value - 17), mods);
                            return true;
                        case 23:
                        case 24:
                            key = new TerminalKey(TerminalKey.SpecialKey.F11 + (unicodeKeyCode.Value - 23), mods);
                            return true;
                        case 29:
                            key = new TerminalKey(TerminalKey.SpecialKey.F16, mods);
                            return true;
                        case 57427:
                            key = new TerminalKey(TerminalKey.SpecialKey.Begin, mods);
                            return true;
                        default: return Bail(out key);
                    }
                case 'A':
                case 'B':
                case 'C':
                case 'D':
                case 'E':
                case 'H':
                case 'F':
                case 'P':
                case 'Q':
                case 'R':
                case 'S':
                    TerminalKey.SpecialKey specialKey = finalCp switch
                    {
                        'A' => TerminalKey.SpecialKey.Up,
                        'B' => TerminalKey.SpecialKey.Down,
                        'C' => TerminalKey.SpecialKey.Right,
                        'D' => TerminalKey.SpecialKey.Left,
                        'E' => TerminalKey.SpecialKey.Begin,
                        'H' => TerminalKey.SpecialKey.Home,
                        'F' => TerminalKey.SpecialKey.End,
                        'P' => TerminalKey.SpecialKey.F1,
                        'Q' => TerminalKey.SpecialKey.F2,
                        // Note: R only used in legacy mode (prob to differentiate from cursor pos response)
                        // So we can't actually get CSI 1 ; x:y R
                        'R' => TerminalKey.SpecialKey.F3,
                        'S' => TerminalKey.SpecialKey.F4,
                    };
                    if (unicodeKeyCode is null)
                    {
                        key = new TerminalKey(specialKey);
                        return true;
                    }
                    if (unicodeKeyCode == 1)
                    {
                        key = new TerminalKey(specialKey, mods);
                        return true;
                    }
                    goto default;
                default: return Bail(out key);
            }

            bool Bail(out TerminalKey? key)
            {
                key = null;
                _reader.UnRead(CollectionsMarshal.AsSpan(_keyboardProtocolCodePoints));
                return false;
            }

            int ParseEscapeCode(List<Range> ranges, List<int?> list, TimeSpan timeout)
            {
                var cp = -1;
                var len = 0;
                do
                {
                    len = list.Count;
                    cp = ParseField(list, timeout);
                    ranges.Add(len..list.Count);
                } while (cp == ';');
                return cp;
            }

            int ParseField(List<int?> list, TimeSpan keyTimeout)
            {
                Span<char> storage = stackalloc char[8];
                var numberList = new SlimList<char>(storage);
                var cp = -1;
                for (cp = _reader.ReadCodePoint(keyTimeout); cp != -1; cp = _reader.ReadCodePoint(keyTimeout))
                {
                    for (; cp != -1; cp = _reader.ReadCodePoint(keyTimeout))
                    {
                        // meta
                        _keyboardProtocolCodePoints.Add(cp);

                        var rune = new Rune(cp);
                        if (!rune.IsAscii)
                        {
                            break;
                        }
                        var ch = (char)rune.Value;
                        if (!char.IsDigit(ch))
                        {
                            break;
                        }
                        numberList.Add(ch);
                    }
                    if (numberList.Length > 0)
                    {
                        list.Add(int.Parse(numberList.AsSpan()));
                    }
                    else
                    {
                        list.Add(null);
                    }
                    numberList.Clear();

                    if (cp != ':') break;
                }
                return cp;
            }
        }

        private readonly List<int> _mouseReadItems = new(24);

        private int ConsumeMouseEventParams(string mouseKeyType)
        {
            if (!HasMouseSupport) return 0;

            var timeout = TimeSpan.FromMilliseconds(1);
            // See https://www.invisible-island.net/xterm/ctlseqs/ctlseqs.html#:~:text=Mouse%20Tracking,-The
            switch (mouseKeyType)
            {
                case "\x1b[M":
                {
                    Span<byte> mouseBytes = stackalloc byte[3];
                    int readCount = _reader.ReadBytes(mouseBytes, timeout);
                    if (readCount < 3)
                    {
                        _reader.UnRead(mouseBytes[..readCount]);
                        return 0;
                    }

                    var buttonPos = (X: 0, Y: 0);
                    var buttonValue = mouseBytes[0] - 32;

                    XTermButtonInfo info = ParseXtermMouseButton(buttonValue);

                    buttonPos = (mouseBytes[1] - 32, mouseBytes[2] - 32);

                    if (buttonPos.X <= 0 || buttonPos.Y <= 0)
                    {
                        _reader.UnRead(mouseBytes);
                        return 0;
                    }

                    int amountAdded = EnqueueEventsFromInfo(info, buttonPos);

                    if (amountAdded > 0)
                    {
                        return amountAdded;
                    }

                    _reader.UnRead(mouseBytes);
                    return 0;
                }

                case "\x1b[<":
                {
                    _mouseReadItems.Clear();

                    var buttonValue = TakeNumber(_mouseReadItems, timeout);
                    if (buttonValue is null)
                    {
                        _reader.UnRead(CollectionsMarshal.AsSpan(_mouseReadItems));
                        return 0;
                    }

                    var info = ParseXtermMouseButton(buttonValue.Value);
                    (int? X, int? Y) buttonPos = (null, null);

                    int semi = -1;

                    semi = _reader.ReadCodePoint(timeout);
                    if (semi != -1)
                        _mouseReadItems.Add(semi);

                    if (semi != ';')
                    {
                        _reader.UnRead(CollectionsMarshal.AsSpan(_mouseReadItems));
                        return 0;
                    }

                    buttonPos.X = TakeNumber(_mouseReadItems, timeout);
                    if (buttonPos.X is null)
                    {
                        _reader.UnRead(CollectionsMarshal.AsSpan(_mouseReadItems));
                        return 0;
                    }
                    if (buttonPos.X == 0) buttonPos.X = 1;

                    semi = _reader.ReadCodePoint(timeout);
                    if (semi != -1)
                        _mouseReadItems.Add(semi);

                    if (semi != ';')
                    {
                        _reader.UnRead(CollectionsMarshal.AsSpan(_mouseReadItems));
                        return 0;
                    }

                    buttonPos.Y = TakeNumber(_mouseReadItems, timeout);
                    if (buttonPos.Y is null)
                    {
                        _reader.UnRead(CollectionsMarshal.AsSpan(_mouseReadItems));
                        return 0;
                    }
                    if (buttonPos.Y == 0) buttonPos.Y = 1;

                    var lastM = _reader.ReadCodePoint(timeout);

                    if (lastM != -1)
                        _mouseReadItems.Add(lastM);

                    if (lastM == 'M')
                    {
                        // If we got a real button, keep it as a click
                        // If we got a "release" button, keep it as a release
                    }
                    else if (lastM == 'm')
                    {
                        // We got a real button, change it to a release
                        info.ClickCount = -1;
                    }
                    else
                    {
                        _reader.UnRead(CollectionsMarshal.AsSpan(_mouseReadItems));
                        return 0;
                    }

                    var addCount = EnqueueEventsFromInfo(info, (buttonPos.X.Value, buttonPos.Y.Value));
                    if (addCount > 0)
                    {
                        return addCount;
                    }

                    _reader.UnRead(CollectionsMarshal.AsSpan(_mouseReadItems));
                    return 0;
                }

                case "\x1b[":
                {
                    _mouseReadItems.Clear();
                    var buttonValue = TakeNumber(_mouseReadItems, timeout);
                    if (buttonValue is null)
                    {
                        _reader.UnRead(CollectionsMarshal.AsSpan(_mouseReadItems));
                        return 0;
                    }

                    var info = ParseXtermMouseButton(buttonValue.Value);
                    (int? X, int? Y) buttonPos = (null, null);

                    int semi = -1;

                    semi = _reader.ReadCodePoint(timeout);
                    if (semi != -1)
                        _mouseReadItems.Add(semi);

                    if (semi != ';')
                    {
                        _reader.UnRead(CollectionsMarshal.AsSpan(_mouseReadItems));
                        return 0;
                    }

                    buttonPos.X = TakeNumber(_mouseReadItems, timeout);
                    if (buttonPos.X is null)
                    {
                        _reader.UnRead(CollectionsMarshal.AsSpan(_mouseReadItems));
                        return 0;
                    }
                    if (buttonPos.X == 0) buttonPos.X = 1;

                    semi = _reader.ReadCodePoint(timeout);
                    if (semi != -1)
                        _mouseReadItems.Add(semi);

                    if (semi != ';')
                    {
                        _reader.UnRead(CollectionsMarshal.AsSpan(_mouseReadItems));
                        return 0;
                    }

                    buttonPos.Y = TakeNumber(_mouseReadItems, timeout);
                    if (buttonPos.Y is null)
                    {
                        _reader.UnRead(CollectionsMarshal.AsSpan(_mouseReadItems));
                        return 0;
                    }
                    if (buttonPos.Y == 0) buttonPos.Y = 1;

                    var lastM = _reader.ReadCodePoint(timeout);

                    if (lastM != -1)
                        _mouseReadItems.Add(lastM);

                    if (lastM != 'M')
                    {
                        _reader.UnRead(CollectionsMarshal.AsSpan(_mouseReadItems));
                        return 0;
                    }

                    var addCount = EnqueueEventsFromInfo(info, (buttonPos.X.Value, buttonPos.Y.Value));
                    if (addCount > 0)
                    {
                        return addCount;
                    }

                    _reader.UnRead(CollectionsMarshal.AsSpan(_mouseReadItems));
                    return 0;
                }

                // TODO: Learn how to parse `xm` cap ...
                default:
                    throw new ArgumentException("Unsupported mouse key type", nameof(mouseKeyType));
            }


            int? TakeNumber(List<int> output, TimeSpan timeout)
            {
                var startLength = output.Count;
                Span<char> buffer = stackalloc char[8];
                var numbers = new SlimList<char>(buffer);
                while (true)
                {
                    int cp = _reader.ReadCodePoint(timeout);
                    if (cp == -1) break;
                    char ch = (char)cp;
                    if (cp > char.MaxValue || cp < char.MinValue || !char.IsDigit(ch))
                    {
                        _reader.UnRead(cp);
                        break;
                    }
                    numbers.Add(ch);
                    output.Add(cp);
                }
                if (int.TryParse(numbers.AsSpan(), out var i))
                {
                    return i;
                }
                _reader.UnRead(numbers.AsSpan());
                return null;
            }
        }

        private (int, int)? _lastMousePos;
        private int EnqueueEventsFromInfo(in XTermButtonInfo info, (int X, int Y) buttonPos)
        {
            int amountAdded = 0;

            // TODO: Maybe check if it's possible for a release to happen only with a motion event?
            // (probably not)
            if (info.IsMotion)
            {
                _lastMousePos ??= buttonPos;
                EnqueueEvent(new TerminalMouseMoveEvent(this, _lastMousePos.Value, buttonPos, info.Mods));
                amountAdded++;
            }
            else
            {
                if (info.Buttons != TerminalMouseButtons.None)
                {
                    EnqueueEvent(new TerminalMouseClickEvent(this, buttonPos, info.Buttons, info.Mods, info.ClickCount));
                    amountAdded++;
                }
                if (info.ScrollDelta != 0)
                {
                    EnqueueEvent(new TerminalMouseScrollEvent(this, buttonPos, info.ScrollDelta, info.ScrollDir, info.Mods));
                    amountAdded++;
                }
            }
            _lastMousePos = buttonPos;
            return amountAdded;
        }

        private struct XTermButtonInfo
        {
            public TerminalMouseButtons Buttons = TerminalMouseButtons.None;
            public TerminalKeyModifiers Mods = TerminalKeyModifiers.None;
            public int ClickCount = 0;
            public TerminalScrollDirection ScrollDir = TerminalScrollDirection.Vertical;
            public int ScrollDelta = 0;
            public bool IsMotion = false;

            public XTermButtonInfo() { }
        }

        private XTermButtonInfo ParseXtermMouseButton(int buttonValue)
        {
            XTermButtonInfo info = default;

            var buttonBits = buttonValue & 0b11;

            // TODO: Implement buttons 6 & 7
            // But then also think about how to send release events
            if ((buttonValue & 128) != 0)
            {
                // Back and forward buttons map to button 8 & 9 (?)
                // Unsure what 6 and 7 map to?
                if (buttonBits == 0)
                {
                    info.Buttons = TerminalMouseButtons.Mouse4;
                    info.ClickCount = 1;
                }
                else if (buttonBits == 1)
                {
                    info.Buttons = TerminalMouseButtons.Mouse5;
                    info.ClickCount = 1;
                }
            }
            else if ((buttonValue & 64) != 0)
            {
                if (buttonBits == 0)
                {
                    info.ScrollDir = TerminalScrollDirection.Vertical;
                    info.ScrollDelta = 1;
                }
                else if (buttonBits == 1)
                {
                    info.ScrollDelta = -1;
                    info.ScrollDir = TerminalScrollDirection.Vertical;
                }
                else if (buttonBits == 2)
                {
                    info.ScrollDelta = 1;
                    info.ScrollDir = TerminalScrollDirection.Horizontal;
                }
                else if (buttonBits == 3)
                {
                    info.ScrollDelta = -1;
                    info.ScrollDir = TerminalScrollDirection.Horizontal;
                }
            }
            else
            {
                if (buttonBits == 0) info.Buttons = TerminalMouseButtons.MouseLeft;
                else if (buttonBits == 1) info.Buttons = TerminalMouseButtons.MouseMiddle;
                else if (buttonBits == 2) info.Buttons = TerminalMouseButtons.MouseRight;
                if (buttonBits == 3)
                {
                    info.ClickCount = -1;
                    // FIXME: Instead of assuming all have been released, keep track of what's currently held.
                    // Though even then, what's to be done about which one was released?
                    info.Buttons = TerminalMouseButtons.MouseLeft | TerminalMouseButtons.MouseRight | TerminalMouseButtons.MouseMiddle | TerminalMouseButtons.Mouse4 | TerminalMouseButtons.Mouse5;
                }
                else info.ClickCount = 1;
            }

            var buttonMods = buttonValue & 0b11100;
            if ((buttonMods & 4) != 0) info.Mods |= TerminalKeyModifiers.Shift;
            if ((buttonMods & 8) != 0) info.Mods |= TerminalKeyModifiers.Meta;
            if ((buttonMods & 16) != 0) info.Mods |= TerminalKeyModifiers.Control;
            info.Mods |= _keyboardProtocolMods;

            info.IsMotion = (buttonValue & 32) != 0;

            return info;
        }

        public sealed override void MoveCursor(int dx, int dy)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(dx, -32767, nameof(dx));
            ArgumentOutOfRangeException.ThrowIfLessThan(dy, -32767, nameof(dy));

            ArgumentOutOfRangeException.ThrowIfGreaterThan(dx, 32767, nameof(dx));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(dy, 32767, nameof(dy));

            var bounds = (Lines, Columns);
            if (_cursorPosCache is not null || dx < 0)
            {
                _cursorPosCache ??= RawCursorPositionQuery();
                if (_cursorPosCache is null) throw new InvalidOperationException();
                var origPos = _cursorPosCache.Value.X;
                var resultPos = origPos + dx;
                dx = Math.Clamp(resultPos, 1, bounds.Columns) - origPos;
            }
            if (_cursorPosCache is not null || dy < 0)
            {
                _cursorPosCache ??= RawCursorPositionQuery();
                if (_cursorPosCache is null) throw new InvalidOperationException();
                var origPos = _cursorPosCache.Value.Y;
                var resultPos = origPos - dy;
                dy = origPos - Math.Clamp(resultPos, 1, bounds.Lines);
            }

            if (Math.Abs(dx) == 1 && dy == 0)
            {
                var cap = dx > 0 ? GetStringCap("cuf1") : GetStringCap("cub1");
                if (cap is not null)
                {
                    WriteTermInfoString(cap);
                    if (_cursorPosCache is not null)
                        _cursorPosCache = (_cursorPosCache.Value.X + Math.Sign(dx), _cursorPosCache.Value.Y);
                    dx = 0;
                }
            }

            if (Math.Abs(dy) == 1 && dx == 0)
            {
                var cap = dy > 0 ? GetStringCap("cuu1") : GetStringCap("cud1");
                if (cap is not null)
                {
                    WriteTermInfoString(cap);
                    if (_cursorPosCache is not null)
                        _cursorPosCache = (_cursorPosCache.Value.X, _cursorPosCache.Value.Y + Math.Sign(-dy));
                    dy = 0;
                }
            }

            if (dx == 0 && dy == 0)
            {
                return;
            }

            if (GetStringCap("mrcup") is { } mrcup)
            {
                WriteTermInfoString(NCurses.Tiparm(mrcup, dy, dx));

                if (_cursorPosCache is not null)
                    _cursorPosCache = (_cursorPosCache.Value.X + dx, _cursorPosCache.Value.Y + -dy);
                return;
            }

            _scratchSb.Clear();

            if (dy > 0)
            {
                _scratchSb.Append(RequireStringCap("cuu1").Repeat(dy));
            }
            else if (dy < 0)
            {
                _scratchSb.Append(RequireStringCap("cud1").Repeat(-dy));
            }

            if (dx > 0)
            {
                _scratchSb.Append(RequireStringCap("cuf1").Repeat(dx));
            }
            else if (dx < 0)
            {
                _scratchSb.Append(RequireStringCap("cub1").Repeat(-dx));
            }

            if (_scratchSb.Length > 0)
            {
                WriteTermInfoString(_scratchSb.ToString());

                if (_cursorPosCache is not null)
                    _cursorPosCache = (_cursorPosCache.Value.X + dx, _cursorPosCache.Value.Y + -dy);
            }
            else
            {
                var (x, y) = CursorPosition;
                CursorPosition = (x + dx, y - dy);
            }
        }

        private static readonly (TerminalAnsiColor.AnsiColors, TerminalTrueColor)[] _defaultColorsMapping =
            Enum.GetValues<TerminalAnsiColor.AnsiColors>()
            .Select(static a => (a, TerminalAnsiColor.FromColor(a).NearestTrueColor))
            .ToArray();
        private TerminalAnsiColor? NearestAnsiFrom(TerminalAnsiColor col, TerminalAnsiColor.AnsiColors upperBound)
        {
            var asTrueColor = TrueColorFor(col);
            if (HasXtermOSC(4))
            {
                if (_defaultColors is null) QueryAnsiColors();

                if (_defaultColors.Length > 0)
                {
                    var defaultColorsArray = _defaultColors.Where(a => a.Item1 <= upperBound && a.Item1 != col.Color);
                    if (defaultColorsArray.Any())
                    {
                        return asTrueColor.NearestAnsiColorFrom(defaultColorsArray);
                    }
                }
            }

            var defaultMappingsArray = _defaultColorsMapping.Where(a => a.Item1 <= upperBound && a.Item1 != col.Color);
            if (!defaultMappingsArray.Any()) return null;
            return asTrueColor.NearestAnsiColorFrom(defaultMappingsArray);
        }
        private readonly StringBuilder _writeScratchSb = new();
        public override void Write(ColoredString str)
        {
            if (str.Text.Length <= 0) return;
            ITerminalColor fg = str.Color.Foreground, bg = str.Color.Background;
            var colorAttributes = str.Color.Attributes;

            if (!HasTrueColor)
            {
                if (fg is TerminalTrueColor fgtc)
                    fg = NearestAnsiColor(fgtc);
                if (bg is TerminalTrueColor bgtc)
                    bg = NearestAnsiColor(bgtc);
            }

            if ((ColorSupport & TerminalColorSupport.XTerm256) != TerminalColorSupport.XTerm256)
            {
                if (HasTrueColor && fg is TerminalAnsiColor afg) fg = TrueColorFor(afg);
                if (HasTrueColor && bg is TerminalAnsiColor abg) bg = TrueColorFor(abg);
                if (!HasTrueColor)
                {
                    // TODO: Map 256 to 16
                    if (fg is TerminalAnsiColor afg2 && afg2.Color > TerminalAnsiColor.AnsiColors.Color15)
                    {
                        var newFg = NearestAnsiFrom(afg2, TerminalAnsiColor.AnsiColors.Color15);
                        fg = newFg ?? ITerminalColor.DefaultColor;
                    }
                    if (bg is TerminalAnsiColor abg2 && abg2.Color > TerminalAnsiColor.AnsiColors.Color15)
                    {
                        var newBg = NearestAnsiFrom(abg2, TerminalAnsiColor.AnsiColors.Color15);
                        bg = newBg ?? ITerminalColor.DefaultColor;
                    }
                }
            }

            if ((ColorSupport & TerminalColorSupport.Ansi16) != TerminalColorSupport.Ansi16)
            {
                if (HasTrueColor && fg is TerminalAnsiColor afg) fg = TrueColorFor(afg);
                if (HasTrueColor && bg is TerminalAnsiColor abg) bg = TrueColorFor(abg);
                if (!HasTrueColor)
                {
                    // TODO: Map 16 to 8
                    if (fg is TerminalAnsiColor afg2 && afg2.Color > TerminalAnsiColor.AnsiColors.Color8)
                    {
                        var newFg = TerminalAnsiColor.FromColor(afg2.Color - 8);
                        fg = newFg ?? ITerminalColor.DefaultColor;
                        colorAttributes |= TerminalCellColor.CellAttributes.Bold;
                    }
                    if (bg is TerminalAnsiColor abg2 && abg2.Color > TerminalAnsiColor.AnsiColors.Color8)
                    {
                        var newBg = TerminalAnsiColor.FromColor(abg2.Color - 8);
                        bg = newBg ?? ITerminalColor.DefaultColor;
                        colorAttributes |= TerminalCellColor.CellAttributes.Bold;
                    }
                }
            }

            if ((ColorSupport & TerminalColorSupport.Ansi8) != TerminalColorSupport.Ansi8)
            {
                if (HasTrueColor && fg is TerminalAnsiColor afg) fg = TrueColorFor(afg);
                if (HasTrueColor && bg is TerminalAnsiColor abg) bg = TrueColorFor(abg);
                if (!HasTrueColor)
                {
                    fg = ITerminalColor.DefaultColor;
                    bg = ITerminalColor.DefaultColor;
                }
            }

            _writeScratchSb.Clear();

            var usableAttributes = TerminalCellColor.CellAttributes.Underline
                                   | TerminalCellColor.CellAttributes.Reverse
                                   | TerminalCellColor.CellAttributes.Bold;

            if (colorAttributes != TerminalCellColor.CellAttributes.None)
            {
                // https://www.man7.org/linux/man-pages/man5/terminfo.5.html
                // The 9 parameters are, in order:
                // standout, underline, reverse, blink, dim, bold, blank, protect, alternate character set.
                // Not all modes need be supported by sgr, only those
                // for which corresponding separate attribute commands exist.
                if ((str.Color.Attributes & usableAttributes) != 0x0)
                {
                    _writeScratchSb.Append(NCurses.Tiparm(RequireStringCap("sgr"),
                        0,
                        colorAttributes.HasFlag(TerminalCellColor.CellAttributes.Underline) ? 1 : 0,
                        colorAttributes.HasFlag(TerminalCellColor.CellAttributes.Reverse) ? 1 : 0,
                        0,
                        0,
                        colorAttributes.HasFlag(TerminalCellColor.CellAttributes.Bold) ? 1 : 0,
                        0,
                        0,
                        0
                        ));

                }
            }

            switch (fg)
            {
                case TerminalTrueColor tc:
                    _writeScratchSb.Append(GetTrueColorForegroundString(tc));
                    break;
                case TerminalAnsiColor ac:
                    _writeScratchSb.Append(NCurses.Tiparm(RequireStringCap("setaf"), (int)ac.Color));
                    break;
                case ITerminalColor.TerminalDefaultColor:
                    break;
                default:
                    throw new NotImplementedException();
            }

            switch (bg)
            {
                case TerminalTrueColor tc:
                    _writeScratchSb.Append(GetTrueColorBackgroundString(tc));
                    break;
                case TerminalAnsiColor ac:
                    _writeScratchSb.Append(NCurses.Tiparm(RequireStringCap("setab"), (int)ac.Color));
                    break;
                case ITerminalColor.TerminalDefaultColor:
                    break;
                default:
                    throw new NotImplementedException();
            }
            WriteTermInfoString(_writeScratchSb.ToString());
            _writeScratchSb.Clear();

            WriteRaw(str.Text);

            if ((colorAttributes & usableAttributes) != 0x0)
            {
                _writeScratchSb.Append(NCurses.Tiparm(RequireStringCap("sgr"), 0, 0, 0, 0, 0, 0, 0, 0, 0));
            }
            if (fg is not ITerminalColor.TerminalDefaultColor || bg is not ITerminalColor.TerminalDefaultColor)
            {
                if (GetStringCap("sgr0") is { } sgr0)
                {
                    _writeScratchSb.Append(sgr0);
                }
                else
                {
                    if (GetBoolCap("AX")) _writeScratchSb.Append(RequireStringCap("op"));
                }
            }
            WriteTermInfoString(_writeScratchSb.ToString());
            _writeScratchSb.Clear();

            if (_cursorPosCache is { } cursorPosCache)
            {
                var cols = Columns;
                cursorPosCache.X--;
                var sp = str.Text.AsSpan();
                bool shouldSet = true;
                for (int i = 0, len = 0; shouldSet && i < str.Text.Length; i += len)
                {
                    len = StringInfo.GetNextTextElementLength(str.Text, i);
                    var g = sp.Slice(i, len);
                    if (g.Length > 1 || char.IsSurrogate(g[0]))
                    {
                        shouldSet = false;
                        break;
                    }
                    if (g.Length == 1)
                    {
                        char ch = g[0];
                        if (ch == '\r')
                        {
                            cursorPosCache.X = 0;
                            continue;
                        }
                        else if (ch == '\n')
                        {
                            cursorPosCache.Y++;
                            continue;
                        }
                        else if (ch < 0x20 || ch >= 0x7f)
                        {
                            shouldSet = false;
                            break;
                        }
                    }

                    cursorPosCache.X++;
                    if (cursorPosCache.X == cols)
                    {
                        cursorPosCache.Y++;
                        cursorPosCache.X = 0;
                    }
                }

                if (shouldSet)
                {
                    cursorPosCache.X++;
                    cursorPosCache.Y = Math.Min(Lines, cursorPosCache.Y);
                    // For some reason, \n behaves weirdly at the very right edge of the screen
                    if (cursorPosCache.X > 1)
                        _cursorPosCache = cursorPosCache;
                    else _cursorPosCache = null;
                }
                else
                {
                    _cursorPosCache = null;
                }
            }
        }

        // public override void Write(IEnumerable<ColoredString> strs)
        // {
        //     throw new NotImplementedException();
        // }

        public override bool CanPollEvent(Type t)
        {
            if (t.IsAssignableTo(typeof(TerminalKeyEvent)))
            {
                return true;
            }
            if (t.IsAssignableTo(typeof(ITerminalMouseEvent)))
            {
                return HasMouseSupport;
            }
            return false;
        }

        protected void WriteRaw(string s)
        {
            if (s.Length <= 0) return;
            _writer.Write(s);
        }

        protected void WriteTermInfoString(string s)
        {
            if (s.Length <= 0) return;
            _writer.WriteTermInfoString(s);
        }

        // TODO: CACHE
        private bool HasRGBCap()
        {
            if (GetUserBoolCap("RGB")) return true;
            if (GetUserNumCap("RGB") > -1) return true;
            if (GetUserStringCap("RGB") is not null) return true;
            return false;
        }

        // TODO: CACHE
        private (int R, int G, int B) GetRGBLimits()
        {
            (int, int, int)? lim = null;

            if (lim is null)
            {
                if (GetUserStringCap("RGB") is { } rgbStr)
                {
                    var parts = rgbStr.Split('/');
                    if (parts.Length >= 3)
                    {
                        if (int.TryParse(parts[0].Trim(), out var rNum)
                            && int.TryParse(parts[1].Trim(), out var gNum)
                            && int.TryParse(parts[2].Trim(), out var bNum))
                        {
                            rNum = Math.Max(0, rNum);
                            gNum = Math.Max(0, gNum);
                            bNum = Math.Max(0, bNum);
                            lim = (rNum, gNum, bNum);
                        }
                    }
                }
            }

            if (lim is null)
            {
                if (GetUserBoolCap("RGB"))
                {
                    var cols = RequireNumCap("colors");
                    var split = (cols + 2) / 3;
                    var splitRem = (cols + 2) % 3;
                    lim = (split + splitRem, split, split);
                }
            }

            if (lim is null)
            {
                var rgbNum = GetUserNumCap("RGB");
                if (rgbNum > -1)
                {
                    lim = (rgbNum, rgbNum, rgbNum);
                }
            }

            if (lim is not null)
            {
                return ((1 << lim.Value.Item1) - 1, (1 << lim.Value.Item2) - 1, (1 << lim.Value.Item3) - 1);
            }

            return HasTrueColor ? (255, 255, 255) : (1, 1, 1);
        }

        private string GetTrueColorForegroundString(TerminalTrueColor col)
        {
            return GetTrueColorForegroundString(col.R, col.G, col.B);
        }

        private string GetTrueColorBackgroundString(TerminalTrueColor col)
        {
            return GetTrueColorBackgroundString(col.R, col.G, col.B);
        }

        private string GetTrueColorForegroundString(byte r, byte g, byte b)
        {
            var cap = GetStringCap("setrgbf");
            if (cap is not null)
            {
                return NCurses.Tiparm(cap, r, g, b);
            }

            if (HasRGBCap())
            {
                cap = RequireStringCap("setaf");
                var rgbLimits = GetRGBLimits();
                return NCurses.Tiparm(cap, r * rgbLimits.R / 255, g * rgbLimits.G / 255, b * rgbLimits.B / 255);
            }

            _scratchSb.Clear();
            _scratchSb.Append("\x1b[38;2;");
            _scratchSb.Append(r);
            _scratchSb.Append(';');
            _scratchSb.Append(g);
            _scratchSb.Append(';');
            _scratchSb.Append(b);
            _scratchSb.Append('m');
            return _scratchSb.ToString();
        }

        private string GetTrueColorBackgroundString(byte r, byte g, byte b)
        {
            var cap = GetStringCap("setrgbb");
            if (cap is not null)
            {
                return NCurses.Tiparm(cap, r, g, b);
            }

            if (HasRGBCap())
            {
                cap = RequireStringCap("setab");
                var rgbLimits = GetRGBLimits();
                return NCurses.Tiparm(cap, r * rgbLimits.R / 255, g * rgbLimits.G / 255, b * rgbLimits.B / 255);
            }

            _scratchSb.Clear();
            _scratchSb.Append("\x1b[48;2;");
            _scratchSb.Append(r);
            _scratchSb.Append(';');
            _scratchSb.Append(g);
            _scratchSb.Append(';');
            _scratchSb.Append(b);
            _scratchSb.Append('m');
            return _scratchSb.ToString();
        }

        private readonly Dictionary<string, TerminalKey> _termInfoKeys = new();
        private int _minTermInfoKeyLength = 0;
        private int _maxTermInfoKeyLength = 0;
        private void LoadKeys()
        {
            TryInsertKey("kcub1", TerminalKey.SpecialKey.Left);
            TryInsertKey("kcuf1", TerminalKey.SpecialKey.Right);
            TryInsertKey("kcuu1", TerminalKey.SpecialKey.Up);
            TryInsertKey("kcud1", TerminalKey.SpecialKey.Down);
            TryInsertKey("khome", TerminalKey.SpecialKey.Home);
            TryInsertKey("kend", TerminalKey.SpecialKey.End);
            TryInsertKey("knp", TerminalKey.SpecialKey.PageDown);
            TryInsertKey("kpp", TerminalKey.SpecialKey.PageUp);
            TryInsertKey("kdch1", TerminalKey.SpecialKey.Delete);
            TryInsertKey("kich1", TerminalKey.SpecialKey.Insert);
            TryInsertKey("kprt", TerminalKey.SpecialKey.PrintScreen);
            TryInsertKey("kf0", TerminalKey.SpecialKey.F0);
            TryInsertKey("kf1", TerminalKey.SpecialKey.F1);
            TryInsertKey("kf2", TerminalKey.SpecialKey.F2);
            TryInsertKey("kf3", TerminalKey.SpecialKey.F3);
            TryInsertKey("kf4", TerminalKey.SpecialKey.F4);
            TryInsertKey("kf5", TerminalKey.SpecialKey.F5);
            TryInsertKey("kf6", TerminalKey.SpecialKey.F6);
            TryInsertKey("kf7", TerminalKey.SpecialKey.F7);
            TryInsertKey("kf8", TerminalKey.SpecialKey.F8);
            TryInsertKey("kf9", TerminalKey.SpecialKey.F9);
            TryInsertKey("kf10", TerminalKey.SpecialKey.F10);
            TryInsertKey("kf11", TerminalKey.SpecialKey.F11);
            TryInsertKey("kf12", TerminalKey.SpecialKey.F12);
            TryInsertKey("kf13", TerminalKey.SpecialKey.F13);
            TryInsertKey("kf14", TerminalKey.SpecialKey.F14);
            TryInsertKey("kf15", TerminalKey.SpecialKey.F15);
            TryInsertKey("kf16", TerminalKey.SpecialKey.F16);
            TryInsertKey("kf17", TerminalKey.SpecialKey.F17);
            TryInsertKey("kf18", TerminalKey.SpecialKey.F18);
            TryInsertKey("kf19", TerminalKey.SpecialKey.F19);
            TryInsertKey("kf20", TerminalKey.SpecialKey.F20);
            TryInsertKey("kf21", TerminalKey.SpecialKey.F21);
            TryInsertKey("kf22", TerminalKey.SpecialKey.F22);
            TryInsertKey("kf23", TerminalKey.SpecialKey.F23);
            TryInsertKey("kf24", TerminalKey.SpecialKey.F24);
            TryInsertKey("kDC", new TerminalKey(TerminalKey.SpecialKey.Delete, TerminalKeyModifiers.Shift));
            TryInsertKey("kEND", new TerminalKey(TerminalKey.SpecialKey.End, TerminalKeyModifiers.Shift));
            TryInsertKey("kHOM", new TerminalKey(TerminalKey.SpecialKey.Home, TerminalKeyModifiers.Shift));
            TryInsertKey("kLFT", new TerminalKey(TerminalKey.SpecialKey.Left, TerminalKeyModifiers.Shift));
            TryInsertKey("kRIT", new TerminalKey(TerminalKey.SpecialKey.Right, TerminalKeyModifiers.Shift));
            if (!HasKeyboardProtocol)
                EnableControlMappings();
            _ = GetMouseKeysSorted();
            if (_termInfoKeys.Count > 0)
            {
                _minTermInfoKeyLength = _termInfoKeys.Min(static a => a.Key.Length);
                _maxTermInfoKeyLength = _termInfoKeys.Max(static a => a.Key.Length);
            }
        }

        private void EnableControlMappings()
        {
            // Only insert if high probability of being right
            // For example, if we get \x00 its unlikely that they pressed a NUL key, they probably hit C-Space
            // On the other hand, they could have indeed pressed escape for \x1b instead of C-[ or C-3
            _termInfoKeys["\x00"] = new TerminalKey(' ', TerminalKeyModifiers.Control);
            _termInfoKeys["\x1C"] = new TerminalKey('4', TerminalKeyModifiers.Control);
            _termInfoKeys["\x1D"] = new TerminalKey('5', TerminalKeyModifiers.Control);
            _termInfoKeys["\x1E"] = new TerminalKey('6', TerminalKeyModifiers.Control);
            _termInfoKeys["\x1F"] = new TerminalKey('7', TerminalKeyModifiers.Control);

            for (int i = 0x1; i <= 0x1A; i++)
            {
                if (i is 0x0d or 0x09 or 0x08) continue;
                string str = new([(char)i]);
                _termInfoKeys[str] = new TerminalKey('a' + (i - 1), TerminalKeyModifiers.Control);
            }

            _termInfoKeys["\x1c"] = new TerminalKey('\\', TerminalKeyModifiers.Control);
            _termInfoKeys["\x1d"] = new TerminalKey(']', TerminalKeyModifiers.Control);
            _termInfoKeys["\x1e"] = new TerminalKey('^', TerminalKeyModifiers.Control);
            _termInfoKeys["\x1f"] = new TerminalKey('/', TerminalKeyModifiers.Control);
            _termInfoKeys["\x1b[Z"] = new TerminalKey('\t', TerminalKeyModifiers.Shift);
            _termInfoKeys["\x08"] = new TerminalKey('\x7f', TerminalKeyModifiers.Control);
        }

        private bool TryInsertKey(string name, TerminalKey key, string? defaultValue = null)
        {
            var str = GetStringCap(name) ?? defaultValue;
            if (str is null) return false;
            _termInfoKeys[str] = key;
            return true;
        }

        private bool TryInsertKey(string name, TerminalKey.SpecialKey special, string? defaultValue = null)
            => TryInsertKey(name, new TerminalKey(special), defaultValue);

        private (int X, int Y)? _saveCursor;

        // Used to use sc/rc, but we can't the new cursor position when resetting with rc
        public void SaveCursor()
        {
            Flush();
            _saveCursor = CursorPosition;
        }

        public void RestoreCursor()
        {
            if (_saveCursor.HasValue)
            {
                CursorPosition = _saveCursor.Value;
            }
        }

        protected internal virtual string? GetUserStringCap(string s)
        {
            return NCurses.Tigetuserstr(s);
        }

        protected internal virtual bool GetUserBoolCap(string s)
        {
            return NCurses.Tigetuserflag(s);
        }

        /// <summary>
        /// Throws if the cap does not exist (i.e. invalid cap name)
        /// </summary>
        /// <param name="s">cap name</param>
        /// <returns> -1 if canceled or absent, >=0 for existant.</returns>
        protected internal virtual int GetUserNumCap(string s)
        {
            return NCurses.Tigetusernum(s);
        }

        protected internal virtual bool GetBoolCap(string s)
        {
            return NCurses.Tigetflag(s);
        }

        protected void RequireBoolCap(string s)
        {
            var cap = GetBoolCap(s);
            if (!cap) throw new ArgumentNullException(nameof(s), $"Expected boolean capability {s}");
        }

        protected internal virtual string? GetStringCap(string s)
        {
            return NCurses.Tigetstr(s);
        }

        protected string RequireStringCap(string s)
        {
            var cap = GetStringCap(s);
            ArgumentNullException.ThrowIfNull(cap);
            return cap;
        }

        /// <summary>
        /// Throws if the cap does not exist (i.e. invalid cap name)
        /// </summary>
        /// <param name="s">cap name</param>
        /// <returns> -1 if canceled or absent, >=0 for existant.</returns>
        protected internal virtual int GetNumCap(string s)
        {
            return NCurses.Tigetnum(s);
        }

        /// <summary>
        /// Make sure the (numeric) capability exist and is present.
        /// </summary>
        /// <param name="s">cap name</param>
        /// <returns>A cap with <c>value >= 0</c>.</returns>
        protected int RequireNumCap(string s)
        {
            var cap = GetNumCap(s);
            if (cap < 0) throw new ArgumentNullException(nameof(s), $"Expected numeric capability {s}");
            return cap;
        }

        /// <summary>
        /// Returns the default terminal instance using the terminal type provided by the $TERM environment variable.
        /// The terminal is hooked to STDIN and STDOUT.
        /// </summary>
        public static NCursesTerminal Default => DefaultNCursesTerminal.Instance;

        /// <summary>
        /// Returns a terminal instance using the provided terminal type.
        /// The terminal is hooked to STDIN and STDOUT.
        /// The returned terminal is NOT guaranteed to be unique if given the same terminal type.
        /// In particular, it may match <see cref="Default"/> if the terminal type matches.
        /// </summary>
        /// <param name="term">The terminal type (e.g. $TERM environment veriable).</param>
        /// <returns>A (possibly reused) terminal instance with the provided terminal type.</returns>
        public static NCursesTerminal DefaultWith(string term) => DefaultNCursesTerminal.WithTerm(term);
    }

    /// <summary>
    /// Represents an <see cref="NCursesTerminal"/> connected to the default stdin and stdout (FD 0 &amp; 1).
    /// </summary>
    internal sealed class DefaultNCursesTerminal : NCursesTerminal
    {
        private static nint _currentScreenPtr = nint.Zero;

        private bool? _isDefaultTermCache;
        private bool IsDefaultTerm
        {
            get
            {
                if (_isDefaultTermCache is not null) return _isDefaultTermCache.Value;

                var env = Environment.GetEnvironmentVariable("TERM");
                _isDefaultTermCache = env is not null && TermName == env;
                return _isDefaultTermCache.Value;
            }
        }

        public sealed override int Lines
        {
            get
            {
                // Flush();
                if (OldWindowSize is null) EnsureWindowSizeInit();
                else if (ShouldPollWindowSize)
                {
                    ShouldPollWindowSize = false;
                    PollWindowSize();
                }
                return OldWindowSize!.Value.Lines;
            }
        }

        public sealed override int Columns
        {
            get
            {
                // Flush();
                if (OldWindowSize is null) EnsureWindowSizeInit();
                else if (ShouldPollWindowSize)
                {
                    ShouldPollWindowSize = false;
                    PollWindowSize();
                }
                return OldWindowSize!.Value.Columns;
            }
        }

        private const int STDIN_FD = 0;
        private const int STDOUT_FD = 1;
        private const int STDERR_FD = 2;

        private DefaultNCursesTerminal() : this(Environment.GetEnvironmentVariable("TERM") ?? "xterm-256color")
        {
        }

        // private DefaultNCursesTerminal(string term) : base(new FileStream(new SafeFileHandle(STDIN_FD, false), FileAccess.Read), Console.OpenStandardOutput(), term)
        private DefaultNCursesTerminal(string term)
            : base(new FileStream(new SafeFileHandle(STDIN_FD, false), FileAccess.Read), new FileStream(new SafeFileHandle(STDOUT_FD, false), FileAccess.Write), term)
        {
        }

        ~DefaultNCursesTerminal()
        {
            if (!_didDispose)
            {
                var v = DisposeAsync();
                v.Wait();
            }
        }

        [MemberNotNull(nameof(OldWindowSize))]
        private void EnsureWindowSizeInit()
        {
            if (OldWindowSize is not null) return;

            var winSize = new LibcInterop.WinSize();
            LibcInterop.ioctl(STDOUT_FD, new(LibcInterop.TIOCGWINSZ), ref winSize);

            OldWindowSize = (winSize.ws_row, winSize.ws_col);
        }

        protected override void PollWindowSize()
        {
            var newSize = (Console.WindowHeight, Console.WindowWidth);
            var wasNull = OldWindowSize is null;
            OldWindowSize ??= newSize;
            if (wasNull || newSize != OldWindowSize)
                EnqueueEvent(new TerminalWindowEvent(this, OldWindowSize.Value, newSize));
            OldWindowSize = newSize;
        }

        private PosixSignalRegistration? _signalReg;
        private void RegisterSignalHandler()
        {
            if (!OperatingSystem.IsLinux())
                return;

            Debug.Assert(_signalReg is null);
            _signalReg = PosixSignalRegistration.Create(PosixSignal.SIGWINCH, context => ShouldPollWindowSize = true);
        }

        private void UnregisterSignalHandler()
        {
            _signalReg?.Dispose();
            _signalReg = null;
        }

        private LibcInterop.Termios _originalTermios = new();
        private volatile bool _didInit = false;
        private bool _didDispose = false;
        public override void Init()
        {
            if (_didInit) return;

            if (_currentScreenPtr != nint.Zero)
            {
                throw new InvalidOperationException("Cannot currently create multiple simultaneous instances of NCursesTerminal");
            }

            if (LibcInterop.isatty(STDOUT_FD) == 0)
            {
                throw new InvalidOperationException("Stdout must be a TTY to initialize the terminal");
            }

            if (LibcInterop.isatty(STDIN_FD) == 0)
            {
                // throw new InvalidOperationException("Stdin must be a TTY to initialize the terminal");
                Console.Error.WriteLine("warning: Stdin is not connected to a TTY");
                Thread.Sleep(TimeSpan.FromMilliseconds(1000));
            }

            NCurses.UseExtendedNames(true);

            _didInit = true;

            NCurses.Setupterm(TermName, STDOUT_FD);

            LibcInterop.tcgetattr(STDOUT_FD, ref _originalTermios);
            var newTermios = _originalTermios;

            LibcInterop.cfmakeraw(ref newTermios);

            if (false)
            {
                newTermios.c_iflag &= ~(LibcInterop.InputFlags.IGNBRK | LibcInterop.InputFlags.BRKINT
                        | LibcInterop.InputFlags.PARMRK | LibcInterop.InputFlags.IXON | LibcInterop.InputFlags.ICRNL
                        | LibcInterop.InputFlags.IGNCR);
                newTermios.c_iflag |= LibcInterop.InputFlags.IUTF8; // IUTF8 not in POSIX

                newTermios.c_oflag &= ~(LibcInterop.OutputFlags.OPOST);
                newTermios.c_oflag |= (0);

                newTermios.c_cflag &= ~(LibcInterop.ControlFlags.PARENB | LibcInterop.ControlFlags.CSIZE);
                newTermios.c_cflag |= (LibcInterop.ControlFlags.CS8);

                newTermios.c_lflag &= ~(LibcInterop.LocalFlags.ISIG | LibcInterop.LocalFlags.ICANON
                                        | LibcInterop.LocalFlags.ECHO | LibcInterop.LocalFlags.ECHOE
                                        | LibcInterop.LocalFlags.ECHOK | LibcInterop.LocalFlags.ECHONL
                                        | LibcInterop.LocalFlags.ECHOCTL | LibcInterop.LocalFlags.ECHOPRT
                                        | LibcInterop.LocalFlags.ECHOKE | LibcInterop.LocalFlags.IEXTEN);
                newTermios.c_lflag |= (0);
                unsafe
                {
                    newTermios.c_cc[LibcInterop.Cc.VMIN] = 1;
                    newTermios.c_cc[LibcInterop.Cc.VTIME] = 0;
                }
            }

            // LibcInterop.tcsetattr(STDOUT_FD, LibcInterop.TcSetAttrAction.TCSADRAIN, in newTermios);
            LibcInterop.tcsetattr(STDOUT_FD, LibcInterop.TcSetAttrAction.TCSANOW, in newTermios);

            RegisterSignalHandler();

            EnsureWindowSizeInit();

            _currentScreenPtr = _screenPtr;

            base.Init();

            _cursorPosCache = RawCursorPositionQuery();
        }

        public override async ValueTask DisposeAsync()
        {
            if (!_didInit) return;
            if (_didDispose) return;

            if (_currentScreenPtr != _screenPtr)
            {
                throw new InvalidOperationException("Cannot dispose when NCursesTerminal is not current");
            }

            _didDispose = true;

            await base.DisposeAsync();

            _currentScreenPtr = nint.Zero;

            UnregisterSignalHandler();

            LibcInterop.tcsetattr(STDOUT_FD, LibcInterop.TcSetAttrAction.TCSAFLUSH, in _originalTermios);

            if (Equals(this, _instance))
                _instance = null;
        }

        public override bool CanPollEvent(Type t)
        {
            if (t.IsAssignableTo(typeof(TerminalWindowEvent)))
            {
                return _signalReg is not null;
            }
            return base.CanPollEvent(t);
        }

        protected internal override string? GetUserStringCap(string s)
        {
            EnsureTerminalCurrent();
            return base.GetUserStringCap(s);
        }

        protected internal override bool GetUserBoolCap(string s)
        {
            EnsureTerminalCurrent();
            return base.GetUserBoolCap(s);
        }

        protected internal override int GetUserNumCap(string s)
        {
            EnsureTerminalCurrent();
            return base.GetUserNumCap(s);
        }

        protected internal override string? GetStringCap(string s)
        {
            EnsureTerminalCurrent();
            return base.GetStringCap(s);
        }

        protected internal override bool GetBoolCap(string s)
        {
            EnsureTerminalCurrent();
            return base.GetBoolCap(s);
        }

        protected internal override int GetNumCap(string s)
        {
            EnsureTerminalCurrent();
            return base.GetNumCap(s);
        }

        private void EnsureTerminalCurrent()
        {
            if (_currentScreenPtr != _screenPtr)
            {
                // NOTE(Ark1409): Expect other work to be done here
                _currentScreenPtr = _screenPtr;
                throw new InvalidOperationException("Cannot currently create multiple simultaneous instances of NCursesTerminal");
            }
        }

        private static DefaultNCursesTerminal? _instance = null;
        public static DefaultNCursesTerminal Instance => _instance ??= new();

        internal static DefaultNCursesTerminal WithTerm(string term)
        {
            var env = Environment.GetEnvironmentVariable("TERM");
            if (env is not null && env == term) return Instance;
            return new(term);
        }
    }
}
