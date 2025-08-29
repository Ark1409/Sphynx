using Sphynx.Utils;

namespace Sphynx.Client.API;

public class SphynxServerConnection : IDisposable
{
    public LockableStream Stream { get; }

    private int _disposed = 0;

    public event Action<SphynxServerConnection>? OnDispose;

    public SphynxServerConnection(Stream stream)
    {
        Stream = new LockableStream(stream);
    }

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0) return;

        OnDispose?.Invoke(this);
        OnDispose = null!;

        Stream?.Dispose();
    }

    public class LockableStream : Stream
    {
        private readonly Stream _stream;
        private volatile int _operationsInFlight = 0;
        private volatile int _locksInFlight = 0;
        private const int _maxLocks = 1;
        private readonly object _monitor = new object();

        public LockableStream(Stream stream)
        {
            _stream = stream;
        }

        public LockedStream Rent(CancellationToken cancellationToken = default)
        {
            lock (_monitor)
            {
                cancellationToken.ThrowIfCancellationRequested();

                while (_operationsInFlight > 0 || _locksInFlight >= _maxLocks)
                {
                    Monitor.Wait(_monitor);
                    cancellationToken.ThrowIfCancellationRequested();
                }

                Interlocked.Increment(ref _locksInFlight);
            }

            return new(this);
        }

        public Task<LockedStream> RentAsync(CancellationToken cancellationToken = default)
        {
            return Task.Factory.StartNew((s) => ((LockableStream)s!).Rent(cancellationToken), this);
        }

        public class LockedStream : Stream
        {
            private readonly LockableStream _originalStream;
            private int _disposed = 0;

            public LockedStream(LockableStream l)
            {
                _originalStream = l;
            }

            public override bool CanRead => _originalStream._stream.CanRead;

            public override bool CanSeek => _originalStream._stream.CanSeek;

            public override bool CanWrite => _originalStream._stream.CanWrite;

            public override long Length => _originalStream._stream.Length;

            public override long Position
            {
                get => _originalStream._stream.Position;
                set { throwIfDisposed(); _originalStream._stream.Position = value; }
            }

            public override bool CanTimeout => _originalStream._stream.CanTimeout;

            public override int ReadTimeout
            {
                get => _originalStream._stream.ReadTimeout; set { throwIfDisposed(); _originalStream._stream.ReadTimeout = value; }
            }

            public override int WriteTimeout
            {
                get => _originalStream._stream.WriteTimeout; set { throwIfDisposed(); _originalStream._stream.WriteTimeout = value; }
            }


            public override void Flush()
            {
                _originalStream._stream.Flush();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throwIfDisposed();
                return _originalStream._stream.Read(buffer, offset, count);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throwIfDisposed();
                return _originalStream._stream.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                throwIfDisposed();
                _originalStream._stream.SetLength(value);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throwIfDisposed();
                _originalStream._stream.Write(buffer, offset, count);
            }

            public override bool Equals(object? other)
            {
                if (other is LockedStream lockStream) return lockStream == this;
                if (other is Stream s) return s.Equals(_originalStream._stream);

                return false;
            }

            public override string? ToString() => _originalStream._stream.ToString();

            public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
            {
                throwIfDisposed();
                return _originalStream._stream.BeginRead(buffer, offset, count, callback, state);
            }

            public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
            {
                throwIfDisposed();
                return _originalStream._stream.BeginWrite(buffer, offset, count, callback, state);
            }

            public override void Close()
            {
                throwIfDisposed();
                _originalStream._stream.Close();
            }

            public override void CopyTo(Stream destination, int bufferSize)
            {
                throwIfDisposed();
                _originalStream._stream.CopyTo(destination, bufferSize);
            }

            public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
            {
                throwIfDisposed();
                await _originalStream._stream.CopyToAsync(destination, bufferSize, cancellationToken);
            }

            public override int EndRead(IAsyncResult asyncResult)
            {
                return _originalStream._stream.EndRead(asyncResult);
            }

            public override void EndWrite(IAsyncResult asyncResult)
            {
                _originalStream._stream.EndWrite(asyncResult);
            }

            public override Task FlushAsync(CancellationToken cancellationToken)
            {
                return _originalStream._stream.FlushAsync(cancellationToken);
            }

            public override int Read(Span<byte> buffer)
            {
                throwIfDisposed();
                return _originalStream._stream.Read(buffer);
            }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                throwIfDisposed();
                return _originalStream._stream.ReadAsync(buffer, offset, count, cancellationToken);
            }

            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                throwIfDisposed();
                return _originalStream._stream.ReadAsync(buffer, cancellationToken);
            }

            public override int ReadByte()
            {
                throwIfDisposed();
                return _originalStream._stream.ReadByte();
            }

            public override void Write(ReadOnlySpan<byte> buffer)
            {
                throwIfDisposed();
                _originalStream._stream.Write(buffer);
            }

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                throwIfDisposed();
                return _originalStream._stream.WriteAsync(buffer, offset, count, cancellationToken);
            }

            public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            {
                throwIfDisposed();
                return _originalStream._stream.WriteAsync(buffer, cancellationToken);
            }

            public override void WriteByte(byte value)
            {
                throwIfDisposed();
                _originalStream._stream.WriteByte(value);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0) return;

                    lock (_originalStream._monitor)
                    {
                        if (Interlocked.Decrement(ref _originalStream._locksInFlight) == 0)
                            Monitor.PulseAll(_originalStream._monitor);
                    }
                }
            }

            private void throwIfDisposed()
            {
                ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
            }
        }

        public override bool CanRead => _stream.CanRead;

        public override bool CanSeek => _stream.CanSeek;

        public override bool CanWrite => _stream.CanWrite;

        public override long Length => _stream.Length;

        public override long Position
        {
            get => _stream.Position;
            set
            {
                using var _ = waitUntilAvailable();
                _stream.Position = value;
            }
        }

        public override bool CanTimeout => _stream.CanTimeout;

        public override int ReadTimeout
        {
            get => _stream.ReadTimeout;
            set
            {
                using var _ = waitUntilAvailable();
                _stream.ReadTimeout = value;
            }
        }

        public override int WriteTimeout
        {
            get => _stream.WriteTimeout;
            set
            {
                using var _ = waitUntilAvailable();
                _stream.WriteTimeout = value;
            }
        }


        public override void Flush()
        {
            _stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            using var _ = waitUntilAvailable();
            return _stream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            using var _ = waitUntilAvailable();
            return _stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            using var _ = waitUntilAvailable();
            _stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            using var _ = waitUntilAvailable();
            _stream.Write(buffer, offset, count);
        }

        public override bool Equals(object? other)
        {
            if (other is LockableStream lockStream) return lockStream == this;
            if (other is Stream s) return s.Equals(_stream);

            return false;
        }

        public override string? ToString() => _stream.ToString();

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            var releaser = waitUntilAvailable();
            return _stream.BeginRead(buffer, offset, count, res =>
            {
                try
                {
                    callback?.Invoke(res);
                }
                finally
                {
                    releaser.Dispose();
                }
            }, state);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            var releaser = waitUntilAvailable();
            return _stream.BeginWrite(buffer, offset, count, res =>
            {
                try
                {
                    callback?.Invoke(res);
                }
                finally
                {
                    releaser.Dispose();
                }
            }, state);
        }

        public override void Close()
        {
            using var _ = waitUntilAvailable();
            _stream.Close();
        }

        public override void CopyTo(Stream destination, int bufferSize)
        {
            using var _ = waitUntilAvailable();
            _stream.CopyTo(destination, bufferSize);
        }

        public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            using var _ = waitUntilAvailable(cancellationToken);
            await _stream.CopyToAsync(destination, bufferSize, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                using var _ = waitUntilAvailable();
                _stream.Dispose();
            }
        }

        public override async ValueTask DisposeAsync()
        {
            using var _ = waitUntilAvailable();
            await _stream.DisposeAsync();
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return _stream.EndRead(asyncResult);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            _stream.EndWrite(asyncResult);
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _stream.FlushAsync(cancellationToken);
        }

        public override int Read(Span<byte> buffer)
        {
            using var _ = waitUntilAvailable();
            return _stream.Read(buffer);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            using var _ = waitUntilAvailable(cancellationToken);
            return await _stream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            using var _ = waitUntilAvailable(cancellationToken);
            return await _stream.ReadAsync(buffer, cancellationToken);
        }

        public override int ReadByte()
        {
            using var _ = waitUntilAvailable();
            return _stream.ReadByte();
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            using var _ = waitUntilAvailable();
            _stream.Write(buffer);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            using var _ = waitUntilAvailable(cancellationToken);
            await _stream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            using var _ = waitUntilAvailable(cancellationToken);
            await _stream.WriteAsync(buffer, cancellationToken);
        }

        public override void WriteByte(byte value)
        {
            using var _ = waitUntilAvailable();
            _stream.WriteByte(value);
        }

        private ValueInvokeOnDisposal<LockableStream> waitUntilAvailable(CancellationToken cancellationToken = default)
        {
            lock (_monitor)
            {
                cancellationToken.ThrowIfCancellationRequested();

                while (_locksInFlight > 0)
                {
                    Monitor.Wait(_monitor);
                    cancellationToken.ThrowIfCancellationRequested();
                }

                Interlocked.Increment(ref _operationsInFlight);
            }

            return new(this, static s =>
            {
                lock (s._monitor)
                {
                    if (Interlocked.Decrement(ref s._operationsInFlight) == 0)
                        Monitor.PulseAll(s._monitor);
                }
            });
        }
    }
}
