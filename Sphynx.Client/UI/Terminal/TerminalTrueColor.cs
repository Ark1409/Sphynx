// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics.CodeAnalysis;
using Sphynx.Utils;

namespace Sphynx.Client.Tui.Terminal
{
    public readonly struct TerminalTrueColor : ITerminalColor, IEquatable<TerminalTrueColor>
    {
        public required byte R { get; init; }
        public required byte G { get; init; }
        public required byte B { get; init; }

        private static TerminalAnsiColor[]? _defaultColors = null;

        public TerminalAnsiColor NearestAnsiColor
            => NearestAnsiColorFrom(_defaultColors ??=
                    [.. Enum.GetValues<TerminalAnsiColor.AnsiColors>().Select(TerminalAnsiColor.FromColor)]);


        internal TerminalAnsiColor NearestAnsiColorFrom(TerminalAnsiColor[] colors)
        {
            var obj = this;
            return colors
                        .Select(col => (col, distance: TrueColorDistance(col.NearestTrueColor, obj)))
                        .MinBy(pair => pair.distance).col;
        }

        public static TerminalTrueColor FromNormalized(float r, float g, float b) => new()
        {
            R = (byte)(255 * r),
            G = (byte)(255 * g),
            B = (byte)(255 * b),
        };

        public static TerminalTrueColor FromNormalized(double r, double g, double b) => new()
        {
            R = (byte)(255 * r),
            G = (byte)(255 * g),
            B = (byte)(255 * b),
        };

        public static bool TryParseHex(string hex, [NotNullWhen(true)] out TerminalTrueColor color)
        {
            int col;
            try
            {
                col = Convert.ToInt32(hex, 16);
                if (col > 0xffffff || col < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(hex), "Invalid hex color specified");
                }
            }
            catch
            {
                color = default;
                return false;
            }

            if (BitConverter.IsLittleEndian)
            {
                color = new()
                {
                    R = (byte)((col >> 16) & 0xff),
                    G = (byte)((col >> 8) & 0xff),
                    B = (byte)(col & 0xff),
                };
            }
            else
            {
                color = new()
                {
                    R = (byte)(col & 0xff),
                    G = (byte)((col >> 8) & 0xff),
                    B = (byte)((col >> 16) & 0xff),
                };
            }

            return true;
        }

        public readonly bool Equals(TerminalTrueColor other) => other.R == R && other.B == B && other.G == G;

        private static int TrueColorDistance(TerminalTrueColor col1, TerminalTrueColor col2)
        {
            Span<int> vals = [
                Math.Abs((int)col1.R - (int)col2.R),
                Math.Abs((int)col1.G - (int)col2.G),
                Math.Abs((int)col1.B - (int)col2.B)
            ];

            return vals.Max()!.Value;
        }

        public override int GetHashCode() => R << 16 | G << 8 | B;
    }
}
