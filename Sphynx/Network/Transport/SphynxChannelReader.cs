// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft;
using Sphynx.Utils;
using ChannelOpenedHandler = System.Func<Sphynx.Network.Transport.SphynxChannelReader.Channel, System.Threading.Tasks.ValueTask>;

namespace Sphynx.Network.Transport
{
    public partial class SphynxChannelReader : IDisposable, IAsyncDisposable
    {
        protected bool OwnsStream;
        protected Stream Stream;

        protected bool IsDisposed { get => _disposed != 0; set => _disposed = value ? 1 : 0; }
        private volatile int _disposed;

        /// <summary>
        /// The maximum number of concurrently open reading channels.
        /// </summary>
        public int MaxOpenChannels { get; set; } = int.MaxValue;

        /// <summary>
        /// The current number of open channels.
        /// </summary>
        public int OpenChannelCount => OpenChannels.Count;

        /// <summary>
        /// Callback for when a new channel is opened.
        /// </summary>
        public ChannelOpenedHandler ChannelOpened
        {
            protected get => _channelOpened;
            set => _channelOpened = value;
        }

        private volatile ChannelOpenedHandler _channelOpened;

        /// <summary>
        /// A lock held while the reader is <see cref="RunAsync">running</see>.
        /// </summary>
        protected readonly SemaphoreSlim RunLock = new(1, 1);

        protected CancellationTokenSource RunCts = new();

        private Task? _runTask;

        private readonly AsyncLocal<bool> _isInsideRunTask = new();

        public SphynxChannelReader(Stream stream, ChannelOpenedHandler onChannelOpened, bool ownsStream = false)
        {
            Stream = stream;
            _channelOpened = onChannelOpened;
            OwnsStream = ownsStream;
        }

        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            // Prevent any accidental deadlocks
            if (_isInsideRunTask.Value)
                return;

            using (await RunLock.RentAsync(cancellationToken).ConfigureAwait(false))
            {
                if (_runTask?.Exception != null)
                    throw _runTask!.Exception.GetBaseException();

                if (_runTask?.IsCompleted ?? false)
                    return;

                ThrowIfDisposed();

                _isInsideRunTask.Value = true;

                try
                {
                    if (cancellationToken.CanBeCanceled)
                        RunCts = CancellationTokenSource.CreateLinkedTokenSource(RunCts.Token, cancellationToken);

                    await (_runTask = ReadChannelsAsync(RunCts.Token)).ConfigureAwait(false);
                }
                catch
                {
                    // ignore
                }

                _isInsideRunTask.Value = false;
            }

