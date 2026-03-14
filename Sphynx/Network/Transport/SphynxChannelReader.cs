// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using FastEnumUtility;
using Microsoft;
using Sphynx.Network.Packet;
using Sphynx.Storage;
using Sphynx.Utils;
using Version = Sphynx.Core.Version;

namespace Sphynx.Network.Transport
{
    public class SphynxChannelReader : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// The currently supported protocol version.
        /// </summary>
        public static Version ProtocolVersion => V001Channel.ProtocolVersion;

        /// <summary>
        /// Callback for when a new channel is opened.
        /// </summary>
        public Action<Channel> ChannelOpened
        {
            protected get => _channelOpened;
            set => _channelOpened = value;
        }

        private volatile Action<Channel> _channelOpened;

        /// <summary>
        /// Callback for when a <see cref="SphynxFrameType.CHANNEL_DROP"/> is received.
        /// </summary>
        /// <seealso cref="SphynxFrameType.CHANNEL_DROP"/>
        public Action<long>? ChannelDropReceived
        {
            protected get => _channelDropReceived;
            set => _channelDropReceived = value;
        }

        private volatile Action<long>? _channelDropReceived;

        /// <summary>
        /// Callback for when incoming channel data is dropped.
        /// </summary>
        public Action<long, int>? ChannelDataDropped
        {
            protected get => _channelDataDropped;
            set => _channelDataDropped = value;
        }

        private volatile Action<long, int>? _channelDataDropped;

        protected readonly ConcurrentDictionary<long, Channel> OpenChannels = new();

        /// <summary>
        /// A lock held while the reader is <see cref="RunAsync">running</see>.
        /// </summary>
        protected readonly SemaphoreSlim RunLock = new(1, 1);

        private CancellationTokenSource _runCts = new();
        private volatile Task? _runTask;
        private readonly AsyncLocal<bool> _isInsideRunTask = new();

        protected readonly bool OwnsStream;
        protected readonly Stream Stream;
        private int _disposed;

        /// <summary>
        /// The exception that caused the run loop to terminate.
        /// </summary>
        protected Exception? RunException => _runException;

        private volatile Exception? _runException;

        public SphynxChannelReader(Stream stream, Action<Channel> onChannelOpened, bool ownsStream = false)
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
                var runTask = _runTask;

                if (runTask != null)
                    // Propagate exceptions to concurrent callers
                    await runTask.ConfigureAwait(false);

                ThrowIfDisposed();

