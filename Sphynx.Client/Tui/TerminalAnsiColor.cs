// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Sphynx.Client.Tui
{
    public sealed class TerminalAnsiColor : ITerminalColor, IEquatable<TerminalAnsiColor>, IEquatable<TerminalAnsiColor.AnsiColors>
    {
        public AnsiColors Color { get; private init; }

        /// <summary>
        /// Tries to retrieve the nearest direct-color value based off of the encoded color value.
        /// </summary>
        /// <remarks>
        /// Since terminals tend to be able to change the colors of these values, these should not be confused with the
        /// color you would actually get from displaying this <see cref="TerminalAnsiColor"/> on the screen. Instead,
        /// this is meant to act as a sort of "best-fit" solution when one does not necessarily have access to the
        /// terminal they will be displaying on directly.
        ///
        /// The implemenetation tries to use sensible default values when possible; the normal 8 basic colors would
        /// normally be returning their max component values if not for the non-standard bright versions we have to
        /// accomodate. Instead, we let the bright versions emit a full color component value with the darker versions
        /// emitting half of said value (128). This behaviour is inherited from Windows Console/PowerShell. For the 6x6x6 color
        /// gradient/24 grayscale colors using the formula used (most?) by terminals such as e.g. xterm (see
        /// https://en.wikipedia.org/wiki/ANSI_escape_code#8-bit).
        /// </remarks>
        /// <returns>Nearest true (direct) color value.</returns>
        /// <seealso cref="Terminal.TrueColorFor(TerminalAnsiColor)">
        public TerminalTrueColor NearestTrueColor
        {
            get
            {
                if (Color is >= AnsiColors.Black and <= AnsiColors.White)
                {
                    byte val = (byte)Color;
                    return new((byte)((val & 0x1) * 128), (byte)(((val >> 1) & 0x1) * 128), (byte)(((val >> 2) & 0x1) * 128));
                }

                if (Color is >= AnsiColors.BrightBlack and <= AnsiColors.BrightWhite)
                {
                    // no standard color definition for these 8 bright versions
                    byte val = (byte)Color;
                    return new((byte)((val & 0x1) * 255), (byte)(((val >> 1) & 0x1) * 255), (byte)(((val >> 2) & 0x1) * 255));
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

                    return new((byte)r, (byte)g, (byte)b);
                }

                if (Color is >= AnsiColors.Color232 and <= AnsiColors.Color255)
                {
                    byte val = (byte)Color;
                    int gray = val - 232;
                    int col = gray * 10 + 8;
                    return new((byte)col, (byte)col, (byte)col);
                }

                throw new ArgumentOutOfRangeException("Invalid color value for direct color translation");
            }
        }

        private static readonly TerminalAnsiColor[] _defaultColors = Enum.GetValues<AnsiColors>()
            .Order()
            .Select(a => new TerminalAnsiColor { Color = a })
            .ToArray();

        public static TerminalAnsiColor FromColor(AnsiColors color) => _defaultColors[(int)color];
        public static implicit operator TerminalAnsiColor(AnsiColors a) => FromColor(a);
        public static implicit operator AnsiColors(TerminalAnsiColor a) => a.Color;

        public bool Equals(TerminalAnsiColor? other) => other?.Color == Color;
        public bool Equals(AnsiColors other) => other == Color;
        public override bool Equals(object? obj) => Equals(obj as TerminalAnsiColor);

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

        public static readonly TerminalAnsiColor Black = FromColor(AnsiColors.Black);
        public static readonly TerminalAnsiColor Red = FromColor(AnsiColors.Red);
        public static readonly TerminalAnsiColor Green = FromColor(AnsiColors.Green);
        public static readonly TerminalAnsiColor Yellow = FromColor(AnsiColors.Yellow);
        public static readonly TerminalAnsiColor Blue = FromColor(AnsiColors.Blue);
        public static readonly TerminalAnsiColor Magenta = FromColor(AnsiColors.Magenta);
        public static readonly TerminalAnsiColor Cyan = FromColor(AnsiColors.Cyan);
        public static readonly TerminalAnsiColor White = FromColor(AnsiColors.White);
        public static readonly TerminalAnsiColor BrightBlack = FromColor(AnsiColors.BrightBlack);
        public static readonly TerminalAnsiColor BrightRed = FromColor(AnsiColors.BrightRed);
        public static readonly TerminalAnsiColor BrightGreen = FromColor(AnsiColors.BrightGreen);
        public static readonly TerminalAnsiColor BrightYellow = FromColor(AnsiColors.BrightYellow);
        public static readonly TerminalAnsiColor BrightBlue = FromColor(AnsiColors.BrightBlue);
        public static readonly TerminalAnsiColor BrightMagenta = FromColor(AnsiColors.BrightMagenta);
        public static readonly TerminalAnsiColor BrightCyan = FromColor(AnsiColors.BrightCyan);
        public static readonly TerminalAnsiColor BrightWhite = FromColor(AnsiColors.BrightWhite);
        public static readonly TerminalAnsiColor Color16 = FromColor(AnsiColors.Color16);
        public static readonly TerminalAnsiColor Color17 = FromColor(AnsiColors.Color17);
        public static readonly TerminalAnsiColor Color18 = FromColor(AnsiColors.Color18);
        public static readonly TerminalAnsiColor Color19 = FromColor(AnsiColors.Color19);
        public static readonly TerminalAnsiColor Color20 = FromColor(AnsiColors.Color20);
        public static readonly TerminalAnsiColor Color21 = FromColor(AnsiColors.Color21);
        public static readonly TerminalAnsiColor Color22 = FromColor(AnsiColors.Color22);
        public static readonly TerminalAnsiColor Color23 = FromColor(AnsiColors.Color23);
        public static readonly TerminalAnsiColor Color24 = FromColor(AnsiColors.Color24);
        public static readonly TerminalAnsiColor Color25 = FromColor(AnsiColors.Color25);
        public static readonly TerminalAnsiColor Color26 = FromColor(AnsiColors.Color26);
        public static readonly TerminalAnsiColor Color27 = FromColor(AnsiColors.Color27);
        public static readonly TerminalAnsiColor Color28 = FromColor(AnsiColors.Color28);
        public static readonly TerminalAnsiColor Color29 = FromColor(AnsiColors.Color29);
        public static readonly TerminalAnsiColor Color30 = FromColor(AnsiColors.Color30);
        public static readonly TerminalAnsiColor Color31 = FromColor(AnsiColors.Color31);
        public static readonly TerminalAnsiColor Color32 = FromColor(AnsiColors.Color32);
        public static readonly TerminalAnsiColor Color33 = FromColor(AnsiColors.Color33);
        public static readonly TerminalAnsiColor Color34 = FromColor(AnsiColors.Color34);
        public static readonly TerminalAnsiColor Color35 = FromColor(AnsiColors.Color35);
        public static readonly TerminalAnsiColor Color36 = FromColor(AnsiColors.Color36);
        public static readonly TerminalAnsiColor Color37 = FromColor(AnsiColors.Color37);
        public static readonly TerminalAnsiColor Color38 = FromColor(AnsiColors.Color38);
        public static readonly TerminalAnsiColor Color39 = FromColor(AnsiColors.Color39);
        public static readonly TerminalAnsiColor Color40 = FromColor(AnsiColors.Color40);
        public static readonly TerminalAnsiColor Color41 = FromColor(AnsiColors.Color41);
        public static readonly TerminalAnsiColor Color42 = FromColor(AnsiColors.Color42);
        public static readonly TerminalAnsiColor Color43 = FromColor(AnsiColors.Color43);
        public static readonly TerminalAnsiColor Color44 = FromColor(AnsiColors.Color44);
        public static readonly TerminalAnsiColor Color45 = FromColor(AnsiColors.Color45);
        public static readonly TerminalAnsiColor Color46 = FromColor(AnsiColors.Color46);
        public static readonly TerminalAnsiColor Color47 = FromColor(AnsiColors.Color47);
        public static readonly TerminalAnsiColor Color48 = FromColor(AnsiColors.Color48);
        public static readonly TerminalAnsiColor Color49 = FromColor(AnsiColors.Color49);
        public static readonly TerminalAnsiColor Color50 = FromColor(AnsiColors.Color50);
        public static readonly TerminalAnsiColor Color51 = FromColor(AnsiColors.Color51);
        public static readonly TerminalAnsiColor Color52 = FromColor(AnsiColors.Color52);
        public static readonly TerminalAnsiColor Color53 = FromColor(AnsiColors.Color53);
        public static readonly TerminalAnsiColor Color54 = FromColor(AnsiColors.Color54);
        public static readonly TerminalAnsiColor Color55 = FromColor(AnsiColors.Color55);
        public static readonly TerminalAnsiColor Color56 = FromColor(AnsiColors.Color56);
        public static readonly TerminalAnsiColor Color57 = FromColor(AnsiColors.Color57);
        public static readonly TerminalAnsiColor Color58 = FromColor(AnsiColors.Color58);
        public static readonly TerminalAnsiColor Color59 = FromColor(AnsiColors.Color59);
        public static readonly TerminalAnsiColor Color60 = FromColor(AnsiColors.Color60);
        public static readonly TerminalAnsiColor Color61 = FromColor(AnsiColors.Color61);
        public static readonly TerminalAnsiColor Color62 = FromColor(AnsiColors.Color62);
        public static readonly TerminalAnsiColor Color63 = FromColor(AnsiColors.Color63);
        public static readonly TerminalAnsiColor Color64 = FromColor(AnsiColors.Color64);
        public static readonly TerminalAnsiColor Color65 = FromColor(AnsiColors.Color65);
        public static readonly TerminalAnsiColor Color66 = FromColor(AnsiColors.Color66);
        public static readonly TerminalAnsiColor Color67 = FromColor(AnsiColors.Color67);
        public static readonly TerminalAnsiColor Color68 = FromColor(AnsiColors.Color68);
        public static readonly TerminalAnsiColor Color69 = FromColor(AnsiColors.Color69);
        public static readonly TerminalAnsiColor Color70 = FromColor(AnsiColors.Color70);
        public static readonly TerminalAnsiColor Color71 = FromColor(AnsiColors.Color71);
        public static readonly TerminalAnsiColor Color72 = FromColor(AnsiColors.Color72);
        public static readonly TerminalAnsiColor Color73 = FromColor(AnsiColors.Color73);
        public static readonly TerminalAnsiColor Color74 = FromColor(AnsiColors.Color74);
        public static readonly TerminalAnsiColor Color75 = FromColor(AnsiColors.Color75);
        public static readonly TerminalAnsiColor Color76 = FromColor(AnsiColors.Color76);
        public static readonly TerminalAnsiColor Color77 = FromColor(AnsiColors.Color77);
        public static readonly TerminalAnsiColor Color78 = FromColor(AnsiColors.Color78);
        public static readonly TerminalAnsiColor Color79 = FromColor(AnsiColors.Color79);
        public static readonly TerminalAnsiColor Color80 = FromColor(AnsiColors.Color80);
        public static readonly TerminalAnsiColor Color81 = FromColor(AnsiColors.Color81);
        public static readonly TerminalAnsiColor Color82 = FromColor(AnsiColors.Color82);
        public static readonly TerminalAnsiColor Color83 = FromColor(AnsiColors.Color83);
        public static readonly TerminalAnsiColor Color84 = FromColor(AnsiColors.Color84);
        public static readonly TerminalAnsiColor Color85 = FromColor(AnsiColors.Color85);
        public static readonly TerminalAnsiColor Color86 = FromColor(AnsiColors.Color86);
        public static readonly TerminalAnsiColor Color87 = FromColor(AnsiColors.Color87);
        public static readonly TerminalAnsiColor Color88 = FromColor(AnsiColors.Color88);
        public static readonly TerminalAnsiColor Color89 = FromColor(AnsiColors.Color89);
        public static readonly TerminalAnsiColor Color90 = FromColor(AnsiColors.Color90);
        public static readonly TerminalAnsiColor Color91 = FromColor(AnsiColors.Color91);
        public static readonly TerminalAnsiColor Color92 = FromColor(AnsiColors.Color92);
        public static readonly TerminalAnsiColor Color93 = FromColor(AnsiColors.Color93);
        public static readonly TerminalAnsiColor Color94 = FromColor(AnsiColors.Color94);
        public static readonly TerminalAnsiColor Color95 = FromColor(AnsiColors.Color95);
        public static readonly TerminalAnsiColor Color96 = FromColor(AnsiColors.Color96);
        public static readonly TerminalAnsiColor Color97 = FromColor(AnsiColors.Color97);
        public static readonly TerminalAnsiColor Color98 = FromColor(AnsiColors.Color98);
        public static readonly TerminalAnsiColor Color99 = FromColor(AnsiColors.Color99);
        public static readonly TerminalAnsiColor Color100 = FromColor(AnsiColors.Color100);
        public static readonly TerminalAnsiColor Color101 = FromColor(AnsiColors.Color101);
        public static readonly TerminalAnsiColor Color102 = FromColor(AnsiColors.Color102);
        public static readonly TerminalAnsiColor Color103 = FromColor(AnsiColors.Color103);
        public static readonly TerminalAnsiColor Color104 = FromColor(AnsiColors.Color104);
        public static readonly TerminalAnsiColor Color105 = FromColor(AnsiColors.Color105);
        public static readonly TerminalAnsiColor Color106 = FromColor(AnsiColors.Color106);
        public static readonly TerminalAnsiColor Color107 = FromColor(AnsiColors.Color107);
        public static readonly TerminalAnsiColor Color108 = FromColor(AnsiColors.Color108);
        public static readonly TerminalAnsiColor Color109 = FromColor(AnsiColors.Color109);
        public static readonly TerminalAnsiColor Color110 = FromColor(AnsiColors.Color110);
        public static readonly TerminalAnsiColor Color111 = FromColor(AnsiColors.Color111);
        public static readonly TerminalAnsiColor Color112 = FromColor(AnsiColors.Color112);
        public static readonly TerminalAnsiColor Color113 = FromColor(AnsiColors.Color113);
        public static readonly TerminalAnsiColor Color114 = FromColor(AnsiColors.Color114);
        public static readonly TerminalAnsiColor Color115 = FromColor(AnsiColors.Color115);
        public static readonly TerminalAnsiColor Color116 = FromColor(AnsiColors.Color116);
        public static readonly TerminalAnsiColor Color117 = FromColor(AnsiColors.Color117);
        public static readonly TerminalAnsiColor Color118 = FromColor(AnsiColors.Color118);
        public static readonly TerminalAnsiColor Color119 = FromColor(AnsiColors.Color119);
        public static readonly TerminalAnsiColor Color120 = FromColor(AnsiColors.Color120);
        public static readonly TerminalAnsiColor Color121 = FromColor(AnsiColors.Color121);
        public static readonly TerminalAnsiColor Color122 = FromColor(AnsiColors.Color122);
        public static readonly TerminalAnsiColor Color123 = FromColor(AnsiColors.Color123);
        public static readonly TerminalAnsiColor Color124 = FromColor(AnsiColors.Color124);
        public static readonly TerminalAnsiColor Color125 = FromColor(AnsiColors.Color125);
        public static readonly TerminalAnsiColor Color126 = FromColor(AnsiColors.Color126);
        public static readonly TerminalAnsiColor Color127 = FromColor(AnsiColors.Color127);
        public static readonly TerminalAnsiColor Color128 = FromColor(AnsiColors.Color128);
        public static readonly TerminalAnsiColor Color129 = FromColor(AnsiColors.Color129);
        public static readonly TerminalAnsiColor Color130 = FromColor(AnsiColors.Color130);
        public static readonly TerminalAnsiColor Color131 = FromColor(AnsiColors.Color131);
        public static readonly TerminalAnsiColor Color132 = FromColor(AnsiColors.Color132);
        public static readonly TerminalAnsiColor Color133 = FromColor(AnsiColors.Color133);
        public static readonly TerminalAnsiColor Color134 = FromColor(AnsiColors.Color134);
        public static readonly TerminalAnsiColor Color135 = FromColor(AnsiColors.Color135);
        public static readonly TerminalAnsiColor Color136 = FromColor(AnsiColors.Color136);
        public static readonly TerminalAnsiColor Color137 = FromColor(AnsiColors.Color137);
        public static readonly TerminalAnsiColor Color138 = FromColor(AnsiColors.Color138);
        public static readonly TerminalAnsiColor Color139 = FromColor(AnsiColors.Color139);
        public static readonly TerminalAnsiColor Color140 = FromColor(AnsiColors.Color140);
        public static readonly TerminalAnsiColor Color141 = FromColor(AnsiColors.Color141);
        public static readonly TerminalAnsiColor Color142 = FromColor(AnsiColors.Color142);
        public static readonly TerminalAnsiColor Color143 = FromColor(AnsiColors.Color143);
        public static readonly TerminalAnsiColor Color144 = FromColor(AnsiColors.Color144);
        public static readonly TerminalAnsiColor Color145 = FromColor(AnsiColors.Color145);
        public static readonly TerminalAnsiColor Color146 = FromColor(AnsiColors.Color146);
        public static readonly TerminalAnsiColor Color147 = FromColor(AnsiColors.Color147);
        public static readonly TerminalAnsiColor Color148 = FromColor(AnsiColors.Color148);
        public static readonly TerminalAnsiColor Color149 = FromColor(AnsiColors.Color149);
        public static readonly TerminalAnsiColor Color150 = FromColor(AnsiColors.Color150);
        public static readonly TerminalAnsiColor Color151 = FromColor(AnsiColors.Color151);
        public static readonly TerminalAnsiColor Color152 = FromColor(AnsiColors.Color152);
        public static readonly TerminalAnsiColor Color153 = FromColor(AnsiColors.Color153);
        public static readonly TerminalAnsiColor Color154 = FromColor(AnsiColors.Color154);
        public static readonly TerminalAnsiColor Color155 = FromColor(AnsiColors.Color155);
        public static readonly TerminalAnsiColor Color156 = FromColor(AnsiColors.Color156);
        public static readonly TerminalAnsiColor Color157 = FromColor(AnsiColors.Color157);
        public static readonly TerminalAnsiColor Color158 = FromColor(AnsiColors.Color158);
        public static readonly TerminalAnsiColor Color159 = FromColor(AnsiColors.Color159);
        public static readonly TerminalAnsiColor Color160 = FromColor(AnsiColors.Color160);
        public static readonly TerminalAnsiColor Color161 = FromColor(AnsiColors.Color161);
        public static readonly TerminalAnsiColor Color162 = FromColor(AnsiColors.Color162);
        public static readonly TerminalAnsiColor Color163 = FromColor(AnsiColors.Color163);
        public static readonly TerminalAnsiColor Color164 = FromColor(AnsiColors.Color164);
        public static readonly TerminalAnsiColor Color165 = FromColor(AnsiColors.Color165);
        public static readonly TerminalAnsiColor Color166 = FromColor(AnsiColors.Color166);
        public static readonly TerminalAnsiColor Color167 = FromColor(AnsiColors.Color167);
        public static readonly TerminalAnsiColor Color168 = FromColor(AnsiColors.Color168);
        public static readonly TerminalAnsiColor Color169 = FromColor(AnsiColors.Color169);
        public static readonly TerminalAnsiColor Color170 = FromColor(AnsiColors.Color170);
        public static readonly TerminalAnsiColor Color171 = FromColor(AnsiColors.Color171);
        public static readonly TerminalAnsiColor Color172 = FromColor(AnsiColors.Color172);
        public static readonly TerminalAnsiColor Color173 = FromColor(AnsiColors.Color173);
        public static readonly TerminalAnsiColor Color174 = FromColor(AnsiColors.Color174);
        public static readonly TerminalAnsiColor Color175 = FromColor(AnsiColors.Color175);
        public static readonly TerminalAnsiColor Color176 = FromColor(AnsiColors.Color176);
        public static readonly TerminalAnsiColor Color177 = FromColor(AnsiColors.Color177);
        public static readonly TerminalAnsiColor Color178 = FromColor(AnsiColors.Color178);
        public static readonly TerminalAnsiColor Color179 = FromColor(AnsiColors.Color179);
        public static readonly TerminalAnsiColor Color180 = FromColor(AnsiColors.Color180);
        public static readonly TerminalAnsiColor Color181 = FromColor(AnsiColors.Color181);
        public static readonly TerminalAnsiColor Color182 = FromColor(AnsiColors.Color182);
        public static readonly TerminalAnsiColor Color183 = FromColor(AnsiColors.Color183);
        public static readonly TerminalAnsiColor Color184 = FromColor(AnsiColors.Color184);
        public static readonly TerminalAnsiColor Color185 = FromColor(AnsiColors.Color185);
        public static readonly TerminalAnsiColor Color186 = FromColor(AnsiColors.Color186);
        public static readonly TerminalAnsiColor Color187 = FromColor(AnsiColors.Color187);
        public static readonly TerminalAnsiColor Color188 = FromColor(AnsiColors.Color188);
        public static readonly TerminalAnsiColor Color189 = FromColor(AnsiColors.Color189);
        public static readonly TerminalAnsiColor Color190 = FromColor(AnsiColors.Color190);
        public static readonly TerminalAnsiColor Color191 = FromColor(AnsiColors.Color191);
        public static readonly TerminalAnsiColor Color192 = FromColor(AnsiColors.Color192);
        public static readonly TerminalAnsiColor Color193 = FromColor(AnsiColors.Color193);
        public static readonly TerminalAnsiColor Color194 = FromColor(AnsiColors.Color194);
        public static readonly TerminalAnsiColor Color195 = FromColor(AnsiColors.Color195);
        public static readonly TerminalAnsiColor Color196 = FromColor(AnsiColors.Color196);
        public static readonly TerminalAnsiColor Color197 = FromColor(AnsiColors.Color197);
        public static readonly TerminalAnsiColor Color198 = FromColor(AnsiColors.Color198);
        public static readonly TerminalAnsiColor Color199 = FromColor(AnsiColors.Color199);
        public static readonly TerminalAnsiColor Color200 = FromColor(AnsiColors.Color200);
        public static readonly TerminalAnsiColor Color201 = FromColor(AnsiColors.Color201);
        public static readonly TerminalAnsiColor Color202 = FromColor(AnsiColors.Color202);
        public static readonly TerminalAnsiColor Color203 = FromColor(AnsiColors.Color203);
        public static readonly TerminalAnsiColor Color204 = FromColor(AnsiColors.Color204);
        public static readonly TerminalAnsiColor Color205 = FromColor(AnsiColors.Color205);
        public static readonly TerminalAnsiColor Color206 = FromColor(AnsiColors.Color206);
        public static readonly TerminalAnsiColor Color207 = FromColor(AnsiColors.Color207);
        public static readonly TerminalAnsiColor Color208 = FromColor(AnsiColors.Color208);
        public static readonly TerminalAnsiColor Color209 = FromColor(AnsiColors.Color209);
        public static readonly TerminalAnsiColor Color210 = FromColor(AnsiColors.Color210);
        public static readonly TerminalAnsiColor Color211 = FromColor(AnsiColors.Color211);
        public static readonly TerminalAnsiColor Color212 = FromColor(AnsiColors.Color212);
        public static readonly TerminalAnsiColor Color213 = FromColor(AnsiColors.Color213);
        public static readonly TerminalAnsiColor Color214 = FromColor(AnsiColors.Color214);
        public static readonly TerminalAnsiColor Color215 = FromColor(AnsiColors.Color215);
        public static readonly TerminalAnsiColor Color216 = FromColor(AnsiColors.Color216);
        public static readonly TerminalAnsiColor Color217 = FromColor(AnsiColors.Color217);
        public static readonly TerminalAnsiColor Color218 = FromColor(AnsiColors.Color218);
        public static readonly TerminalAnsiColor Color219 = FromColor(AnsiColors.Color219);
        public static readonly TerminalAnsiColor Color220 = FromColor(AnsiColors.Color220);
        public static readonly TerminalAnsiColor Color221 = FromColor(AnsiColors.Color221);
        public static readonly TerminalAnsiColor Color222 = FromColor(AnsiColors.Color222);
        public static readonly TerminalAnsiColor Color223 = FromColor(AnsiColors.Color223);
        public static readonly TerminalAnsiColor Color224 = FromColor(AnsiColors.Color224);
        public static readonly TerminalAnsiColor Color225 = FromColor(AnsiColors.Color225);
        public static readonly TerminalAnsiColor Color226 = FromColor(AnsiColors.Color226);
        public static readonly TerminalAnsiColor Color227 = FromColor(AnsiColors.Color227);
        public static readonly TerminalAnsiColor Color228 = FromColor(AnsiColors.Color228);
        public static readonly TerminalAnsiColor Color229 = FromColor(AnsiColors.Color229);
        public static readonly TerminalAnsiColor Color230 = FromColor(AnsiColors.Color230);
        public static readonly TerminalAnsiColor Color231 = FromColor(AnsiColors.Color231);
        public static readonly TerminalAnsiColor Color232 = FromColor(AnsiColors.Color232);
        public static readonly TerminalAnsiColor Color233 = FromColor(AnsiColors.Color233);
        public static readonly TerminalAnsiColor Color234 = FromColor(AnsiColors.Color234);
        public static readonly TerminalAnsiColor Color235 = FromColor(AnsiColors.Color235);
        public static readonly TerminalAnsiColor Color236 = FromColor(AnsiColors.Color236);
        public static readonly TerminalAnsiColor Color237 = FromColor(AnsiColors.Color237);
        public static readonly TerminalAnsiColor Color238 = FromColor(AnsiColors.Color238);
        public static readonly TerminalAnsiColor Color239 = FromColor(AnsiColors.Color239);
        public static readonly TerminalAnsiColor Color240 = FromColor(AnsiColors.Color240);
        public static readonly TerminalAnsiColor Color241 = FromColor(AnsiColors.Color241);
        public static readonly TerminalAnsiColor Color242 = FromColor(AnsiColors.Color242);
        public static readonly TerminalAnsiColor Color243 = FromColor(AnsiColors.Color243);
        public static readonly TerminalAnsiColor Color244 = FromColor(AnsiColors.Color244);
        public static readonly TerminalAnsiColor Color245 = FromColor(AnsiColors.Color245);
        public static readonly TerminalAnsiColor Color246 = FromColor(AnsiColors.Color246);
        public static readonly TerminalAnsiColor Color247 = FromColor(AnsiColors.Color247);
        public static readonly TerminalAnsiColor Color248 = FromColor(AnsiColors.Color248);
        public static readonly TerminalAnsiColor Color249 = FromColor(AnsiColors.Color249);
        public static readonly TerminalAnsiColor Color250 = FromColor(AnsiColors.Color250);
        public static readonly TerminalAnsiColor Color251 = FromColor(AnsiColors.Color251);
        public static readonly TerminalAnsiColor Color252 = FromColor(AnsiColors.Color252);
        public static readonly TerminalAnsiColor Color253 = FromColor(AnsiColors.Color253);
        public static readonly TerminalAnsiColor Color254 = FromColor(AnsiColors.Color254);
        public static readonly TerminalAnsiColor Color255 = FromColor(AnsiColors.Color255);
    }
}
