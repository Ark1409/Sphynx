// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Sphynx.Utils
{
    public static class SpanExtensions
    {
        public static T? Max<T>(this in Span<T> span) where T : struct, IComparable<T>
        {
            if (span.Length == 0) return null;
            var max = span[0];
            for (int i = 1; i < span.Length; i++)
            {
                var val = span[i];
                int ret = Comparer<T>.Default.Compare(max, val);
                if (ret > 0) max = val;
            }
            return max;
        }

        public static T? Max<T>(this in ReadOnlySpan<T> span) where T : struct, IComparable<T>
        {
            if (span.Length == 0) return null;
            var max = span[0];
            for (int i = 1; i < span.Length; i++)
            {
                var val = span[i];
                int ret = Comparer<T>.Default.Compare(max, val);
                if (ret > 0) max = val;
            }
            return max;
        }
    }
}
