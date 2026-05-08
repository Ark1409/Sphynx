// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using Microsoft;
using Sphynx.Utils;

namespace Sphynx.Network.Transport
{
    public abstract class SphynxChannelReader : IDisposable, IAsyncDisposable
    {
        protected bool IsDisposed
        {
            get => _disposed != 0;
            set
            {
                _disposeException = value ? GetDisposedException() : null;
                _disposed = value ? 1 : 0;
            }
        }

        private volatile int _disposed;
        private ObjectDisposedException? _disposeException;

        protected readonly SemaphoreSlim RunLock = new(1, 1);
        protected CancellationTokenSource RunCts = new();
        private readonly AsyncLocal<bool> _isInsideRunTask = new();

        /// <summary>
        /// The <see cref="RunAsync">run task</see> for this reader.
        /// </summary>
        public Task? RunTask { get; protected set; }

        /// <summary>
        /// The maximum number of concurrently open reading channels.
        /// </summary>
        public virtual int MaxOpenChannels { get; set; } = int.MaxValue;

        /// <summary>
        /// Callback for when a new channel is opened.
        /// </summary>
        public abstract void OnChannelOpened(ChannelOpenedHandler callback, object? state = null);

        /// <summary>
        /// Actively begins reading from the underlying stream.
        /// </summary>
        public void Start(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();

            _ = RunAsync(cancellationToken);
        }

        /// <summary>
        /// Actively begins reading from the underlying stream. Blocks until the reader finishes.
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
                ThrowIfDisposed();

                if (RunTask?.IsCompletedSuccessfully ?? false)
                    return;

                if (RunTask?.Exception != null)
                    throw RunTask!.Exception.InnerException!;

                _isInsideRunTask.Value = true;

                try
                {
                    if (cancellationToken.CanBeCanceled && !RunCts.IsCancellationRequested)
                        RunCts = CancellationTokenSource.CreateLinkedTokenSource(RunCts.Token, cancellationToken);

                    await (RunTask = ReadChannelsAsync(RunCts.Token)).ConfigureAwait(false);
                }
                catch
                {
                    // ignore, caught by the RunTask
                }

