// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace Sphynx.Client.Tui.Terminal
{
    public abstract class WindowsTerminal : Terminal, IStreamTerminal
    {
        private Stream Stream { get; }

        protected TextWriter Writer { get; }
        protected TextReader Reader { get; }
        private readonly StringBuilder _pendingChars = new();

        public override bool HasGraphicsProtocol => false;

        public override (int x, int y) CursorPosition
        {
            get
            {
                Writer.Write("\e[6n");
                Flush();

                int row = 0;
                int col = 0;

                int? startIndex = null;

                var currentIndex = _pendingChars.Length;

                for (int ch = Reader.Read(), lastChar = 0x0; ch != -1; lastChar = ch, ch = Reader.Read(), currentIndex++)
                {
                    _pendingChars.Append((char)ch);
                    if (ch == '[' && lastChar == '\e')
                    {
                        Debug.Assert(currentIndex > 0);
                        startIndex = currentIndex - 1;
                    }
                    else if (startIndex is not null && ch == 'R')
                    {
                        string body = _pendingChars.ToString(startIndex.Value, _pendingChars.Length - startIndex.Value - 1);
                        var parts = body.Split(';');
                        int r = 0, c = 0;

                        if (parts.Length == 2)
                        {
                            if (parts[0].Length == 0) r = 1;
                            else if (!int.TryParse(parts[0], out r)) r = 0;

                            if (parts[1].Length == 0) c = 1;
                            else if (!int.TryParse(parts[1], out c)) c = 0;
                        }
                        else if (body.Length == 0)
                        {
                            r = c = 1;
                        }

                        if (r > 0 && c > 0)
                        {
                            Debug.Assert(r > 0 && r <= 32767);
                            Debug.Assert(c > 0 && c <= 32767);
                            row = r;
                            col = c;
                            break;
                        }

                        startIndex = null;
                    }
                }

                if (startIndex is null) throw new IOException("Terminal input stream closed unexpectedly");

                _ = _pendingChars.Remove(startIndex.Value, _pendingChars.Length - startIndex.Value - 1);

                return (row, col);
            }

            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value.x, nameof(value.x));
                ArgumentOutOfRangeException.ThrowIfNegative(value.y, nameof(value.y));

                ArgumentOutOfRangeException.ThrowIfGreaterThan(value.x, 32767, nameof(value.x));
                ArgumentOutOfRangeException.ThrowIfGreaterThan(value.y, 32767, nameof(value.y));

                string s = "\e[";
                if (value.y != 1 && value.y != 0) s += value.y.ToString();
                s += ";";
                if (value.x != 1 && value.x != 0) s += value.x.ToString();
                s += "H";
                Writer.Write(s);
            }
        }

        public WindowsTerminal(Stream stream)
        {
            Stream = stream;
            Writer = new StreamWriter(Stream, Encoding.UTF8, leaveOpen: true);
            Reader = new StreamReader(Stream, Encoding.UTF8, leaveOpen: true);
        }

        public override void Clear() => Writer.Write("\e[2J");
        public override void ClearLine() => Writer.Write("\e[2K");

        public override void Erase(int count = 1) => Writer.Write($"\e[{count}X");

        public override void MoveCursor(int dx, int dy)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(dx, 32767, nameof(dx));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(dy, 32767, nameof(dy));

            ArgumentOutOfRangeException.ThrowIfLessThan(dx, -32767, nameof(dx));
            ArgumentOutOfRangeException.ThrowIfLessThan(dy, -32767, nameof(dy));

            string s = "";
            switch (dx)
            {
                case > 0:
                    if (dx == 1) s += "\e[C";
                    else s += $"\e[{dx}C";
                    break;
                case < 0:
                    s += $"\e[{-dx}D";
                    break;
                case 0:
                    break;
            }

            switch (dy)
            {
                case > 0:
                    if (dy == 1) s += "\e[B";
                    else s += $"\e[{dy}B";
                    break;
                case < 0:
                    s += $"\e[{-dy}A";
                    break;
                case 0:
                    break;
            }

            if (s.Length > 0)
                Writer.Write(s);
        }

        public override int ReadKey(TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public override void SetCursorColor(ITerminalColor color)
        {
        }

        public override void SetCursorShape(CursorShape shape)
        {
            switch (shape)
            {
                case CursorShape.DefaultShape:
                    Writer.Write("\e[0 q");
                    break;
                case CursorShape.BlinkingBlock:
                    Writer.Write("\e[1 q");
                    break;
                case CursorShape.SteadyBlock:
                    Writer.Write("\e[2 q");
                    break;
                case CursorShape.BlinkingUnderline:
                    Writer.Write("\e[3 q");
                    break;
                case CursorShape.SteadyUnderline:
                    Writer.Write("\e[4 q");
                    break;
                case CursorShape.BlinkingBar:
                    Writer.Write("\e[5 q");
                    break;
                case CursorShape.SteadyBar:
                    Writer.Write("\e[6 q");
                    break;
                default:
                    throw new InvalidEnumArgumentException(nameof(shape), (int)shape, typeof(CursorShape));
            }
        }

        public override void SetCursorVisiblity(CursorVisibility visibility)
        {
            switch (visibility)
            {
                case CursorVisibility.Invisible:
                    Writer.Write("\e[?25l");
                    break;
                case CursorVisibility.Visible:
                    Writer.Write("\e[?25h");
                    break;
                default:
                    throw new InvalidEnumArgumentException(nameof(visibility), (int)visibility, typeof(CursorVisibility));
            }
        }

        public override TerminalTrueColor TrueColorFor(TerminalAnsiColor color)
        {
            return color.NearestTrueColor;
        }

        public override TerminalAnsiColor NearestAnsiColor(TerminalTrueColor color)
        {
            return color.NearestAnsiColor;
        }

        public override void Write(ColoredString str)
        {
            Writer.Write(GetEscapeCodeFor(str.Color));
            Writer.Write(str.Text);
            Writer.Write("\e[0;39;49m");
        }


        public void Write(params ColoredString[] strings) => Write((IEnumerable<ColoredString>)strings);

        public void Write(IEnumerable<ColoredString> strings)
        {
            var currentColor = TerminalCellColor.Default;

            foreach (var str in strings)
            {
                Writer.Write(GetEscapeCodeFor(str.Color, currentColor));
                Writer.Write(str.Text);
                currentColor = str.Color;
            }

            Writer.Write("\e[0;39;49m");
        }

        private static readonly StringBuilder _escapeBuilder = new(32);

        private string GetEscapeCodeFor(TerminalCellColor cellColor, TerminalCellColor currentColor)
        {
            _escapeBuilder.Clear();
            _escapeBuilder.Append("\e[");

            if (cellColor.Attributes.HasFlag(TerminalCellColor.CellAttributes.Bold)
                    && !currentColor.Attributes.HasFlag(TerminalCellColor.CellAttributes.Bold))
            {
                _escapeBuilder.Append("1;");
            }

            if (cellColor.Attributes.HasFlag(TerminalCellColor.CellAttributes.Underline)
                    && !currentColor.Attributes.HasFlag(TerminalCellColor.CellAttributes.Underline))
            {
                _escapeBuilder.Append("4;");
            }

            if (!HasTrueColor && cellColor.Foreground is TerminalTrueColor fgtc)
            {
                cellColor = new TerminalCellColor()
                {
                    Foreground = NearestAnsiColor(fgtc),
                    Background = cellColor.Background,
                    Attributes = cellColor.Attributes
                };
            }

            if (!HasTrueColor && cellColor.Background is TerminalTrueColor bgtc)
            {
                cellColor = new TerminalCellColor()
                {
                    Foreground = cellColor.Foreground,
                    Background = NearestAnsiColor(bgtc),
                    Attributes = cellColor.Attributes
                };
            }

            switch (cellColor.Foreground)
            {
                case TerminalAnsiColor ac:
                    if (currentColor.Foreground is TerminalAnsiColor cac && cac.Equals(ac))
                        break;
                    int val = (int)ac.Color;
                    switch (val)
                    {
                        case < 8:
                            _escapeBuilder.Append($"{30 + ac.Color};");
                            break;
                        case < 16:
                            _escapeBuilder.Append($"{90 + ac.Color - 8};");
                            break;
                        case <= 255:
                            _escapeBuilder.Append($"38;5;{ac.Color};");
                            break;
                    }
                    break;
                case TerminalTrueColor tc:
                    if (currentColor.Foreground is TerminalTrueColor ctc && ctc.Equals(tc))
                        break;
                    _escapeBuilder.Append($"38;2;{tc.R};{tc.G};{tc.B};");
                    break;
                default:
                    if (cellColor.Foreground.Equals(ITerminalColor.DefaultForeground))
                    {
                        _escapeBuilder.Append("39;");
                        break;
                    }
                    throw new ArgumentException("Unsupported terminal color type");
            }

            switch (cellColor.Background)
            {
                case TerminalAnsiColor ac:
                    if (currentColor.Background is TerminalAnsiColor cac && cac.Equals(ac))
                        break;
                    int val = (int)ac.Color;
                    switch (val)
                    {
                        case < 8:
                            _escapeBuilder.Append($"{40 + ac.Color};");
                            break;
                        case < 16:
                            _escapeBuilder.Append($"{100 + ac.Color - 8};");
                            break;
                        case <= 255:
                            _escapeBuilder.Append($"48;5;{ac.Color};");
                            break;
                    }
                    break;
                case TerminalTrueColor tc:
                    if (currentColor.Background is TerminalTrueColor ctc && ctc.Equals(tc))
                        break;
                    _escapeBuilder.Append($"48;2;{tc.R};{tc.G};{tc.B};");
                    break;
                default:
                    if (cellColor.Foreground.Equals(ITerminalColor.DefaultBackground))
                    {
                        _escapeBuilder.Append("49;");
                        break;
                    }
                    throw new ArgumentException("Unsupported terminal color type");
            }

            if (_escapeBuilder[^1] == ';') _ = _escapeBuilder.Remove(_escapeBuilder.Length - 1, 1);
            if (_escapeBuilder[^1] == '[') return string.Empty;

            return _escapeBuilder.Append('m').ToString();
        }

        private string GetEscapeCodeFor(TerminalCellColor cellColor) => GetEscapeCodeFor(cellColor, TerminalCellColor.Default);

        public void Flush()
        {
            Writer.Flush();
            Stream.Flush();
        }

        public static WindowsTerminal StandardTerminal => field ??= new StandardWindowsTerminal();
    }
}
