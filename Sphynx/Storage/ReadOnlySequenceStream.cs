// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using System.Diagnostics;

namespace Sphynx.Storage
{
    /// <summary>
    /// A poolable stream over a byte <see cref="ReadOnlySequence{T}"/>.
    /// </summary>
    /// <remarks>
    /// Adapted from: <see href="https://github.com/dotnet/Nerdbank.Streams/blob/main/src/Nerdbank.Streams/ReadOnlySequenceStream.cs"/>
    /// </remarks>
    public class ReadOnlySequenceStream : Stream
    {
        private static readonly Task<int> _taskOfZero = Task.FromResult(0);

        /// <inheritdoc/>
        public override long Length => _readOnlySequence.Length;

        /// <inheritdoc/>
        public override long Position
        {
            get => _readOnlySequence.Slice(0, _position).Length;
            set => _position = _readOnlySequence.GetPosition(value, _readOnlySequence.Start);
        }

        /// <inheritdoc/>
        public override bool CanRead => !_disposed;

        /// <inheritdoc/>
        public override bool CanSeek => !_disposed;

        /// <inheritdoc/>
        public override bool CanWrite => false;

        private ReadOnlySequence<byte> _readOnlySequence;

        private SequencePosition _position;
        private bool _allowSeeking;
        private bool _disposed;

        // A reusable task if two consecutive reads return the same number of bytes.
        private Task<int>? _lastReadTask;

        public ReadOnlySequenceStream(ReadOnlySequence<byte> readOnlySequence, bool allowSeeking = true)
        {
            _readOnlySequence = readOnlySequence;
            _position = readOnlySequence.Start;
            _allowSeeking = allowSeeking;
        }

        /// <inheritdoc/>
        public override void Flush() => throw new NotSupportedException();

        /// <inheritdoc/>
        public override Task FlushAsync(CancellationToken cancellationToken) => Task.FromException(new NotSupportedException());

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            ReadOnlySequence<byte> remaining = _readOnlySequence.Slice(_position);
            ReadOnlySequence<byte> toCopy = remaining.Slice(0, Math.Min(count, remaining.Length));

            _position = toCopy.End;
            toCopy.CopyTo(buffer.AsSpan(offset, count));

            return (int)toCopy.Length;
        }

        /// <inheritdoc/>
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<int>(cancellationToken);

            int bytesRead = Read(buffer, offset, count);
            if (bytesRead == 0)
            {
                return _taskOfZero;
            }

            Debug.Assert(_lastReadTask is null || _lastReadTask.IsCompleted);

            // Task is guaranteed to already be complete so calling `.Result` should be fine.
            if (_lastReadTask?.Result == bytesRead)
            {
                return _lastReadTask;
            }
            else
            {
                return _lastReadTask = Task.FromResult(bytesRead);
            }
        }

        /// <inheritdoc/>
        public override int ReadByte()
        {
            ReadOnlySequence<byte> remaining = _readOnlySequence.Slice(_position);
            if (remaining.Length > 0)
            {
                byte result = remaining.First.Span[0];
                _position = _readOnlySequence.GetPosition(1, _position);
                return result;
            }
            else
            {
                return -1;
            }
        }

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin)
        {
            ThrowIfDisposed();

            if (!_allowSeeking)
                throw new NotSupportedException();

            return SeekInternal(offset, origin);
        }

        private long SeekInternal(long offset, SeekOrigin origin)
        {
            SequencePosition relativeTo;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    relativeTo = _readOnlySequence.Start;
                    break;
                case SeekOrigin.Current:
                    if (offset >= 0)
                    {
                        relativeTo = _position;
                    }
                    else
                    {
                        relativeTo = _readOnlySequence.Start;
                        offset += Position;
                    }

                    break;
                case SeekOrigin.End:
                    if (offset >= 0)
                    {
                        relativeTo = _readOnlySequence.End;
                    }
                    else
                    {
                        relativeTo = _readOnlySequence.Start;
                        offset += Length;
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(origin));
            }

            _position = _readOnlySequence.GetPosition(offset, relativeTo);
            return Position;
        }

        /// <inheritdoc/>
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        /// <inheritdoc/>
        public override void WriteByte(byte value) => throw new NotSupportedException();

        /// <inheritdoc/>
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            Task.FromException(new NotSupportedException());

        /// <inheritdoc/>
        public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            foreach (ReadOnlyMemory<byte> segment in _readOnlySequence)
            {
                await destination.WriteAsync(segment, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public override int Read(Span<byte> buffer)
        {
            ReadOnlySequence<byte> remaining = _readOnlySequence.Slice(_position);
            ReadOnlySequence<byte> toCopy = remaining.Slice(0, Math.Min(buffer.Length, remaining.Length));

            _position = toCopy.End;
            toCopy.CopyTo(buffer);

            return (int)toCopy.Length;
        }

        /// <inheritdoc/>
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return ValueTask.FromCanceled<int>(cancellationToken);

            return new ValueTask<int>(Read(buffer.Span));
        }

        /// <inheritdoc/>
        public override void Write(ReadOnlySpan<byte> buffer) => throw new NotSupportedException();

        /// <inheritdoc/>
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) =>
            ValueTask.FromException(new NotSupportedException());

        public virtual void Reset(ReadOnlySequence<byte> newSequence = default, bool? allowSeeking = null)
        {
            _readOnlySequence = newSequence;
            _position = newSequence.Start;

            if (allowSeeking.HasValue)
                _allowSeeking = allowSeeking.Value;
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
                base.Dispose(disposing);
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);
        }
    }
}