                _isInsideRunTask.Value = false;
            }
        }

        protected abstract Task ReadChannelsAsync(CancellationToken cancellationToken);

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            RunCts.Cancel();
            RunCts.Dispose();

            // Wait for the run loop to end
            if (!_isInsideRunTask.Value)
            {
                RunLock.Wait();
                RunLock.Release();
            }

            _disposeException = new ObjectDisposedException(GetType().Name, RunTask?.Exception?.InnerException);

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            await RunCts.CancelAsync().ConfigureAwait(false);
            RunCts.Dispose();

            // Wait for the run loop to end
            if (!_isInsideRunTask.Value)
            {
                await RunLock.WaitAsync().ConfigureAwait(false);
                RunLock.Release();
            }

            _disposeException = new ObjectDisposedException(GetType().Name, RunTask?.Exception?.InnerException);

            await DisposeAsyncCore().ConfigureAwait(false);
            Dispose(false);
            GC.SuppressFinalize(this);
        }

        protected virtual ValueTask DisposeAsyncCore()
        {
            return ValueTask.CompletedTask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ThrowIfDisposed()
        {
            if (IsDisposed)
                ThrowDisposedException();

            [DoesNotReturn]
            [StackTraceHidden]
            void ThrowDisposedException() => throw GetDisposedException();
        }

        protected ObjectDisposedException GetDisposedException() =>
            _disposeException ??= new ObjectDisposedException(GetType().Name, RunTask?.Exception?.InnerException);

        public abstract class Channel : IAsyncDisposable, IDisposableObservable
        {
            protected static readonly ChannelClosedException CloseSentinel = new();

            public ChannelId ChannelId { get; protected set; }

            public bool IsDisposed => CloseException != null;
            protected volatile ChannelClosedException? CloseException;
            public virtual Action<Channel, Exception?>? OnDispose { protected get; set; }

            public virtual long BytesRead { get; protected set; }

            protected virtual SphynxChannelReader Parent { get; }

            public Channel(SphynxChannelReader parent, ChannelId channelId)
            {
                Parent = parent;
                ChannelId = channelId;
            }

            public abstract int Read(Span<byte> buffer);
            public abstract ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default);

            public virtual int ReadAtLeast(Memory<byte> buffer, int minSize = -1, bool throwOnEnd = false)
            {
                if (minSize == -1)
                    minSize = buffer.Length;

                int bytesRead = 0;

                while (bytesRead < minSize)
                {
                    int readCount = Read(buffer[bytesRead..].Span);

                    if (readCount <= 0)
                    {
                        if (throwOnEnd)
                            ThrowEndException();

                        break;
                    }

                    bytesRead += readCount;
                }

                return bytesRead;

                [DoesNotReturn]
                void ThrowEndException() => throw CloseException ?? CloseSentinel;
            }

            public virtual async ValueTask<int> ReadAtLeastAsync(Memory<byte> buffer, int minSize = -1, bool throwOnEnd = false,
                CancellationToken cancellationToken = default)
            {
                if (minSize == -1)
                    minSize = buffer.Length;

                int bytesRead = 0;

                while (bytesRead < minSize)
                {
                    int readCount = await ReadAsync(buffer[bytesRead..], cancellationToken).ConfigureAwait(false);

                    if (readCount <= 0)
                    {
                        if (throwOnEnd)
                            ThrowEndException();

                        break;
                    }

                    bytesRead += readCount;
                }

                return bytesRead;

                [DoesNotReturn]
                void ThrowEndException() => throw CloseException ?? CloseSentinel;
            }

            private ChannelStream? _channelStream;

            public virtual Stream AsStream(bool leaveOpen = true)
            {
                _channelStream ??= new ChannelStream(this, leaveOpen);
                _channelStream.LeaveOpen = leaveOpen;
                return _channelStream;
            }

            private ChannelPipeReader? _channelPipeReader;

            public virtual PipeReader AsPipeReader(bool leaveOpen = true)
            {
                _channelPipeReader ??= new ChannelPipeReader(this, null, leaveOpen);
                _channelPipeReader.LeaveOpen = leaveOpen;
                return _channelPipeReader;
            }

            public void Dispose() => Dispose(null);

            public void Dispose(Exception? disposeException)
            {
                if (!TryReserveDispose(disposeException))
                    return;

                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    try
                    {
                        OnDispose?.Invoke(this, CloseException?.InnerException);
                        OnDispose = null;
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }

            public ValueTask DisposeAsync() => DisposeAsync(null);

            public async ValueTask DisposeAsync(Exception? disposeException)
            {
                if (!TryReserveDispose(disposeException))
                    return;

                await DisposeAsyncCore().ConfigureAwait(false);
                Dispose(false);
                GC.SuppressFinalize(this);
            }

            protected virtual ValueTask DisposeAsyncCore()
            {
                try
                {
                    OnDispose?.Invoke(this, CloseException?.InnerException);
                    OnDispose = null;
                }
                catch
                {
                    // ignore
                }

                return ValueTask.CompletedTask;
            }

            [MemberNotNullWhen(true, nameof(CloseException))]
            protected virtual bool TryReserveDispose(Exception? disposeException)
            {
                if (IsDisposed)
                    return false;

                CloseException = ToCloseException(disposeException);
                return true;
            }

            private protected static ChannelClosedException ToCloseException(Exception? ex) => ex switch
            {
                null => CloseSentinel,
                ChannelClosedException closed => closed,
                _ => new ChannelClosedException(ex)
            };

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected void ThrowIfDisposed()
            {
                if (IsDisposed)
                    ThrowDisposedException();

                [DoesNotReturn]
                [StackTraceHidden]
                void ThrowDisposedException() => throw GetDisposedException();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected ObjectDisposedException GetDisposedException() => new(GetType().Name, CloseException);
        }

        protected class ChannelStream : Stream
        {
            public override bool CanRead => !Channel.IsDisposed;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override bool CanTimeout => false;
            public override long Length => throw new NotSupportedException();

            public override long Position
            {
                get => Channel.BytesRead;
                set => throw new NotSupportedException();
            }

            public Channel Channel { get; }
            public bool LeaveOpen { get; set; }

            public ChannelStream(Channel channel, bool leaveOpen = true)
            {
                Channel = channel;
                LeaveOpen = leaveOpen;
            }

            public sealed override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
                TaskToAsyncResult.Begin(ReadAsync(buffer, offset, count, default), callback, state);

            public sealed override int EndRead(IAsyncResult asyncResult) =>
                TaskToAsyncResult.End<int>(asyncResult);

            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                return Channel.ReadAsync(buffer, cancellationToken);
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return Read(new Span<byte>(buffer, offset, count));
            }

            public override int Read(Span<byte> buffer)
            {
                return Channel.Read(buffer);
            }

            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override void WriteByte(byte value) => throw new NotSupportedException();
            public override void Write(ReadOnlySpan<byte> span) => throw new NotSupportedException();

            public override void Flush() => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();

            public override ValueTask DisposeAsync()
            {
                return LeaveOpen || Channel.IsDisposed ? base.DisposeAsync() : Channel.DisposeAsync();
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing && !LeaveOpen && !Channel.IsDisposed)
                    Channel.Dispose();
            }
        }

        protected class ChannelPipeReader : PipeReader
        {
            public static readonly PipeOptions DefaultPipeOptions = new(
                pauseWriterThreshold: 4096,
                resumeWriterThreshold: 2048,
                minimumSegmentSize: 1024,
                useSynchronizationContext: false);

            protected static readonly StreamPipeReaderOptions DefaultStreamPipeOptions = new(DefaultPipeOptions.Pool,
                DefaultPipeOptions.MinimumSegmentSize,
                minimumReadSize: 1,
                leaveOpen: true,
                useZeroByteReads: false);

            public Channel Channel { get; }
            public bool LeaveOpen { get; set; }

            private readonly PipeReader _reader;

            public ChannelPipeReader(Channel channel, PipeReader? internalReader = null, bool leaveOpen = true)
            {
                Channel = channel;
                LeaveOpen = leaveOpen;
                _reader = internalReader ?? CreatePipeReader(channel, leaveOpen);
            }

            protected virtual PipeReader CreatePipeReader(Channel channel, bool leaveOpen) => Create(channel.AsStream(), DefaultStreamPipeOptions);

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
                return LeaveOpen || Channel.IsDisposed ? ValueTask.CompletedTask : Channel.DisposeAsync(exception);
            }

            public override Stream AsStream(bool leaveOpen = false)
            {
                return Channel.AsStream(leaveOpen);
            }

            public override void Complete(Exception? exception = null)
            {
                if (!LeaveOpen && !Channel.IsDisposed)
                    Channel.Dispose(exception);
            }
        }
    }
}
