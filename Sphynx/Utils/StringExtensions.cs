// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Sphynx.Utils
{
    public static class StringExtensions
    {
        /// <summary>
        /// Constructs a string with exactly <paramref name="count"/> occurences of the specified string within it.
        /// </summary>
        /// <param name="str">The string to repeat.</param>
        /// <param name="count">Total number of times <pararef name="str"/> should appear within the resulting string.
        /// <c>0</c> gives the <see cref="string.Empty">empty string.</see></param>
        /// <returns>The repeated string.</returns>
        public static string Repeat(this string str, int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), $"{nameof(count)} cannot be negative");
            }

            if (count == 0) return string.Empty;
            if (count == 1) return str;
            if (str.Length == 1) return Repeat(str[0], count);

            return string.Create(str.Length * count,
                str,
            [MethodImpl(MethodImplOptions.AggressiveOptimization)]
            (data, s) =>
                {
                    var strSpan = s.AsSpan();
                    for (int i = 0; i < count; i++) strSpan.CopyTo(data.Slice(i * s.Length, s.Length));
                });
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static int Count(this string str, char ch)
        {
            int count = 0;
            for (int i = 0; (i = str.IndexOf(ch, i)) != -1; count++, i++) { }
            return count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Repeat(this char ch, int count) => new(ch, count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string RemoveTabs(this string str, int spaceCount = 4) => str.Replace("\t", ' '.Repeat(spaceCount));

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLatin1Printable(this char ch) => ch is >= '\x20' and <= '\x7E' or >= '\xA0' and <= '\xFF';

        public static int CodePointCount(this ReadOnlySpan<char> str)
        {
            int cpCount = 0;
            for (int i = 0; i < str.Length; i++)
            {
                var ch = str[i];
                if (char.IsSurrogate(ch))
                {
                    if (i >= str.Length - 1) throw new ArgumentException();
                    var nextCh = str[++i];
                    Debug.Assert(char.IsSurrogatePair(ch, nextCh));
                }
                cpCount++;
            }
            return cpCount;
        }

        public static int CodePointCount(this Span<char> str) => CodePointCount((ReadOnlySpan<char>)str);

        public static int CodePointCount(this string str)
        {
            return CodePointCount(str.AsSpan());
        }

        public static int GraphemeCount(this string str)
        {
            var sp = str.AsSpan();
            var count = 0;
            for (int i = 0; i < str.Length;)
            {
                i += StringInfo.GetNextTextElementLength(str, i);
                count++;
            }
            return count;
        }

        public static List<int> ToCodePoints(this string str)
        {
            var cps = new List<int>(str.Length);
            for (int i = 0; i < str.Length; i++)
            {
                int cp;
                var ch = str[i];
                if (char.IsSurrogate(ch))
                {
                    if (i >= str.Length - 1) throw new InvalidOperationException();
                    var nextCh = str[++i];
                    Debug.Assert(char.IsSurrogatePair(ch, nextCh));
                    cp = char.ConvertToUtf32(ch, nextCh);
                }
                else
                {
                    cp = ch;
                }
                cps.Add(cp);
            }
            return cps;
        }

        public static int ToCodePoints(this string str, Span<int> codePoints)
        {
            int cps = 0;
            for (int i = 0; cps < codePoints.Length && i < str.Length; i++)
            {
                int cp;
                var ch = str[i];
                if (char.IsSurrogate(ch))
                {
                    if (i >= str.Length - 1) throw new InvalidOperationException();
                    var nextCh = str[++i];
                    Debug.Assert(char.IsSurrogatePair(ch, nextCh));
                    cp = char.ConvertToUtf32(ch, nextCh);
                }
                else
                {
                    cp = ch;
                }
                codePoints[cps++] = cp;
            }
            return cps;
        }

        /// <summary>
        /// Determines the (maximum) number of (contiguous) chars the can be safely taken from the string to create a
        /// valid UTF-16 character string.
        /// </summary>
        /// <param name="str">The string in question.</param>
        /// <returns>The number of chars which are safe to take from.</returns>
        public static int SafeLength(this string str)
        {
            int i = 0;
            for (; i < str.Length; i++)
            {
                var ch = str[i];
                if (!char.IsSurrogate(ch)) continue;
                if (i >= str.Length - 1) break;
                var nextCh = str[i + 1];
                if (!char.IsSurrogatePair(ch, nextCh)) break;
                i++;
            }
            return i;
        }

        public static int LineCount(this string str) => LineCount(str, Environment.NewLine);
        public static int LineCount(this string str, string newLine)
        {
            if (str.Length <= 0) return 0;
            int count = 1;
            for (int i = str.IndexOf(newLine); i < str.Length && i != -1; i = str.IndexOf(newLine, i + newLine.Length))
            {
                count++;
            }
            return count;
        }

        public static byte? GetAscii(this Rune r)
        {
            if (!r.IsAscii) return null;
            Span<byte> b = stackalloc byte[1];
            var count = r.EncodeToUtf8(b);
            Debug.Assert(count == 1);
            return b[0];
        }

        public static byte? GetAscii(this in Rune? r) => r?.GetAscii();
    }
}
