// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.CompilerServices;
using Sphynx.Collections;

namespace Sphynx.Streams
{
    public class RewindableStream : Stream
    {
        private readonly Stream _stream;
        private readonly Deque<byte> _bytes = new();
        private int _disposed = 0;

        public RewindableStream(Stream stream)
        {
            _stream = stream;
        }

        /// <summary>
        /// Retrieves a lower bound on the number of bytes that can be read without blocking.
        /// Practically, this returns the number of bytes stored in the <see cref="UnRead(byte, bool)"/> buffer ahead of
        /// the stream.
        /// </summary>
        public int MinimumPending => _bytes.Count;

        public override bool CanRead => _disposed != 0 && (_bytes.Count > 0 || _stream.CanRead);

        public override bool CanSeek => false;

        public override bool CanWrite => _disposed != 0 && _stream.CanWrite;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override bool CanTimeout => _stream.CanTimeout;
        public override int ReadTimeout { get => _stream.ReadTimeout; set => _stream.ReadTimeout = value; }
        public override int WriteTimeout { get => _stream.WriteTimeout; set => _stream.WriteTimeout = value; }

        public override void Flush() => _stream.Flush();

        public override int Read(Span<byte> b)
        {
            ThrowIfDisposed();

            int readCount = _bytes.DequeueFront(b);

            // if (readCount >= b.Length) return readCount;
            if (readCount > 0) return readCount;

            try
            {
                return readCount += _stream.Read(b[readCount..]);
            }
            catch
            {
                return readCount;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ThrowIfDisposed();
            var b = new Span<byte>(buffer, offset, count);

            int readCount = _bytes.DequeueFront(b);

            if (readCount > 0) return readCount;

            try
            {
                return readCount += _stream.Read(buffer, offset + readCount, count - readCount);
            }
            catch
            {
                return readCount;
            }
        }

        public override int ReadByte()
        {
            ThrowIfDisposed();
            if (_bytes.Count > 0) return _bytes.DequeueFront();
            return _stream.ReadByte();
        }

        public override ValueTask<int> ReadAsync(Memory<byte> b, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();

            int readCount = _bytes.DequeueFront(b.Span);

            if (readCount > 0) return ValueTask.FromResult(readCount);

            return ReadAsyncSub(b, readCount, readCount <= 0 ? cancellationToken : default);

            [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
            async ValueTask<int> ReadAsyncSub(Memory<byte> b, int readCount, CancellationToken cancellationToken)
            {
                try
                {
                    var extraCount = await _stream.ReadAsync(b.Slice(readCount), cancellationToken);
                    return readCount + extraCount;
                }
                catch
                {
                    return readCount;
                }
            }
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();
            var b = new Span<byte>(buffer, offset, count);
            int readCount = _bytes.DequeueFront(b);

            if (readCount > 0) return Task.FromResult(readCount);

            var task = _stream.ReadAsync(buffer, offset + readCount, count - readCount, readCount <= 0 ? cancellationToken : default);
            if (readCount > 0)
                task = task.ContinueWith(static (t, readCount) => (int)readCount! + (t.IsCompletedSuccessfully ? t.Result : 0),
                        readCount);
            return task;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            ThrowIfDisposed();
            _stream.Write(buffer, offset, count);
        }

        /// <summary>
        /// Inserts the byte into the stream such that <c>Read</c> will read it before reading the underlying stream.
        /// </summary>
        /// <param name="b">The byte to insert.</param>
        /// <param name="front">Whether to place the byte at the front of back of the stream.</param>
        /// <seealso cref="PushFront(ReadOnlySpan{byte}, bool)"/>
        /// <seealso cref="PushBack(ReadOnlySpan{byte}, bool)"/>
        public void UnRead(byte b, bool front = true)
        {
            Span<byte> buf = [b];
            UnRead(buf, front);
        }

        /// <summary>
        /// Inserts the bytes into the stream such that <c>Read</c> will read them before reading the underlying stream.
        /// </summary>
        /// <param name="bytes">The bytes to insert.</param>
        /// <param name="front">Whether to place the bytes at the front of back of the stream.</param>
        /// <seealso cref="PushFront(ReadOnlySpan{byte}, bool)"/>
        /// <seealso cref="PushBack(ReadOnlySpan{byte}, bool)"/>
        public void UnRead(ReadOnlySpan<byte> bytes, bool front = true)
        {
            if (front) PushFront(bytes);
            else PushBack(bytes);
        }

        /// <summary>
        /// Inserts the bytes into the stream such that the next <c>Read</c> call reads them.
        /// </summary>
        /// <param name="bytes">The bytes to insert.</param>
        /// <param name="reverse">If <c>true</c>, then <c>items[0]</c> will be at the front of the read queue after this function returns.
        /// Otherwise, <c>items[^1]</c> will be at the front. Defaults to <c>true</c>.</param>
        public void PushFront(ReadOnlySpan<byte> bytes, bool reverse = true)
        {
            _bytes.EnqueueFront(bytes, reverse);
        }

        /// <summary>
        /// Inserts the bytes into the stream such that <c>Read</c> will read them before reading the underlying stream.
        /// This function differs from <see cref="PushFront(ReadOnlySpan{byte}, bool)"/> in that it places the items at
        /// the end of the buffer.
        /// </summary>
        /// <param name="bytes">The bytes to insert.</param>
        /// <param name="reverse">If <c>true</c>, then <c>items[0]</c> will be at the back of the read queue after this function returns.
        /// Otherwise, <c>items[^1]</c> will be at the back. Defaults to <c>false</c>.</param>
        public void PushBack(ReadOnlySpan<byte> bytes, bool reverse = false)
        {
            _bytes.EnqueueBack(bytes, reverse);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return _stream.WriteAsync(buffer, cancellationToken);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return _stream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override void WriteByte(byte value)
        {
            ThrowIfDisposed();
            _stream.WriteByte(value);
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed >= 2) return;
            _disposed = disposing ? 2 : 1;
            if (disposing)
            {
                _stream.Dispose();
            }
        }
        private void ThrowIfDisposed()
        {
            if (_disposed != 0)
            {
                throw new ObjectDisposedException(nameof(RewindableStream), "Stream already closed");
            }
        }
    }
}
