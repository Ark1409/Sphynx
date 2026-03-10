// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Sphynx.Storage
{
    /// <summary>
    /// A thread-safe object pool holding strong references to its items.
    /// </summary>
    /// <typeparam name="T">The type of objects within this pool.</typeparam>
    public class ObjectPool<T> : IObjectPool<T> where T : class
    {
        private readonly ConcurrentBag<T> _items = new();
        private readonly int _maxSize;

        // To avoid freezing the whole bag when returning to the pool
        private int _count;
        private readonly object? _syncLock;

        private volatile Func<T>? _allocator;

        public Func<T>? Allocator
        {
            get => _allocator;
            set => _allocator = value;
        }

        /// <summary>
        /// Creates a new fixed size pool.
        /// </summary>
        public ObjectPool() : this(Environment.ProcessorCount * 2)
        {
        }

        /// <summary>
        /// Creates a new fixed size pool.
        /// </summary>
        /// <param name="maxSize">The maximum number of items of <typeparamref name="T"/> which the pool can hold at once.</param>
        /// <param name="hardLimit">Whether to hard limit the maximum size, which could in turn decrease performance.</param>
        public ObjectPool(int maxSize, bool hardLimit = false)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxSize, 0);
            _maxSize = maxSize;
            _syncLock = hardLimit ? new object() : null;
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
                Interlocked.Decrement(ref _count);
                return true;
            }
            else
            {
                var allocator = _allocator;

                if (allocator != null)
                {
                    item = allocator.Invoke();
                    return true;
                }

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
            return _syncLock == null ? ReturnFast(obj) : ReturnSlow(obj);
        }

        private bool ReturnFast(T obj)
        {
            // Optimistically performed a non-interlocked read
            if (_count >= _maxSize)
                return false;

            _items.Add(obj);
            Interlocked.Increment(ref _count);
            return true;
        }

        private bool ReturnSlow(T obj)
        {
            Debug.Assert(_syncLock != null);

            lock (_syncLock)
            {
                if (_count >= _maxSize)
                    return false;

                _items.Add(obj);
                Interlocked.Increment(ref _count); // TryTake does not take the sync lock
                return true;
            }
        }

        public void Clear(Action<T>? clearAction = null)
        {
            int count = Volatile.Read(ref _count);

            while (count-- > 0 && TryTake(out var item))
                clearAction?.Invoke(item);
        }
    }
}