                try
                {
                    if (cancellationToken.CanBeCanceled)
                        _runCts = CancellationTokenSource.CreateLinkedTokenSource(_runCts.Token, cancellationToken);

                    _isInsideRunTask.Value = true;
                    await (_runTask = ReadChannelsAsync(_runCts.Token)).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _runException = ex;
                }
                finally
                {
                    _isInsideRunTask.Value = false;
                }
            }

            await DisposeAsync().ConfigureAwait(false);
        }

        private bool _channelCreated;

        protected virtual async Task ReadChannelsAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                var frameHeader = await SphynxFrameHeader.ReceiveAsync(Stream, cancellationToken).ConfigureAwait(false);

                bool channelExists = OpenChannels.TryGetValue(frameHeader.ChannelId, out var openChannel);
                var channel = channelExists ? (V001Channel)openChannel! : null;

                Debug.Assert((!channelExists && channel == null) || (channelExists && channel != null));

                if (!V001Channel.IsValidFrame(frameHeader))
                {
                    if (channelExists)
                    {
                        var protocolException = new ProtocolViolationException(
                            $"Invalid frame received {frameHeader.FrameType} ({nameof(channel.ChannelId)}: {channel!.ChannelId})");

                        try
                        {
                            await channel.DisposeAsync(protocolException).ConfigureAwait(false);
                        }
                        catch
                        {
                            // ignore
                        }
                    }

                    await Stream.SkipAsync(frameHeader.FrameSize, cancellationToken).ConfigureAwait(false);
                    InvokeChannelDataDrop(frameHeader.ChannelId, frameHeader.FrameSize);
                    continue;
                }

                if (frameHeader.FrameType == SphynxFrameType.CHANNEL_DROP)
                {
                    InvokeChannelDrop(frameHeader.ChannelId);

                    await Stream.SkipAsync(frameHeader.FrameSize, cancellationToken).ConfigureAwait(false);
                    InvokeChannelDataDrop(frameHeader.ChannelId, frameHeader.FrameSize);
                    continue;
                }

                if (!channelExists && frameHeader.FrameType != SphynxFrameType.CHANNEL_START)
                {
                    await Stream.SkipAsync(frameHeader.FrameSize, cancellationToken).ConfigureAwait(false);
                    InvokeChannelDataDrop(frameHeader.ChannelId, frameHeader.FrameSize);
                    continue;
                }

                // If we've made it here, we at least have a chance at reading actual valid channel data.

                var sequenceRental = SequencePool.Shared.Rent();
                var sequence = sequenceRental.Value;

                try
                {
                    int readCount = 0;

                    while (readCount < frameHeader.FrameSize)
                    {
                        var memory = sequence.GetMemory(frameHeader.FrameSize - readCount);
                        int bytesFilled = await Stream.TryFillAsync(memory, cancellationToken).ConfigureAwait(false);

                        if (bytesFilled <= 0)
                        {
                            sequenceRental.Dispose();

                            if (channelExists)
                            {
                                var protocolException = new ProtocolViolationException(
                                    $"Invalid frame received {frameHeader.FrameType} ({nameof(channel.ChannelId)}: {channel!.ChannelId})");

                                try
                                {
                                    // This one can be a fire-and-forget, since there is no more data left to read in the stream
                                    _ = channel.DisposeAsync(protocolException);
                                }
                                catch
                                {
                                    // ignore
                                }
                            }

                            ThrowEndOfStreamException();
                        }

                        readCount += bytesFilled;
                        sequence.Advance(bytesFilled);
                    }

                    if (frameHeader.FrameType == SphynxFrameType.CHANNEL_START && !channelExists)
                    {
                        _channelCreated = false;
                        channel = (V001Channel?)OpenChannels.GetOrAdd(frameHeader.ChannelId, static (id, reader) =>
                        {
                            reader._channelCreated = true;
                            return reader.NewChannel(id)!;
                        }, this);

                        // We treat null channel as "we don't want to accept anymore channels"
                        if (channel == null)
                        {
                            sequenceRental.Dispose();

                            if (OpenChannels.TryRemove(new KeyValuePair<long, Channel>(frameHeader.ChannelId, null!)))
                                InvokeChannelDataDrop(frameHeader.ChannelId, frameHeader.FrameSize);

                            continue;
                        }

                        if (_channelCreated)
                            InvokeChannelOpened(channel);
                    }

                    await channel!.OnFrameReceivedAsync(frameHeader, new PooledChannelFrame(sequenceRental), cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                    if (sequenceRental.Value != null)
                        sequenceRental.Dispose();

                    throw;
                }
            }

            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.NoInlining)]
            static void ThrowEndOfStreamException() => throw new EndOfStreamException();
        }

        private readonly struct ChannelOpenedState
        {
            public SphynxChannelReader Reader { get; init; }
            public Channel Channel { get; init; }
        }

        private void InvokeChannelOpened(Channel channel)
        {
            var state = new ChannelOpenedState
            {
                Reader = this,
                Channel = channel,
            };

            ThreadPool.QueueUserWorkItem(static state =>
            {
                try
                {
                    state.Reader._channelOpened.Invoke(state.Channel);
                }
                catch
                {
                    // ignore
                }
            }, state, preferLocal: false);
        }

        private readonly struct ChannelDataDropState
        {
            public SphynxChannelReader Reader { get; init; }
            public long ChannelId { get; init; }
            public int FrameSize { get; init; }
        }

        private void InvokeChannelDataDrop(long channelId, int frameSize)
        {
            if (_channelDataDropped == null)
                return;

            var state = new ChannelDataDropState
            {
                Reader = this,
                ChannelId =  channelId,
                FrameSize = frameSize
            };

            ThreadPool.QueueUserWorkItem(static state =>
            {
                try
                {
                    state.Reader._channelDataDropped!.Invoke(state.ChannelId, state.FrameSize);
                }
                catch
                {
                    // ignore
                }
            }, state, preferLocal: false);
        }

        private readonly struct ChannelDropState
        {
            public SphynxChannelReader Reader { get; init; }
            public long ChannelId { get; init; }
        }

        private void InvokeChannelDrop(long channelId)
        {
            if (_channelDropReceived == null)
                return;

            var state = new ChannelDropState
            {
                Reader = this,
                ChannelId =  channelId,
            };

            ThreadPool.QueueUserWorkItem(static state =>
            {
                try
                {
                    state.Reader._channelDropReceived!.Invoke(state.ChannelId);
                }
                catch
                {
                    // ignore
                }
            }, state, preferLocal: false);
        }

        // Nullable return so that we can support bounding the number of open reading channels
        protected virtual V001Channel? NewChannel(long channelId) => new V001Channel(this, channelId);

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            if (disposing)
            {
                _runCts.Cancel();

                // Wait for run loop to end.
                // At that point, there should be no more channels being added to the OpenChannels list.
                if (!_isInsideRunTask.Value)
                {
                    RunLock.Wait();
                    RunLock.Release();
                }

                var disposeException = new ObjectDisposedException(GetType().Name, _runException);

                foreach (var (_, channel) in OpenChannels)
                {
                    try
                    {
                        channel.Dispose(disposeException);
                    }
                    catch
                    {
                        // ignore
                    }
                }

                if (OwnsStream)
                    Stream.Dispose();
            }
        }

        public virtual async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            await _runCts.CancelAsync().ConfigureAwait(false);

            // Wait for run loop to end.
            // At that point, there should be no more channels being added to the OpenChannels list.
            if (!_isInsideRunTask.Value)
            {
                await RunLock.WaitAsync().ConfigureAwait(false);
                RunLock.Release();
            }

            var disposeException = new ObjectDisposedException(GetType().Name, _runException);

            foreach (var (_, channel) in OpenChannels)
            {
                try
                {
                    await channel.DisposeAsync(disposeException).ConfigureAwait(false);
                }
                catch
                {
                    // ignore
                }
            }

            if (OwnsStream)
                await Stream.DisposeAsync().ConfigureAwait(false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ThrowIfDisposed()
        {
            if (_runCts.IsCancellationRequested)
                ThrowDisposedException();

            [DoesNotReturn]
            [StackTraceHidden]
            [MethodImpl(MethodImplOptions.NoInlining)]
            void ThrowDisposedException() => throw new ObjectDisposedException(GetType().Name);
        }

        #region Channels

        protected internal struct PooledChannelFrame : IDisposable
        {
            private SequencePool.Rental _bufferRental;
            private ReadOnlySequence<byte> _buffer;

            public ReadOnlySequence<byte> Frame
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => IsDisposed ? default : _buffer;
            }

            public ReadOnlySequence<byte> FrameLeft
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => IsDisposed ? default : _buffer.Slice(Position);
            }

            public long Position { get; private set; }

            private bool IsDisposed
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _bufferRental.Value == null;
            }

            public PooledChannelFrame(SequencePool.Rental bufferRental)
            {
                _bufferRental = bufferRental;
                _buffer = bufferRental.Value.AsReadOnlySequence;
            }

            public int Peek(Span<byte> span)
            {
                if (IsDisposed)
                    return 0;

                var currentBuffer = FrameLeft;
                int bytesRead = span.Length < currentBuffer.Length ? span.Length : (int)currentBuffer.Length;

                currentBuffer.Slice(0, bytesRead).CopyTo(span);
                return bytesRead;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Read(Span<byte> span)
            {
                int bytesRead = Peek(span);
                Position += bytesRead;
                return bytesRead;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                if (IsDisposed)
                    return;

                _bufferRental.Dispose();
                _bufferRental = default;
                _buffer = default;
                Position = 0;
            }
        }

        public abstract class Channel : IAsyncDisposable, IDisposableObservable
        {
            public virtual long ChannelId { get; protected set; }

            public virtual Stream AsStream => _channelStream ??= new ChannelStream(this);
            private Stream? _channelStream;

            public virtual bool IsDisposed { get; protected set; }
            protected Exception? DisposeException { get; set; }
            public Action<Channel, Exception?>? OnDispose { protected get; set; }

            public abstract long BytesRead { get; protected set; }
            public abstract long FramesRead { get; protected set; }

            public SphynxChannelReader Reader { get; }

            public Channel(SphynxChannelReader reader, long channelId)
            {
                Reader = reader;
                ChannelId = channelId;
            }

            public virtual void Fill(Memory<byte> buffer)
            {
                int bytesRead = 0;

                while (bytesRead < buffer.Length)
                {
                    int rc = Read(buffer.Span[bytesRead..]);

                    if(rc <= 0)
                        ThrowEndException();

                    bytesRead += rc;
                }
                [DoesNotReturn]
                static void ThrowEndException() => throw new EndOfStreamException();
            }

            public virtual async ValueTask FillAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                int bytesRead = 0;

                while (bytesRead < buffer.Length)
                {
                    int rc = await ReadAsync(buffer[bytesRead..], cancellationToken).ConfigureAwait(false);

                    if(rc <= 0)
                        ThrowEndException();

                    bytesRead += rc;
                }
                [DoesNotReturn]
                static void ThrowEndException() => throw new EndOfStreamException();
            }

            public abstract int Read(Span<byte> buffer);
            public abstract ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default);

            public void Dispose() => Dispose(null);

            public void Dispose(Exception? disposeException)
            {
                GC.SuppressFinalize(this);
                DisposeException = disposeException;
                Dispose(true);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (IsDisposed)
                    return;

                if (disposing)
                {
                    try
                    {
                        OnDispose?.Invoke(this, DisposeException);
                        OnDispose = null;
                    }
                    catch
                    {
                        // ignore
                    }

                    RemoveChannel();
                    _channelStream?.Dispose();
                }

                IsDisposed = true;
            }

            public virtual ValueTask DisposeAsync()
            {
                if (IsDisposed)
                    return ValueTask.CompletedTask;

                try
                {
                    OnDispose?.Invoke(this, DisposeException);
                    OnDispose = null;
                }
                catch
                {
                    // ignore
                }

                RemoveChannel();

                if (_channelStream is null)
                {
                    IsDisposed = true;
                    return ValueTask.CompletedTask;
                }

                return DisposeStream();

                async ValueTask DisposeStream()
                {
                    try
                    {
                        await _channelStream!.DisposeAsync().ConfigureAwait(false);
                    }
                    finally
                    {
                        IsDisposed = true;
                    }
                }
            }

            public ValueTask DisposeAsync(Exception? disposeException)
            {
                DisposeException = disposeException;
                return DisposeAsync();
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
            protected ObjectDisposedException GetDisposedException() => new(GetType().Name, DisposeException);

            private void RemoveChannel()
            {
                Reader.OpenChannels.TryRemove(new KeyValuePair<long, Channel>(ChannelId, this));
            }
        }

        protected class V001Channel : Channel
        {
            /// <summary>
            /// The current protocol implementation.
            /// </summary>
            public static Version ProtocolVersion => new(0, 0, 1);

            // TODO: Make configurable
            public const int MAX_FRAME_SIZE = 4096; // 4KB

            public override long BytesRead { get; protected set; }
            public override long FramesRead { get; protected set; }

            /// <summary>
            /// A <see cref="System.Threading.Channels.Channel{T}"/> holding incoming channel frames.
            /// </summary>
            protected Channel<PooledChannelFrame> FrameChannel { get; }

            protected PooledChannelFrame? CurrentFrame;
            private bool _channelStarted;

            public V001Channel(SphynxChannelReader reader, long channelId) : base(reader, channelId)
            {
                FrameChannel = CreateFrameChannel();
            }

            public static bool IsValidFrame(SphynxFrameHeader header)
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                return header.Version == ProtocolVersion
                       && FastEnum.IsDefined(header.FrameType)
                       && header.FrameSize >= 0
                       && header.FrameSize <= MAX_FRAME_SIZE;
            }

            public override int Read(Span<byte> buffer)
            {
                ThrowIfDisposed();

                if (CurrentFrame != null)
                {
                    var currentFrame = CurrentFrame.Value;
                    int bytesRead = currentFrame.Read(buffer);

                    if (currentFrame.Position == currentFrame.Frame.Length)
                    {
                        FramesRead++;
                        currentFrame.Dispose();
                        CurrentFrame = FrameChannel.Reader.TryRead(out var frame) ? frame : null;
                    }

                    BytesRead += bytesRead;
                    return bytesRead;
                }

                if (FrameChannel.Reader.TryRead(out var newFrame))
                {
                    CurrentFrame = newFrame;
                    return Read(buffer);
                }

                if (FrameChannel.Reader.Completion.IsCompleted)
                    return 0;

                return WaitAndRead(buffer);

                int WaitAndRead(Span<byte> memory)
                {
                    bool isCompleted = FrameChannel.Reader.WaitToReadAsync().Preserve().GetAwaiter().GetResult();

                    if (isCompleted)
                        return 0;

                    return Read(memory);
                }
            }

            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                if (IsDisposed)
                    return ValueTask.FromException<int>(GetDisposedException());

                if (CurrentFrame != null)
                {
                    var currentFrame = CurrentFrame.Value;
                    int bytesRead = currentFrame.Read(buffer.Span);

                    if (currentFrame.Position == currentFrame.Frame.Length)
                    {
                        FramesRead++;
                        currentFrame.Dispose();
                        CurrentFrame = FrameChannel.Reader.TryRead(out var frame) ? frame : null;
                    }

                    BytesRead += bytesRead;
                    return ValueTask.FromResult(bytesRead);
                }

                if (FrameChannel.Reader.TryRead(out var newFrame))
                {
                    CurrentFrame = newFrame;
                    return ReadAsync(buffer, cancellationToken);
                }

                if (FrameChannel.Reader.Completion.IsCompleted)
                    return ValueTask.FromResult(0);

                return WaitAndReadAsync(buffer, cancellationToken);

                [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
                async ValueTask<int> WaitAndReadAsync(Memory<byte> memory, CancellationToken token)
                {
                    bool isCompleted = await FrameChannel.Reader.WaitToReadAsync(token).ConfigureAwait(false);

                    if (isCompleted)
                        return 0;

                    return await ReadAsync(memory, token).ConfigureAwait(false);
                }
            }

            protected internal virtual ValueTask OnFrameReceivedAsync(SphynxFrameHeader header, PooledChannelFrame frame,
                CancellationToken cancellationToken)
            {
                // TODO: Assume this will only be called by a single thread?

                if (IsDisposed)
                    return ValueTask.FromException(GetDisposedException());

                if (cancellationToken.IsCancellationRequested)
                    return ValueTask.FromCanceled(cancellationToken);

                if (FrameChannel.Reader.Completion.IsCompleted)
                    return ValueTask.FromException(new ProtocolViolationException($"No more frames expected ({nameof(ChannelId)}: {ChannelId})"));

                if (!_channelStarted && header.FrameType != SphynxFrameType.CHANNEL_START)
                    return ValueTask.FromException(new ProtocolViolationException(
                        $"First frame of a channel should be {SphynxFrameType.CHANNEL_START} ({nameof(ChannelId)}: {ChannelId})"));

                switch (header.FrameType)
                {
                    case SphynxFrameType.CHANNEL_START:
                        var ex = OnStartReceived(frame);
                        return ex == null ? ValueTask.CompletedTask : ValueTask.FromException(ex);
                    case SphynxFrameType.CHANNEL_DATA:
                        return OnDataReceivedAsync(frame, cancellationToken);
                    case SphynxFrameType.CHANNEL_END:
                        OnEndReceived(frame);
                        return ValueTask.CompletedTask;
                    default:
                        return ValueTask.FromException(new ProtocolViolationException(
                            $"Invalid frame received {header.FrameType} ({nameof(ChannelId)}: {ChannelId})"));
                }
            }

            private Exception? OnStartReceived(PooledChannelFrame startFrame)
            {
                if (_channelStarted)
                    return new ProtocolViolationException(
                        $"Channel should only contain one {SphynxFrameType.CHANNEL_START} frame ({nameof(ChannelId)}: {ChannelId})");

                CurrentFrame = startFrame;
                _channelStarted = true;

                return null;
            }

            private ValueTask OnDataReceivedAsync(PooledChannelFrame dataFrame, CancellationToken cancellationToken)
            {
                return FrameChannel.Writer.WriteAsync(dataFrame, cancellationToken);
            }

            private void OnEndReceived(PooledChannelFrame endFrame)
            {
                byte endCode = endFrame.Frame.FirstSpan[0];
                bool channelAborted = endCode != 0;

                var abortException = channelAborted
                    ? null
                    : new ProtocolViolationException($"Channel aborted ({nameof(ChannelId)}: {ChannelId}, Code: {endCode})");

                if (channelAborted)
                    FrameChannel.Writer.Complete(abortException);

                Reader.OpenChannels.TryRemove(new KeyValuePair<long, Channel>(ChannelId, this));
            }

            protected virtual Channel<PooledChannelFrame> CreateFrameChannel()
            {
                return System.Threading.Channels.Channel.CreateBounded<PooledChannelFrame>(new BoundedChannelOptions(capacity: 16)
                {
                    // False since the reading thread will be the one calling Dispose on this channel,
                    // which completes the underlying Channel<>
                    SingleWriter = false,
                    SingleReader = true,
                    AllowSynchronousContinuations = false,
                    FullMode = BoundedChannelFullMode.Wait,
                });
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    FrameChannel.Writer.TryComplete(DisposeException ?? GetDisposedException());

                    while (FrameChannel.Reader.TryRead(out var frame))
                        frame.Dispose();

                    if (CurrentFrame != null)
                    {
                        CurrentFrame.Value.Dispose();
                        CurrentFrame = null;
                    }
                }

                _channelStarted = false;

                base.Dispose(disposing);
            }

            public override ValueTask DisposeAsync()
            {
                FrameChannel.Writer.TryComplete(DisposeException ?? GetDisposedException());

                while (FrameChannel.Reader.TryRead(out var frame))
                    frame.Dispose();

                if (CurrentFrame != null)
                {
                    CurrentFrame.Value.Dispose();
                    CurrentFrame = null;
                }

                _channelStarted = false;

                return base.DisposeAsync();
            }
        }

        /// <summary>
        /// A stream that reads from a specific channel.
        /// </summary>
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

        #endregion
    }
}
