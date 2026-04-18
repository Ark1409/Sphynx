// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using Microsoft;
using Sphynx.Utils;
using ChannelOpenedHandler = System.Func<object?, Sphynx.Network.Transport.SphynxChannelReader.Channel, System.Threading.Tasks.ValueTask>;

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
        /// <remarks>
        /// If this is set to a value at which we are currently above, we will simply not accept any more
        /// channels until the <see cref="OpenChannelCount"/> drops below it.
        /// </remarks>
        public int MaxOpenChannels { get; set; } = int.MaxValue;

        /// <summary>
        /// The current number of open channels.
        /// </summary>
        public int OpenChannelCount => OpenChannels.Count;

        private object? _onChannelOpenedState;
        private volatile ChannelOpenedHandler? _onChannelOpened;

        /// <summary>
        /// A lock held while the reader is <see cref="RunAsync">running</see>.
        /// </summary>
        protected readonly SemaphoreSlim RunLock = new(1, 1);

        protected CancellationTokenSource RunCts = new();

        private Task? _runTask;
        private readonly AsyncLocal<bool> _isInsideRunTask = new();

        public SphynxChannelReader(Stream stream, bool ownsStream = false)
        {
            Stream = stream;
            OwnsStream = ownsStream;
        }

        /// <summary>
        /// Callback for when a new channel is opened.
        /// </summary>
        public void OnChannelOpened(ChannelOpenedHandler callback, object? state = null)
        {
            _onChannelOpenedState = state;
            _onChannelOpened = callback;
        }

        /// <summary>
        /// Actively begins reading from the underlying stream.
        /// </summary>
        /// <exception cref="Exception">The exception which terminated the reading.</exception>
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

            await CloseChannelsAsync().ConfigureAwait(false);
        }

        private async ValueTask CloseChannelsAsync()
        {
            Debug.Assert(!_isInsideRunTask.Value);

            foreach (var (_, channel) in OpenChannels)
            {
                var closeException = GetDisposedException();

                try
                {
                    await channel.DisposeAsync(closeException).ConfigureAwait(false);
                }
                catch
                {
                    // ignore
                }
            }
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

            _disposeException = new ObjectDisposedException(GetType().Name, _runTask?.Exception?.GetBaseException());
            _runTask = null;
            _onChannelOpened = null;
            _onChannelOpenedState = null;

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

            _disposeException = new ObjectDisposedException(GetType().Name, _runTask?.Exception?.GetBaseException());
            _runTask = null;
            _onChannelOpened = null;
            _onChannelOpenedState = null;

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

        protected ObjectDisposedException GetDisposedException() =>
            _disposeException ??= new ObjectDisposedException(GetType().Name, _runTask?.Exception?.GetBaseException());

        public partial class Channel : IAsyncDisposable, IDisposableObservable
        {
            private static readonly ChannelClosedException _closeSentinel = new();
            private volatile bool _writerCompleted;

            public ChannelId ChannelId { get; protected set; }

            public bool IsDisposed => CloseException != null;
            protected volatile ChannelClosedException? CloseException;
            public virtual Action<Channel, Exception?>? OnDispose { protected get; set; }

            public virtual long BytesRead { get; protected set; }
            public virtual long FramesRead { get; protected set; }

            public SphynxChannelReader Parent { get; }

            public virtual void ReadExactly(Memory<byte> buffer)
            {
                var task = ReadExactlyAsync(buffer);

                if (!task.IsCompleted)
                    task = task.Preserve();

                task.GetAwaiter().GetResult();
            }

            public virtual async ValueTask ReadExactlyAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                var result = await ChannelReader.ReadAtLeastAsync(buffer.Length, cancellationToken).ConfigureAwait(false);

                if (result.IsCanceled || result.IsCompleted)
                    ThrowEndException();

                result.Buffer.CopyTo(buffer.Span);
                ChannelReader.AdvanceTo(result.Buffer.End);

                void ThrowEndException() => throw CloseException ?? _closeSentinel;
            }

            private ChannelStream? _channelStream;

            public virtual Stream AsStream(bool leaveOpen = true)
            {
                _channelStream ??= new ChannelStream(this);
                _channelStream.LeaveOpen = leaveOpen;
                return _channelStream;
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

                GC.SuppressFinalize(this);
                await DisposeAsyncCore().ConfigureAwait(false);
                Dispose(false);
                _writerCompleted = false;
            }

            [MemberNotNull(nameof(CloseException))]
            private bool TryReserveDispose(Exception? disposeException)
            {
                if (CloseException is not null)
                    return false;

                ChannelClosedException closeException;

                if (disposeException is null || _writerCompleted)
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
            public bool LeaveOpen { get; set; }

            public ChannelStream(Channel channel, bool leaveOpen = true)
            {
                _channel = channel;
                LeaveOpen = leaveOpen;
            }

            public sealed override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
                TaskToAsyncResult.Begin(ReadAsync(buffer, offset, count, default), callback, state);

            public sealed override int EndRead(IAsyncResult asyncResult) =>
                TaskToAsyncResult.End<int>(asyncResult);

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

            public override ValueTask DisposeAsync()
            {
                return LeaveOpen ? base.DisposeAsync() : _channel.DisposeAsync();
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing && LeaveOpen)
                    _channel.Dispose();
            }
        }

        protected class ChannelPipeReader : PipeReader
        {
            private readonly Channel _channel;
            private readonly PipeReader _reader;
            public bool LeaveOpen { get; set; }

            public ChannelPipeReader(Channel channel, PipeReader? reader, bool leaveOpen = true)
            {
                _channel = channel;
                _reader = reader ?? CreatePipeReader(channel, leaveOpen);
                LeaveOpen = leaveOpen;
            }

            protected virtual PipeReader CreatePipeReader(Channel channel, bool leaveOpen)
            {
                var defaultOptions = Channel.DefaultPipeOptions;
                var options = new StreamPipeReaderOptions(defaultOptions.Pool,
                    defaultOptions.MinimumSegmentSize,
                    minimumReadSize: 128,
                    leaveOpen: true,
                    useZeroByteReads: false);

                return Create(channel.AsStream(), options);
            }

            public override bool TryRead(out ReadResult result) => _reader.TryRead(out result);

            public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default) => _reader.ReadAsync(cancellationToken);

            protected override ValueTask<ReadResult> ReadAtLeastAsyncCore(int minimumSize, CancellationToken cancellationToken) =>
                _reader.ReadAtLeastAsync(minimumSize, cancellationToken);

            public override void AdvanceTo(SequencePosition consumed) => _reader.AdvanceTo(consumed);

            public override void AdvanceTo(SequencePosition consumed, SequencePosition examined) => _reader.AdvanceTo(consumed, examined);

            public override void CancelPendingRead() => _reader.CancelPendingRead();

            public override Task CopyToAsync(PipeWriter destination, CancellationToken cancellationToken = default) =>
                _reader.CopyToAsync(destination, cancellationToken);

            public override Task CopyToAsync(Stream destination, CancellationToken cancellationToken = default) =>
                _reader.CopyToAsync(destination, cancellationToken);

            [Obsolete($"{nameof(OnWriterCompleted)} has been deprecated and may not be invoked on all implementations of PipeReader.")]
            public override void OnWriterCompleted(Action<Exception?, object?> callback, object? state) => _reader.OnWriterCompleted(callback, state);

            public override ValueTask CompleteAsync(Exception? exception = null)
            {
                if (LeaveOpen)
                    return ValueTask.CompletedTask;

                return _channel.DisposeAsync(exception);
            }

            public override Stream AsStream(bool leaveOpen = false)
            {
                return _channel.AsStream(leaveOpen);
            }

            public override void Complete(Exception? exception = null)
            {
                if (LeaveOpen)
                    return;

                _channel.Dispose(exception);
            }
        }
    }
}
