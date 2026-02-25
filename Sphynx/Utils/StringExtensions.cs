// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

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
    }
}
