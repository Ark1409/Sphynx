// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;

namespace Sphynx.Collections
{
    public sealed class PureMemoryOwner<T> : IMemoryOwner<T>
    {
        public Memory<T> Memory { get; }
        public PureMemoryOwner(Memory<T> mem) => Memory = mem;
        public PureMemoryOwner(T[]? array) => Memory = new Memory<T>(array);
        public PureMemoryOwner(T[]? array, int start, int length) => Memory = new Memory<T>(array, start, length);
        public void Dispose()
        {
        }

        public static implicit operator PureMemoryOwner<T>(T[]? arr) => new(arr);
    }
}
