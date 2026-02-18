// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Sphynx.Utils
{
    public static class RangeExtensions
    {
        public static bool HasOverlap(this IEnumerable<Range> ranges, int length)
        {
            if (!ranges.Any()) return false;
            var array = ranges.Select(a => a.GetOffsetAndLength(length)).ToArray();
            Array.Sort(array, (a, b) => a.Offset.CompareTo(b.Offset));

            for (int i = 0; i + 1 < array.Length; i++)
            {
                var item = array[i];
                var next = array[i + 1];
                if (next.Offset < item.Offset + item.Length) return true;
            }
            return false;
        }

        public static bool HasOverlap(int length, params Range[] ranges) => ranges.HasOverlap(length);

        public static bool HasOverlap(this Range r, int length, params Range[] ranges) => HasOverlap(ranges.Append(r), length);
    }
}
