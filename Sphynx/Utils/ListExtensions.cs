// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.CompilerServices;

namespace Sphynx.Utils
{
    public static class ListExtensions
    {
        public static T Pop<T>(this IList<T> list)
        {
            var i = list.Count - 1;
            var item = list[i];
            list.RemoveAt(i);
            return item;
        }

        public static void PushFront<T>(this IList<T> list, in T item) => list.Insert(0, item);
        public static void Push<T>(this IList<T> list, in T item) => list.Add(item);

        public static void Resize<T>(this List<T> list, int count, Func<T> producer)
        {
            if (count < list.Count)
            {
                list.RemoveRange(count, list.Count - count);
            }
            else
            {
                list.EnsureCapacity(count);
                for (; count > 0; count--)
                {
                    list.Add(producer());
                }
            }
        }

        public static void Resize<T>(this List<T> list, int count, in T value)
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
        public static void Resize<T>(this List<T> list, int count) where T : new() => Resize(list, count, static () => new T());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Grow<T>(this List<T> list, int count, in T value) => Resize(list, list.Count + count, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Grow<T>(this List<T> list, int count) where T : new() => Resize(list, list.Count + count, static () => new T());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Grow<T>(this List<T> list, int count, Func<T> producer) => Resize(list, list.Count + count, producer);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Shrink<T>(this List<T> list, int count) => Resize(list, list.Count - count, default!);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Reserve<T>(this List<T> list, int count) => list.EnsureCapacity(list.Capacity + count);

        public static LinkedListNode<T>? GetNode<T>(this LinkedList<T> list, int i)
        {
            if (i >= list.Count || -i > list.Count) return null;

            LinkedListNode<T>? it = null;

            if (i >= 0)
            {
                if (i > list.Count / 2)
                {
                    it = list.Last!;
                    for (int n = 0; n < list.Count - i - 1; n++, it = it.Previous!) { }
                }
                else
                {
                    it = list.First!;
                    for (int n = 0; n < i; n++, it = it.Next!) { }
                }
            }

            return it ?? GetNode(list, list.Count - (-i - 1))!;
        }
    }
}
