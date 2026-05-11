// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Utils;

namespace Sphynx.Client.Tui
{
    public readonly record struct ColoredString(TerminalCellColor Color, string Text)
    {
        public static implicit operator ColoredString(string s) => new(TerminalCellColor.Default, s);

        public static ColoredString[] Split(string s, params (Range range, TerminalCellColor color)[] colors)
        {
            var tempColors = colors.Where(a => a.range.GetOffsetAndLength(s.Length).Length > 0);
            if (tempColors.Select(a => a.range).HasOverlap(s.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(colors), $"Color ranges cannot be overlapping");
            }

            colors = [.. tempColors];
            Array.Sort(colors, (a, b) =>
                a.range.GetOffsetAndLength(s.Length).Offset.CompareTo(b.range.GetOffsetAndLength(s.Length).Offset));

            return colors.Select(a => new ColoredString(a.color, s[a.range])).ToArray();
        }

        public override string ToString() => Text;
    }

    public static class ColoredStringExtensions
    {
        public static ColoredString WithColor(this string s, in TerminalCellColor color) => new(color, s);
        public static ColoredString WithColor<T>(this string s, T fg)
            where T : ITerminalColor => new(new TerminalCellColor(fg), s);
        public static ColoredString WithColor<T, U>(this string s, T fg, U bg)
            where T : ITerminalColor
            where U : ITerminalColor => new(new TerminalCellColor { Foreground = fg, Background = bg }, s);
    }
}
