// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;

namespace Sphynx.Utils
{
    public static class QueueExtensions
    {
        public static void Enqueue<T>(this Queue<T> queue, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                queue.Enqueue(item);
            }
        }

        public static void Enqueue<T>(this Queue<T> queue, ReadOnlySpan<T> items)
        {
            for (int i = 0; i < items.Length; i++)
            {
                queue.Enqueue(items[i]);
            }
        }

        private const int BLOCKCOPY_THRESHOLD = 1024;
        public static List<T> Dequeue<T>(this Queue<T> queue, int count)
        {
            List<T> list = null!;
            int amountRemove = Math.Min(queue.Count, count);
            if (amountRemove >= queue.Count && queue.Count >= BLOCKCOPY_THRESHOLD)
            {
                list = new List<T>(queue.ToArray());
                queue.Clear();
            }
            else
            {
                list = new List<T>(amountRemove);
                for (int i = 0; i < amountRemove; i++)
                {
                    list.Add(queue.Dequeue());
                }
            }

            return list;
        }

        public static int Dequeue<T>(this Queue<T> queue, Span<T> storage, bool reverse = false)
        {
            int amountRemove = Math.Min(queue.Count, storage.Length);
            if (amountRemove >= queue.Count && queue.Count >= BLOCKCOPY_THRESHOLD)
            {
                using var arr = ArrayPool<T>.Shared.AutoRent(amountRemove);
                queue.CopyTo(arr, 0);
                queue.Clear();
                arr.AsSpan()[..amountRemove].CopyTo(storage);
                if (reverse) storage[..amountRemove].Reverse();
                return amountRemove;
            }

            for (int i = 0; i < amountRemove; i++)
            {
                storage[i] = queue.Dequeue();
            }
            if (reverse)
            {
                storage[..amountRemove].Reverse();
            }

            return amountRemove;
        }
    }
}
