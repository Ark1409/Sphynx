// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Sphynx.Utils
{
    public static class CollectionUtils
    {
        public static ISet<T>? CreateNullableSet<T>(IEnumerable<T>? enumerable)
        {
            if (enumerable is null) return null;
            return enumerable as ISet<T> ?? new HashSet<T>(enumerable);
        }

        public static IList<T>? CreateNullableList<T>(IEnumerable<T>? enumerable)
        {
            if (enumerable is null) return null;
            return enumerable as IList<T> ?? new List<T>(enumerable);
        }
    }
}
