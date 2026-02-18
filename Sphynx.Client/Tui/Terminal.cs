// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Utils;

namespace Sphynx.Client.Tui
{
    public abstract class Terminal : IDisposable
    {
        public abstract TerminalColorSupport ColorSupport { get; }
        public bool HasTrueColor => (ColorSupport & TerminalColorSupport.TrueColor) == TerminalColorSupport.TrueColor;

        public abstract bool HasMouseSupport { get; }
        public abstract bool HasGraphicsProtocol { get; }

        public abstract int Lines { get; }
        public int Rows => Lines;
        public abstract int Columns { get; }

        public abstract void Write(ColoredString str);

        public virtual void Write(IEnumerable<ColoredString> strs)
        {
            foreach (var str in strs)
                Write(str);
        }

        /// <summary>
        /// Erases <paramref name="count" /> cell(s) from the current position backwards.
        /// </summary>
        /// <param name="count">The number of characters to erase.</param>
        public abstract void Erase(int count = 1);

        public abstract (int x, int y) CursorPosition { get; set; }

        public virtual void MoveCursor(int dx, int dy)
        {
            var (x, y) = CursorPosition;
            CursorPosition = (x + dx, y + dy);
        }

        public abstract ITerminalColor CursorColor { get; set; }

        public abstract TerminalCursorShape CursorShape { set; }
        public abstract TerminalCursorVisibility CursorVisibility { set; }

        public abstract TerminalTrueColor TrueColorFor(TerminalAnsiColor color);
        public abstract TerminalAnsiColor NearestAnsiColor(TerminalTrueColor color);

        public abstract void ClearLine();
        public abstract void Clear();

        public virtual void Init() { }
        public virtual void Dispose() { GC.SuppressFinalize(this); }

        public abstract void Flush();

        public abstract TerminalEvent PollEvent();
        protected internal abstract bool CanPollEvent(Type t);
    }

    public enum TerminalCursorShape : byte
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

    public enum TerminalCursorVisibility : byte
    {
        Invisible = 0x0,
        Visible = 0x1,
    }

    // NOTE: Do NOT rely on these being subsets/supersets of each other
    [Flags]
    public enum TerminalColorSupport
    {
        Ansi8 = 0x1, // Normal 0-7
        Ansi16 = 0x3, // (Bright) 8-15; NOTE: May be implemented as (bold + (0-7))
        XTerm256 = 0x7, // XTerm 16-255
        TrueColor = 0xF, // 16777216 RGB
    }

    public static class TerminalExtensions
    {
        public static void Write(this Terminal term, TerminalCellColor color, int cellCount = 1)
            => term.Write(new ColoredString { Color = color, Text = ' '.Repeat(cellCount) });

        public static void MoveCursorLeft(this Terminal term, int count = 1) => term.MoveCursor(-count, 0);
        public static void MoveCursorRight(this Terminal term, int count = 1) => term.MoveCursor(count, 0);

        public static void MoveCursorUp(this Terminal term, int count = 1) => term.MoveCursor(0, -count);
        public static void MoveCursorDown(this Terminal term, int count = 1) => term.MoveCursor(0, count);

        public static void Write(this Terminal term, params ColoredString[] strs) => term.Write((IEnumerable<ColoredString>)strs);

        public static void WriteLine(this Terminal term, ColoredString str)
        {
            term.Write(str);

            // Does not draw the new line with same color because that's probably not what you want.
            term.Write("\r\n");
        }

        public static void SetCursorState(this Terminal term, TerminalCursorShape shape, TerminalCursorVisibility visibility)
        {
            term.CursorShape = shape;
            term.CursorVisibility = visibility;
        }

        public static bool CanPollEvent<T>(this Terminal term) where T : ITerminalEvent => term.CanPollEvent(typeof(T));

        public static TerminalEvent PollUntil<T>(this Terminal term) where T : ITerminalEvent
        {
            var type = typeof(T);
            while (true)
            {
                var ev = term.PollEvent();
                if (type == typeof(TerminalKeyEvent) && ev.KeyEvent.HasValue)
                {
                    return ev;
                }
                else if (type == typeof(TerminalWindowEvent) && ev.WindowEvent.HasValue)
                {
                    return ev;
                }
                else if (type == typeof(TerminalMouseMoveEvent) && ev.MouseMoveEvent.HasValue)
                {
                    return ev;
                }
                else if (type == typeof(TerminalMouseClickEvent) && ev.MouseClickEvent.HasValue)
                {
                    return ev;
                }
                else if (type == typeof(TerminalMouseScrollEvent) && ev.MouseScrollEvent.HasValue)
                {
                    return ev;
                }
            }
        }
    }
}
