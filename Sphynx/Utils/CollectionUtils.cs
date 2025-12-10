namespace Sphynx.Utils
{
    internal static class CollectionUtils
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