// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using System.Collections;

namespace Sphynx.Utils
{
    public static class PoolExtensions
    {
        public static ArrayPoolRent<T> CreateReturner<T>(this ArrayPool<T> arrayPool, T[] arr)
        {
            return new(arrayPool, arr);
        }

        public static ArrayPoolRent<T> CreateReturner<T>(this T[] arr, ArrayPool<T> arrayPool)
        {
            return new(arrayPool, arr);
        }

        public static ArrayPoolRent<T> AutoRent<T>(this ArrayPool<T> arrayPool, int minimumLength)
        {
            return new(arrayPool, minimumLength);
        }

        /// <summary>
        /// Automates the process of returning a rented array from an <see cref="ArrayPool{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of the times within the array.</typeparam>
        public struct ArrayPoolRent<T> : IDisposable, IEnumerable<T>
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

            public readonly int Length => Array.Length;
            public readonly long LongLength => Array.LongLength;

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

            public readonly Span<T> AsSpan() => Array.AsSpan();
            public readonly Memory<T> AsMemory() => Array.AsMemory();

            public readonly IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)Array).GetEnumerator();
            readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public readonly ref T this[int index] => ref Array[index];
            public readonly ref T this[Index index] => ref Array[index];
            public static implicit operator T[](ArrayPoolRent<T> a) => a.Array;
            public static implicit operator Span<T>(ArrayPoolRent<T> a) => a.AsSpan();
            public static implicit operator Memory<T>(ArrayPoolRent<T> a) => a.AsMemory();
            public static implicit operator ReadOnlySpan<T>(ArrayPoolRent<T> a) => a.AsSpan();
            public static implicit operator ReadOnlyMemory<T>(ArrayPoolRent<T> a) => a.AsMemory();
        }
    }
}
