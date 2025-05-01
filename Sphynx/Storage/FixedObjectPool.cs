// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;

namespace Sphynx.Storage
{
    /// <summary>
    /// A thread-safe object pool of fixed size. The pool starts off empty, requiring items to enqueued before
    /// they can be taken from the pool.
    /// </summary>
    /// <typeparam name="T">The type of objects within this pool.</typeparam>
    public class FixedObjectPool<T> where T : class
    {
        // The following implementation is based on Roslyn's ObjectPool`1 source code:
        // https://github.com/dotnet/roslyn/blob/main/src/Dependencies/PooledObjects/ObjectPool%601.cs
        //
        // The MIT License (MIT)
        //
        // Copyright (c) .NET Foundation and Contributors
        //
        // All rights reserved.
        //
        // Permission is hereby granted, free of charge, to any person obtaining a copy
        // of this software and associated documentation files (the "Software"), to deal
        // in the Software without restriction, including without limitation the rights
        // to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
        // copies of the Software, and to permit persons to whom the Software is
        // furnished to do so, subject to the following conditions:
        //
        // The above copyright notice and this permission notice shall be included in all
        // copies or substantial portions of the Software.
        //
        // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
        // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
        // FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
        // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
        // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
        // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
        // SOFTWARE.

        // Whether to perform lockless optimizations which can falsely report pool emptiness.
        private readonly bool _fastChecks;

        // Incremented each time an item is inserted into the pool. Only applicable when _fastChecks is false.
        private int _version;

        // We store the first item in its own field since we expect to be able to satisfy most requests
        // with it.
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
            _items = new T[size - 1];
            _fastChecks = fastChecks;
        }

        public bool TryTake(out T? item)
        {
            while (true)
            {
                int version = _fastChecks ? -1 : Volatile.Read(ref _version);

                // If allowed, we de not synchronize our initial read.
                // In the worst case, we miss some recently returned objects.
                item = _fastChecks ? _firstItem : Volatile.Read(ref _firstItem);

                bool reservedFirstItem = item is not null && item == Interlocked.CompareExchange(ref _firstItem, null, item);

                if (reservedFirstItem)
                    return true;

                if (TryTakeSlow(out item))
                    return true;

                if (_fastChecks || version == Volatile.Read(ref _version))
                {
                    item = null;
                    return false;
                }
            }
        }

        private bool TryTakeSlow(out T? item)
        {
            for (int i = 0; i < _items.Length; i++)
            {
                // If allowed, we de not synchronize our read.
                // In the worst case, we miss some recently returned objects.
                item = _fastChecks ? _items[i] : Volatile.Read(ref _items[i]);

                bool reservedItem = item is not null && item == Interlocked.CompareExchange(ref _items[i], null, item);

                if (reservedItem)
                    return true;
            }

            item = null;
            return false;
        }

        public bool Return(T obj)
        {
            Debug.Assert(obj != null);

            var firstItem = _fastChecks ? _firstItem : Volatile.Read(ref _firstItem);

            if (firstItem is null)
            {
                if (_fastChecks)
                {
                    // In the worst case, two objects may be stored into same slot.
                    // It is very unlikely to happen and will only mean that one of the objects will get collected.
                    _firstItem = firstItem;
                    return true;
                }

                if (firstItem == Interlocked.CompareExchange(ref _firstItem, obj, firstItem))
                {
                    Interlocked.Increment(ref _version);
                    return true;
                }
            }

            return ReturnSlow(obj);
        }

        private bool ReturnSlow(T obj)
        {
            Debug.Assert(obj != null);

            for (int i = 0; i < _items.Length; i++)
            {
                var item = _fastChecks ? _items[i] : Volatile.Read(ref _items[i]);

                if (item is null)
                {
                    if (_fastChecks)
                    {
                        // In the worst case, two objects may be stored into same slot.
                        // It is very unlikely to happen and will only mean that one of the objects will get collected.
                        _items[i] = obj;
                        return true;
                    }

                    if (item == Interlocked.CompareExchange(ref _items[i], obj, item))
                    {
                        Interlocked.Increment(ref _version);
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
