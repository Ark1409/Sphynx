// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics.CodeAnalysis;
using Sphynx.Utils;

namespace Sphynx.Client.Tui
{
    public readonly struct TerminalTrueColor : ITerminalColor, IEquatable<TerminalTrueColor>
    {
        public byte R { get; }
        public byte G { get; }
        public byte B { get; }

        private static (TerminalAnsiColor.AnsiColors, TerminalTrueColor)[]? _defaultColors = null;

        public TerminalTrueColor(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
        }

        /// <summary>
        /// Computes nearest <see cref="TerminalAnsiColor"/> based on the stored color.
        /// Does not take into account current terminal color palette.
        /// </summary>
        /// <seealso cref="TerminalAnsiColor.NearestTrueColor"/>
        /// <seealso cref="Terminal.NearestAnsiColor(TerminalTrueColor)"/>
        public TerminalAnsiColor NearestAnsiColor
            => NearestAnsiColorFrom(_defaultColors ??=
                    [.. Enum.GetValues<TerminalAnsiColor.AnsiColors>().Select(a => (a, TerminalAnsiColor.FromColor(a).NearestTrueColor))]);

        internal TerminalAnsiColor NearestAnsiColorFrom((TerminalAnsiColor.AnsiColors, TerminalTrueColor)[] colorMappings)
        {
            var obj = this;
            return colorMappings
                        .Select(mapping => (col: mapping.Item1, distance: TrueColorDistance(mapping.Item2, obj)))
                        .MinBy(pair => pair.distance).col;
        }


        public static TerminalTrueColor FromNormalized(float r, float g, float b) => new((byte)(255 * r), (byte)(255 * g), (byte)(255 * b));

        public static TerminalTrueColor FromNormalized(double r, double g, double b) => new((byte)(255 * r), (byte)(255 * g), (byte)(255 * b));

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
                color = new((byte)((col >> 16) & 0xff), (byte)((col >> 8) & 0xff), (byte)(col & 0xff));
            }
            else
            {
                color = new((byte)(col & 0xff), (byte)((col >> 8) & 0xff), (byte)((col >> 16) & 0xff));
            }

            return true;
        }

        public override bool Equals(object? obj) => obj is TerminalTrueColor color && Equals(color);
        public readonly bool Equals(TerminalTrueColor other) => other.R == R && other.B == B && other.G == G;

        private static int TrueColorDistance(TerminalTrueColor col1, TerminalTrueColor col2)
        {
            Span<int> vals = [
                Math.Abs(col1.R - col2.R),
                Math.Abs(col1.G - col2.G),
                Math.Abs(col1.B - col2.B)
            ];

            return vals.Max()!.Value;
        }

        public override int GetHashCode() => R << 16 | G << 8 | B;
    }
}
