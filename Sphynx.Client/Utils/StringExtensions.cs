using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace Sphynx.Client.Utils
{
    internal static class StringExtensions
    {
        public static string Repeat(this string str, int count)
        {
            if (count <= 0) return string.Empty;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Repeat(this char ch, int count) => new(ch, count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string RemoveTabs(this string str, int tabCount = 4) => str.Replace("\t", ' '.Repeat(tabCount));
        
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLatin1Printable(this char ch) => ch is >= '\x20' and <= '\x7E' or >= '\xA0' and <= '\xFF';
    }
}
