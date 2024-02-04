using System.Runtime.CompilerServices;

namespace Sphynx.Client.Utils
{
    internal static class ListExtensions
    {
        public static void Resize<T>(this List<T> list, int count, T value)
        {
            if (count < list.Count)
            {
                list.RemoveRange(count, list.Count - count);
            }
            else
            {
                list.AddRange(Enumerable.Repeat(value, count - list.Count));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Resize<T>(this List<T> list, int count) where T : new() => Resize(list, count, new T());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Grow<T>(this List<T> list, int count, T value) => Resize(list, list.Count + count, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Grow<T>(this List<T> list, int count) where T : new() => Resize(list, list.Count + count, new T());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Shrink<T>(this List<T> list, int count, T value) => Resize(list, list.Count - count, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Shrink<T>(this List<T> list, int count) where T : new() => Resize(list, list.Count - count, new T());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Reserve<T>(this List<T> list, int count) => list.EnsureCapacity(list.Capacity + count);
    }
}