            await DisposeAsync().ConfigureAwait(false);
        }

        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 0)
                return;

            RunCts.Cancel();

            // Wait for run loop to end.
            // At that point, there should be no more channels being added to the OpenChannels list.
            if (!_isInsideRunTask.Value)
            {
                RunLock.Wait();
                RunLock.Release();
            }

            _disposeException = new ObjectDisposedException(GetType().Name, _runTask?.Exception);
            _runTask = null;

            GC.SuppressFinalize(this);
            Dispose(true);

            if (OwnsStream)
                Stream.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
                return;

            await RunCts.CancelAsync().ConfigureAwait(false);

            // Wait for run loop to end.
            // At that point, there should be no more channels being added to the OpenChannels list.
            if (!_isInsideRunTask.Value)
            {
                await RunLock.WaitAsync().ConfigureAwait(false);
                RunLock.Release();
            }

            _disposeException = new ObjectDisposedException(GetType().Name, _runTask?.Exception);
            _runTask = null;

            GC.SuppressFinalize(this);

            await DisposeAsyncCore().ConfigureAwait(false);

            if (OwnsStream)
                await Stream.DisposeAsync().ConfigureAwait(false);

            Dispose(false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ThrowIfDisposed()
        {
            if (IsDisposed)
                ThrowDisposedException();

            [DoesNotReturn]
            [StackTraceHidden]
            [MethodImpl(MethodImplOptions.NoInlining)]
            void ThrowDisposedException() => throw GetDisposedException();
        }

        private ObjectDisposedException? _disposeException;

        protected ObjectDisposedException GetDisposedException()
            => _disposeException ??= new ObjectDisposedException(GetType().Name, _runTask?.Exception?.GetBaseException());

        public partial class Channel : IAsyncDisposable, IDisposableObservable
        {
            private static readonly ChannelClosedException _closeSentinel = new();

            public ChannelId ChannelId { get; protected set; }

            public virtual Stream AsStream => _channelStream ??= new ChannelStream(this);
            private Stream? _channelStream;

            public bool IsDisposed => CloseException != null;
            protected volatile ChannelClosedException? CloseException;
            public virtual Action<Channel, Exception?>? OnDispose { protected get; set; }

            public virtual long BytesRead { get; protected set; }
            public virtual long FramesRead { get; protected set; }

            public SphynxChannelReader Reader { get; }

            public virtual void ReadExactly(Memory<byte> buffer)
            {
                int bytesRead = 0;

                while (bytesRead < buffer.Length)
                {
                    int rc = Read(buffer.Span[bytesRead..]);

                    if (rc <= 0)
                        ThrowEndException();

                    bytesRead += rc;
                }

                [DoesNotReturn]
                void ThrowEndException() => throw CloseException ?? new ChannelClosedException();
            }

            public virtual async ValueTask ReadExactlyAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                int bytesRead = 0;

                while (bytesRead < buffer.Length)
                {
                    int rc = await ReadAsync(buffer[bytesRead..], cancellationToken).ConfigureAwait(false);

                    if (rc <= 0)
                        ThrowEndException();

                    bytesRead += rc;
                }

                [DoesNotReturn]
                void ThrowEndException() => throw CloseException ?? new ChannelClosedException();
            }

            public void Dispose() => Dispose(null);

            public void Dispose(Exception? disposeException)
            {
                if (!TryReserveDispose(disposeException))
                    return;

                try
                {
                    OnDispose?.Invoke(this, CloseException?.InnerException);
                    OnDispose = null;
                }
                catch
                {
                    // ignore
                }

                _channelStream?.Dispose();

                GC.SuppressFinalize(this);
                Dispose(true);
            }

            public ValueTask DisposeAsync() => DisposeAsync(null);

            public async ValueTask DisposeAsync(Exception? disposeException)
            {
                if (!TryReserveDispose(disposeException))
                    return;

                try
                {
                    OnDispose?.Invoke(this, CloseException?.InnerException);
                    OnDispose = null;
                }
                catch
                {
                    // ignore
                }

                if (_channelStream is not null)
                    await _channelStream.DisposeAsync().ConfigureAwait(false);

                GC.SuppressFinalize(this);
                await DisposeAsyncCore().ConfigureAwait(false);
                Dispose(false);
            }

            private bool TryReserveDispose(Exception? disposeException)
            {
                if (CloseException is not null)
                    return false;

                ChannelClosedException closeException;

                if (disposeException is null)
                    closeException = _closeSentinel;
                else if (disposeException is ChannelClosedException closedException)
                    closeException = closedException;
                else
                    closeException = new ChannelClosedException(disposeException);

                return Interlocked.CompareExchange(ref CloseException, closeException, null) == null;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected void ThrowIfDisposed()
            {
                if (IsDisposed)
                    ThrowDisposedException();

                [DoesNotReturn]
                [StackTraceHidden]
                [MethodImpl(MethodImplOptions.NoInlining)]
                void ThrowDisposedException() => throw GetDisposedException();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected ObjectDisposedException GetDisposedException() => new(GetType().Name, CloseException);
        }

        protected class ChannelStream : Stream
        {
            public override bool CanRead => !_channel.IsDisposed;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override bool CanTimeout => false;
            public override long Length => throw new NotSupportedException();

            public override long Position
            {
                get => _channel.BytesRead;
                set => throw new NotSupportedException();
            }

            private readonly Channel _channel;

            public ChannelStream(Channel channel)
            {
                _channel = channel;
            }

            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                return _channel.ReadAsync(buffer, cancellationToken);
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return Read(new Span<byte>(buffer, offset, count));
            }

            public override int Read(Span<byte> buffer)
            {
                return _channel.Read(buffer);
            }

            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override void WriteByte(byte value) => throw new NotSupportedException();
            public override void Write(ReadOnlySpan<byte> span) => throw new NotSupportedException();

            public override void Flush() => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();

            // Dispose does nothing
        }
    }
}
