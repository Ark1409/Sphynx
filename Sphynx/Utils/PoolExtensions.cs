// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;

namespace Sphynx.Utils
{
    public static class PoolExtensions
    {
        public static ValueInvokeOnDisposal<T[]> CreateReturner<T>(this ArrayPool<T> arrayPool, T[] arr)
        {
            return new(arr, arr => arrayPool.Return(arr));
        }

        public static ArrayPoolReturner<T> AutoRent<T>(this ArrayPool<T> arrayPool, int minimumLength)
        {
            var arr = arrayPool.Rent(minimumLength);
            return new(arrayPool, arr);
        }

        public readonly struct ArrayPoolReturner<T> : IDisposable
        {
            private readonly ArrayPool<T> _pool;
            public readonly T[] Array;

            public ArrayPoolReturner(ArrayPool<T> pool, T[] ret)
            {
                _pool = pool;
                Array = ret;
            }

            public void Dispose()
            {
                _pool.Return(Array);
            }

            public T this[int index] => Array[index];
            public static implicit operator T[](ArrayPoolReturner<T> a) => a.Array;
        }
    }
}
