// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Sphynx.Storage
{
    /// <summary>
    /// A thread-safe object pool of fixed size holding strong references to its items.
    /// </summary>
    /// <typeparam name="T">The type of objects within this pool.</typeparam>
    /// <remarks>The pool starts off empty, requiring items to enqueued before they can be taken from the pool.</remarks>
    public class FixedObjectPool<T> where T : class
    {
        private readonly ConcurrentBag<T> _items = new();
        private readonly int _size;

        // An upper bound on the current item count
        private int _upperCount;

        /// <summary>
        /// Creates a new fixed size pool.
        /// </summary>
        public FixedObjectPool() : this(Environment.ProcessorCount * 2)
        {
        }

        /// <summary>
        /// Creates a new fixed size pool.
        /// </summary>
        /// <param name="size">The number of items of <typeparamref name="T"/> which the pool can hold at once.</param>
        public FixedObjectPool(int size)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(_size = size, 0);
        }

        /// <summary>
        /// Attempts to take from the pool.
        /// </summary>
        /// <param name="item">The item taken from the pool.</param>
        /// <returns>Whether we could take from the pool.</returns>
        public bool TryTake([NotNullWhen(true)] out T? item)
        {
            if (_items.TryTake(out item))
            {
                Interlocked.Decrement(ref _upperCount);
                return true;
            }
            else
            {
                // This either indicates that some item will be enqueued shortly, or that
                // _upperCount is out-of-sync with the real item count. Let's leave this
                // be for now, as it would be difficult to try and re-sync the _upperCount
                // while also ensuring no other threads have updated it without any mutual
                // exclusion.

                item = null;
                return false;
            }
        }

        /// <summary>
        /// Attempts to return an object to the pool.
        /// </summary>
        /// <param name="obj">The object to return.</param>
        /// <returns>Whether the object could be returned, or if the pull was full.</returns>
        public bool Return(T obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            // Optimistically perform a non-interlocked read
            if (_upperCount >= _size && _items.Count >= _size)
                return false;

            // We could have a situation where two threads increment the count but only one
            // is able to enqueue their item. To avoid enforcing mutual exclusion, let us
            // simply work with _upperCount as an upper-bound on the actual item count.
            if (Interlocked.Increment(ref _upperCount) > _size)
                return false;

            _items.Add(obj);

            return true;
        }
    }
}
