// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Sphynx.Client.Tui.Terminal
{
    public readonly struct TerminalAnsiColor : ITerminalColor, IEquatable<TerminalAnsiColor>
    {
        public required AnsiColors Color { get; init; }

        /// <summary>
        /// Tries to retrieve the nearest direct-color value based off of the encoded color value.
        /// </summary>
        /// <remarks>
        /// Since terminals tend to be able to change the colors of these values, these should not be confused with the
        /// color you would actually get from displaying this <see cref="TerminalAnsiColor"/> on the screen. Instead,
        /// this is meant to act as a sort of "best-fit" solution one when does not necessarily have access to the
        /// terminal they will be displaying on directly.
        ///
        /// The implemenetation tries to use sensible default values when possible; the normal 8 basic colors would
        /// normally be returning their max component values if not for the non-standard bright versions we have to
        /// accomodate. Instead, we let the bright versions emit a full color component value with the darker versions
        /// emitting half of said value (128). This behaviour is inherited from Windows Console/PowerShell. For the 6x6x6 color
        /// gradient/24 grayscale colors using the formula used (most?) by terminals such as e.g. xterm (see
        /// https://en.wikipedia.org/wiki/ANSI_escape_code#8-bit).
        ///</remarks>
        /// <returns>Nearest true (direct) color value.</returns>
        public readonly TerminalTrueColor NearestTrueColor
        {
            get
            {
                if (Color is >= AnsiColors.Black and <= AnsiColors.White)
                {
                    byte val = (byte)Color;
                    return new()
                    {
                        R = (byte)((val & 0x1) * 128),
                        G = (byte)(((val >> 1) & 0x1) * 128),
                        B = (byte)(((val >> 2) & 0x1) * 128),
                    };
                }

                if (Color is >= AnsiColors.BrightBlack and <= AnsiColors.BrightWhite)
                {
                    // no standard color definition for these 8 bright versions
                    byte val = (byte)Color;
                    return new()
                    {
                        R = (byte)((val & 0x1) * 255),
                        G = (byte)(((val >> 1) & 0x1) * 255),
                        B = (byte)(((val >> 2) & 0x1) * 255),
                    };
                }

                if (Color is >= AnsiColors.Color16 and <= AnsiColors.Color231)
                {
                    byte val = (byte)Color;
                    int bIndex = (val - 16) % 6;
                    int gIndex = (val - 16 - bIndex) / 6 % 6;
                    int rIndex = (val - 16 - bIndex - gIndex) / 36 % 6;

                    int r = rIndex == 0 ? 0 : rIndex * 40 + 55;
                    int g = gIndex == 0 ? 0 : gIndex * 40 + 55;
                    int b = bIndex == 0 ? 0 : bIndex * 40 + 55;

                    return new() { R = (byte)r, G = (byte)g, B = (byte)b };
                }

                if (Color is >= AnsiColors.Color232 and <= AnsiColors.Color255)
                {
                    byte val = (byte)Color;
                    int gray = val - 232;
                    int col = gray * 10 + 8;
                    return new() { R = (byte)col, G = (byte)col, B = (byte)col };
                }

                throw new ArgumentOutOfRangeException("Invalid color value for direct color translation");
            }
        }

        private static readonly TerminalAnsiColor[] _defaultColors = Enum.GetValues<AnsiColors>().Select(a => new TerminalAnsiColor { Color = a }).ToArray();

        public static TerminalAnsiColor FromColor(AnsiColors color)
        {
            return _defaultColors[(int)color];
        }

        public static implicit operator TerminalAnsiColor(AnsiColors color) => FromColor(color);

        public readonly bool Equals(TerminalAnsiColor other) => other.Color == Color;
        public override int GetHashCode() => (int)Color;


        public enum AnsiColors : byte
        {
            // Basic 8 colors
            Black = 0,
            Red = 1,
            Green = 2,
            Yellow = 3,
            Blue = 4,
            Magenta = 5,
            Cyan = 6,
            White = 7,

            // Bright versions of basic 8 colors
            // Apparently not a part of ANSI, added by aixterm (https://invisible-island.net/xterm/ctlseqs/ctlseqs.html)
            BrightBlack,
            BrightRed,
            BrightGreen,
            BrightYellow,
            BrightBlue,
            BrightMagenta,
            BrightCyan,
            BrightWhite,

            // 216 extended colors (6, 6x6 gradient squares)
            Color16 = 16,
            Color17,
            Color18,
            Color19,
            Color20,
            Color21,
            Color22,
            Color23,
            Color24,
            Color25,
            Color26,
            Color27,
            Color28,
            Color29,
            Color30,
            Color31,
            Color32,
            Color33,
            Color34,
            Color35,
            Color36,
            Color37,
            Color38,
            Color39,
            Color40,
            Color41,
            Color42,
            Color43,
            Color44,
            Color45,
            Color46,
            Color47,
            Color48,
            Color49,
            Color50,
            Color51,
            Color52,
            Color53,
            Color54,
            Color55,
            Color56,
            Color57,
            Color58,
            Color59,
            Color60,
            Color61,
            Color62,
            Color63,
            Color64,
            Color65,
            Color66,
            Color67,
            Color68,
            Color69,
            Color70,
            Color71,
            Color72,
            Color73,
            Color74,
            Color75,
            Color76,
            Color77,
            Color78,
            Color79,
            Color80,
            Color81,
            Color82,
            Color83,
            Color84,
            Color85,
            Color86,
            Color87,
            Color88,
            Color89,
            Color90,
            Color91,
            Color92,
            Color93,
            Color94,
            Color95,
            Color96,
            Color97,
            Color98,
            Color99,
            Color100,
            Color101,
            Color102,
            Color103,
            Color104,
            Color105,
            Color106,
            Color107,
            Color108,
            Color109,
            Color110,
            Color111,
            Color112,
            Color113,
            Color114,
            Color115,
            Color116,
            Color117,
            Color118,
            Color119,
            Color120,
            Color121,
            Color122,
            Color123,
            Color124,
            Color125,
            Color126,
            Color127,
            Color128,
            Color129,
            Color130,
            Color131,
            Color132,
            Color133,
            Color134,
            Color135,
            Color136,
            Color137,
            Color138,
            Color139,
            Color140,
            Color141,
            Color142,
            Color143,
            Color144,
            Color145,
            Color146,
            Color147,
            Color148,
            Color149,
            Color150,
            Color151,
            Color152,
            Color153,
            Color154,
            Color155,
            Color156,
            Color157,
            Color158,
            Color159,
            Color160,
            Color161,
            Color162,
            Color163,
            Color164,
            Color165,
            Color166,
            Color167,
            Color168,
            Color169,
            Color170,
            Color171,
            Color172,
            Color173,
            Color174,
            Color175,
            Color176,
            Color177,
            Color178,
            Color179,
            Color180,
            Color181,
            Color182,
            Color183,
            Color184,
            Color185,
            Color186,
            Color187,
            Color188,
            Color189,
            Color190,
            Color191,
            Color192,
            Color193,
            Color194,
            Color195,
            Color196,
            Color197,
            Color198,
            Color199,
            Color200,
            Color201,
            Color202,
            Color203,
            Color204,
            Color205,
            Color206,
            Color207,
            Color208,
            Color209,
            Color210,
            Color211,
            Color212,
            Color213,
            Color214,
            Color215,
            Color216,
            Color217,
            Color218,
            Color219,
            Color220,
            Color221,
            Color222,
            Color223,
            Color224,
            Color225,
            Color226,
            Color227,
            Color228,
            Color229,
            Color230,
            Color231,

            // Grayscale Colors (dark to light)
            Color232 = 232,
            Color233,
            Color234,
            Color235,
            Color236,
            Color237,
            Color238,
            Color239,
            Color240,
            Color241,
            Color242,
            Color243,
            Color244,
            Color245,
            Color246,
            Color247,
            Color248,
            Color249,
            Color250,
            Color251,
            Color252,
            Color253,
            Color254,
            Color255,
        }
    }
}
