// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Client.Utils;

namespace Sphynx.Client.Tui.Terminal
{
    public abstract class Terminal : IDisposable
    {
        public abstract bool HasTrueColor { get; }
        public abstract bool HasMouseSupport { get; }
        public abstract bool HasGraphicsProtocol { get; }

        public abstract int Lines { get; }
        public int Rows => Lines;
        public abstract int Columns { get; }

        public abstract int ReadKey(TimeSpan timeout);

        public abstract void Write(ColoredString str);

        /// <summary>
        /// Erases <paramref name="count" /> cell(s) from the current position backwards.
        /// </summary>
        /// <param name="count">The number of characters to erase.</param>
        public abstract void Erase(int count = 1);

        public abstract (int x, int y) CursorPosition { get; set; }

        public abstract void MoveCursor(int dx, int dy);

        public abstract void SetCursorColor(ITerminalColor color);

        public enum CursorShape : byte
        {
            DefaultShape = 1,
            UserShape = DefaultShape,
            BlinkingBlock,
            SteadyBlock,
            BlinkingUnderline,
            SteadyUnderline,
            BlinkingBar,
            SteadyBar
        }

        public enum CursorVisibility : byte
        {
            Invisible = 0x0,
            Visible = 0x1,
        }

        public abstract void SetCursorShape(CursorShape shape);
        public abstract void SetCursorVisiblity(CursorVisibility visibility);

        public abstract TerminalTrueColor TrueColorFor(TerminalAnsiColor color);
        public abstract TerminalAnsiColor NearestAnsiColor(TerminalTrueColor color);

        public abstract void ClearLine();
        public abstract void Clear();

        public virtual void Init() { }
        public virtual void Dispose() { GC.SuppressFinalize(this); }
    }

    public static class TerminalExtensions
    {
        public static void Write(this Terminal term, TerminalCellColor color, int cellCount = 1)
            => term.Write(new ColoredString { Color = color, Text = " ".Repeat(cellCount) });

        public static void MoveCursorLeft(this Terminal term, int count = 1) => term.MoveCursor(-count, 0);
        public static void MoveCursorRight(this Terminal term, int count = 1) => term.MoveCursor(count, 0);

        public static void MoveCursorUp(this Terminal term, int count = 1) => term.MoveCursor(0, -count);
        public static void MoveCursorDown(this Terminal term, int count = 1) => term.MoveCursor(0, count);

        public static int ReadKey(this Terminal term) => term.ReadKey(TimeSpan.MaxValue);

        public static void WriteLine(this Terminal term, ColoredString str)
        {
            term.Write(str);

            // Does not draw the new line with same color because that's probably not what you want.
            term.Write("\r\n");
        }

        public static void Write(this Terminal term, IEnumerable<ColoredString> s)
        {
            foreach (var v in s) term.Write(v);
        }

        public static void Write(this Terminal term, params ColoredString[] s)
        {
            foreach (var v in s) term.Write(v);
        }

        public static void Write(this Terminal term, Span<ColoredString> s)
        {
            foreach (var v in s) term.Write(v);
        }

        public static void WriteLine(this Terminal term, IEnumerable<ColoredString> s)
        {
            foreach (var v in s) term.WriteLine(v);
        }

        public static void WriteLine(this Terminal term, Span<ColoredString> s)
        {
            foreach (var v in s) term.WriteLine(v);
        }

        public static void SetCursorState(this Terminal term, Terminal.CursorShape shape, Terminal.CursorVisibility visibility)
        {
            term.SetCursorShape(shape);
            term.SetCursorVisiblity(visibility);
        }
    }
}
