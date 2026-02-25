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

        public static ArrayPoolRent<T> AutoRent<T>(this ArrayPool<T> arrayPool, int minimumLength)
        {
            return new(arrayPool, minimumLength);
        }

        /// <summary>
        /// Automates the process of returning a rented array from an <see cref="ArrayPool{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of the times within the array.</typeparam>
        public struct ArrayPoolRent<T> : IDisposable
        {
            private readonly ArrayPool<T> _pool;
            public readonly T[] Array;
            private bool _returned = false;

            public ArrayPoolRent(ArrayPool<T> pool, T[] ret)
            {
                _pool = pool;
                Array = ret;
            }

            public ArrayPoolRent(ArrayPool<T> pool, int minimumLength)
            {
                _pool = pool;
                Array = _pool.Rent(minimumLength);
            }

            /// <summary>
            /// Returns the managed array to the <see cref="ArrayPool{T}"/>
            /// This method can be called more than conce.
            /// </summary>
            public void Return()
            {
                if (!_returned)
                    _pool.Return(Array);
                _returned = true;
            }

            /// <summary>
            /// Returns the managed array to the <see cref="ArrayPool{T}"/>
            /// This method can be called more than conce.
            /// </summary>
            public void Dispose()
            {
                Return();
            }

            public readonly T this[int index] => Array[index];
            public static implicit operator T[](ArrayPoolRent<T> a) => a.Array;
        }
    }
}
