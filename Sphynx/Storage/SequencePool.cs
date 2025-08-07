// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

// ReSharper disable ArrangeThisQualifier
// ReSharper disable InconsistentNaming

using System.Buffers;
using Nerdbank.Streams;

namespace Sphynx.Storage
{
    //
    // MIT License
    //
    // Copyright (c) 2017 Yoshifumi Kawai and contributors
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

    /// <summary>
    /// A thread-safe, alloc-free reusable <see cref="Sequence{byte}"/> pool.
    /// </summary>
    public sealed class SequencePool
    {
        /// <summary>
        /// A thread-safe pool of reusable <see cref="Sequence{T}"/> objects.
        /// </summary>
        public static readonly SequencePool Shared = new SequencePool();

        /// <summary>
        /// The value to use for <see cref="Sequence{T}.MinimumSpanLength"/>.
        /// </summary>
        /// <remarks>
        /// Individual users that want a different value for this can modify the setting on the rented <see cref="Sequence{T}"/>
        /// or by supplying their own <see cref="IBufferWriter{T}" />.
        /// </remarks>
        /// <devremarks>
        /// We use 32KB so that when LZ4Codec.MaximumOutputLength is used on this length it does not require a
        /// buffer that would require the Large Object Heap.
        /// </devremarks>
        private const int MinimumSpanLength = 16 * 1024;

        private readonly int maxSize;
        private readonly Stack<Sequence<byte>> pool = new Stack<Sequence<byte>>();

        /// <summary>
        /// The array pool which we share with all <see cref="Sequence{T}"/> objects created by this <see cref="SequencePool"/> instance.
        /// </summary>
        private readonly ArrayPool<byte> arrayPool;

        /// <summary>
        /// Initializes a new instance of the <see cref="SequencePool"/> class.
        /// </summary>
        /// <remarks>
        /// We use a <see cref="maxSize"/> that allows every processor to be involved in messagepack serialization concurrently,
        /// plus one nested serialization per processor (since LZ4 and sometimes other nested serializations may exist).
        /// </remarks>
        public SequencePool()
            : this(Environment.ProcessorCount * 2, ArrayPool<byte>.Create(80 * 1024, 100))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SequencePool"/> class.
        /// </summary>
        /// <param name="maxSize">The maximum size to allow the pool to grow.</param>
        /// <devremarks>
        /// We allow 100 arrays to be shared (instead of the default 50) and reduce the max array length from the default 1MB to something more reasonable for our expected use.
        /// </devremarks>
        public SequencePool(int maxSize)
            : this(maxSize, ArrayPool<byte>.Create(80 * 1024, 100))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SequencePool"/> class.
        /// </summary>
        /// <param name="maxSize">The maximum size to allow the pool to grow.</param>
        /// <param name="arrayPool">Array pool that will be used.</param>
        public SequencePool(int maxSize, ArrayPool<byte> arrayPool)
        {
            this.maxSize = maxSize;
            this.arrayPool = arrayPool;
        }

        /// <summary>
        /// Gets an instance of <see cref="Sequence{T}"/>
        /// This is taken from the recycled pool if one is available; otherwise a new one is created.
        /// </summary>
        /// <returns>The rental tracker that provides access to the object as well as a means to return it.</returns>
        public Rental Rent()
        {
            lock (this.pool)
            {
                if (this.pool.Count > 0)
                {
                    return new Rental(this, this.pool.Pop());
                }
            }

            // Configure the newly created object to share a common array pool with the other instances,
            // otherwise each one will have its own ArrayPool which would likely waste a lot of memory.
            return new Rental(this, new Sequence<byte>(this.arrayPool)
            {
                MinimumSpanLength = MinimumSpanLength, AutoIncreaseMinimumSpanLength = true
            });
        }

        private void Return(Sequence<byte> value)
        {
            value.Reset();
            lock (this.pool)
            {
                if (this.pool.Count < this.maxSize)
                {
                    // Reset to preferred settings in case the renter changed them.
                    value.MinimumSpanLength = MinimumSpanLength;
                    value.AutoIncreaseMinimumSpanLength = true;

                    this.pool.Push(value);
                }
            }
        }

        public readonly struct Rental : IDisposable
        {
            private readonly SequencePool owner;

            internal Rental(SequencePool owner, Sequence<byte> value)
            {
                this.owner = owner;
                this.Value = value;
            }

            /// <summary>
            /// Gets the recyclable <see cref="Sequence{T}"/>.
            /// </summary>
            public Sequence<byte> Value { get; }

            /// <summary>
            /// Returns the recyclable object to the pool.
            /// </summary>
            /// <remarks>
            /// The instance is cleaned first, if a clean delegate was provided.
            /// It is dropped instead of being returned to the pool if the pool is already at its maximum size.
            /// </remarks>
            public void Dispose()
            {
                this.owner?.Return(this.Value);
            }
        }
    }
}
