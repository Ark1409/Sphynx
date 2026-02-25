// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
