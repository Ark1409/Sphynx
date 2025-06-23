// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Sphynx.Storage
{
    /// <summary>
    /// A thread-safe object pool of fixed size holding strong references to its items.
    /// </summary>
    /// <typeparam name="T">The type of objects within this pool.</typeparam>
    /// <remarks>The pool starts off empty, requiring items to enqueued before they can be taken from the pool.</remarks>
    public class FixedObjectPool<T> where T : class
    {
        // Whether to perform lockless optimizations which can falsely report pool emptiness.
        private readonly bool _fastChecks;

        // We store the first item in its own field since we expect to be able to satisfy most requests with it.
        private T? _firstItem;
        private readonly T?[] _items;

        /// <summary>
        /// Creates a new fixed size pool
        /// </summary>
        /// <param name="size">The number of items of <typeparamref name="T"/> which the pool can hold at once.</param>
        /// <param name="fastChecks">Whether to perform lockless optimizations which can falsely report pool emptiness,
        /// but can increase performance.</param>
        public FixedObjectPool(int size, bool fastChecks = true)
        {
            _items = new T?[size - 1];
            _fastChecks = fastChecks;
        }

        /// <summary>
        /// Attempts to take from the pool.
        /// </summary>
        /// <param name="item">The item taken from the pool.</param>
        /// <returns>Whether we could take from the pool.</returns>
        public bool TryTake(out T? item)
        {
            // If allowed, we de not synchronize our initial read.
            // In the worst case, we miss some recently returned objects.
            item = _fastChecks ? _firstItem : Volatile.Read(ref _firstItem);

            if (item is null)
                return false;

            if (Interlocked.CompareExchange(ref _firstItem, null, item) == item)
                return true;

            return TryTakeSlow(out item);
        }

        private bool TryTakeSlow(out T? item)
        {
            for (int i = 0; i < _items.Length; i++)
            {
                // If allowed, we de not synchronize our read. In the worst case, we miss some recently returned objects.
                item = _fastChecks ? _items[i] : Volatile.Read(ref _items[i]);

                if (item is null)
                    continue;

                if (Interlocked.CompareExchange(ref _items[i], null, item) == item)
                    return true;
            }

            item = null;
            return false;
        }

        /// <summary>
        /// Attempts to return an object to the pool
        /// </summary>
        /// <param name="obj">The object to return.</param>
        /// <returns>Whether the object could be returned, or if the pull was full.</returns>
        public bool Return(T obj)
        {
            ArgumentNullException.ThrowIfNull(obj, nameof(obj));

            var firstItem = _fastChecks ? _firstItem : Volatile.Read(ref _firstItem);

            // The first slot is already full.
            if (firstItem is not null)
                return false;

            // Try and reserve the first slot for ourselves.
            if (Interlocked.CompareExchange(ref _firstItem, obj, firstItem) == firstItem)
                return true;

            return ReturnSlow(obj);
        }

        private bool ReturnSlow(T obj)
        {
            for (int i = 0; i < _items.Length; i++)
            {
                var itemRef = _fastChecks ? _items[i] : Volatile.Read(ref _items[i]);

                if (itemRef is not null)
                    continue;

                if (Interlocked.CompareExchange(ref _items[i], obj, itemRef) == itemRef)
                    return true;
            }

            return false;
        }
    }
}
