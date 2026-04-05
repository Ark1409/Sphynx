// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Sphynx.Storage;
using Sphynx.Utils;

namespace Sphynx.Network.Transport
{
    //
    // This file holds the actual frame parsing implementation for the channel reader.
    //

    public partial class SphynxChannelReader
    {
        private object? _onChannelRejectReceivedState;
        private volatile Action<object?, ChannelId>? _onChannelRejectReceived;

        private object? _onFrameDroppedState;
        private volatile Action<object?, SphynxFrameHeader>? _onFrameDropped;

        private object? _onChannelRejectedState;
        private volatile Action<object?, ChannelId>? _onChannelRejected;

        /// <summary>
        /// Holds a mapping of all currently active reading channels.
        /// </summary>
        protected readonly ConcurrentDictionary<ChannelId, Channel> OpenChannels = new();

        /// <summary>
        /// Registers a callback that is called when an open channel is <see cref="Channel.Dispose()">disposed</see> before the remote peer finishes
        /// sending all its data.
        /// </summary>
        /// <remarks>This is when a <see cref="SphynxFrameType.CHANNEL_REJECT"/> should be sent to the remote peer.</remarks>
        public void OnChannelRejected(Action<object?, ChannelId> callback, object? state = null)
        {
            _onChannelRejectedState = state;
            _onChannelRejected = callback;
        }

        /// <summary>
        /// Register a callback for when an incoming channel frame is dropped. This can occur if <see cref="MaxOpenChannels">too many</see>
        /// channels are open, or invalid data is received. Under normal circumstances, existing channels should never lose any data.
        /// </summary>
        public void OnFrameDropped(Action<object?, SphynxFrameHeader> callback, object? state = null)
        {
            _onFrameDroppedState = state;
            _onFrameDropped = callback;
        }

        /// <summary>
        /// Callback for when a <see cref="SphynxFrameType.CHANNEL_REJECT"/> is received.
        /// </summary>
        /// <seealso cref="SphynxFrameType.CHANNEL_REJECT"/>
        public void OnChannelRejectReceived(Action<object?, ChannelId> callback, object? state = null)
        {
            _onChannelRejectReceivedState = state;
            _onChannelRejectReceived = callback;
        }

        protected virtual async Task ReadChannelsAsync(CancellationToken cancellationToken)
        {
            // NOTE: We don't want to split this into multiple async methods, as that would cause unnecessary allocations.

            while (true)
            {
                var frameHeader = await SphynxFrameHeader.ReceiveAsync(Stream, allowInvalid: true, cancellationToken).ConfigureAwait(false);
                bool channelExists = OpenChannels.TryGetValue(frameHeader.ChannelId, out var channel);

                if (!frameHeader.IsValid())
                {
                    // Don't make any assumptions about the header's contents if the versions aren't the same
                    bool sameVersion = frameHeader.Version == SphynxFrameHeader.ProtocolVersion;

                    if (sameVersion && channelExists && frameHeader.FrameType == SphynxFrameType.CHANNEL_DATA)
                    {
                        var protocolException = new SphynxProtocolException(
                            $"Invalid frame received {frameHeader.FrameType} ({nameof(channel.ChannelId)}: {channel!.ChannelId})");
                        DisposeChannel(channel, protocolException);
                    }

                    InvokeFrameDropped(in frameHeader);

                    // We'll still take the size hint, even if the frame itself is invalid
                    if (sameVersion && frameHeader.FrameSize > 0)
                        await Stream.SkipAsync(frameHeader.FrameSize, cancellationToken).ConfigureAwait(false);

                    continue;
                }

                Debug.Assert(Enum.IsDefined(frameHeader.FrameType));

                if (frameHeader.FrameType == SphynxFrameType.CHANNEL_REJECT)
                {
                    InvokeChannelRejectReceived(frameHeader.ChannelId);

                    Debug.Assert(frameHeader.FrameSize == 0);
                    continue;
                }

                if (frameHeader.FrameType == SphynxFrameType.CHANNEL_ABORT)
                {
                    if (channelExists)
                    {
                        var abortException = new ChannelClosedException("The channel was aborted by the remote peer");
                        DisposeChannel(channel!, abortException);
                    }

                    Debug.Assert(frameHeader.FrameSize == 0);
                    continue;
                }

                Debug.Assert(frameHeader.FrameType == SphynxFrameType.CHANNEL_DATA);

                if (frameHeader.HasFlags(ChannelDataFlags.CHANNEL_START))
                {
                    if (channelExists)
                    {
                        var protocolException = new SphynxProtocolException("A duplicate channel with the same ID was opened");
                        DisposeChannel(channel!, protocolException);
                    }

                    if (_onChannelOpened == null)
                    {
                        InvokeChannelRejected(frameHeader.ChannelId);
                        InvokeFrameDropped(in frameHeader);

                        await Stream.SkipAsync(frameHeader.FrameSize, cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    // If we're going to have to store this channel
                    if (!frameHeader.HasFlags(ChannelDataFlags.CHANNEL_END))
                    {
                        // Just drop it if we're at our max
                        if (OpenChannelCount >= MaxOpenChannels)
                        {
                            InvokeChannelRejected(frameHeader.ChannelId);
                            InvokeFrameDropped(in frameHeader);

                            await Stream.SkipAsync(frameHeader.FrameSize, cancellationToken).ConfigureAwait(false);
                            continue;
                        }

                        channel = OpenChannels.GetOrAdd(frameHeader.ChannelId, static (id, reader) => reader.NewChannel(id), this);
                    }
                    else
                    {
                        channel = NewChannel(frameHeader.ChannelId);
                    }

                    channelExists = true;

                    if (!InvokeChannelOpened(channel))
                    {
                        DisposeChannel(channel, null);
                        InvokeFrameDropped(in frameHeader);

                        await Stream.SkipAsync(frameHeader.FrameSize, cancellationToken).ConfigureAwait(false);
                        continue;
                    }
                }

                if (!channelExists)
                {
                    InvokeFrameDropped(in frameHeader);

                    await Stream.SkipAsync(frameHeader.FrameSize, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                Debug.Assert(channelExists && channel != null);

                // Now read the actual channel data

                var sequenceRental = SequencePool.Shared.Rent();
                var sequence = sequenceRental.Value;

                try
                {
                    // Split the frame into 4KB segments
                    const int SEGMENT_SIZE = 4096;
                    for (int segment = 0; segment * SEGMENT_SIZE < frameHeader.FrameSize; segment++)
                    {
                        int bytesLeft = frameHeader.FrameSize - segment * SEGMENT_SIZE;
                        int readSize = Math.Min(SEGMENT_SIZE, bytesLeft);
                        var memory = sequence.GetMemory(readSize)[..readSize];

                        await Stream.ReadExactlyAsync(memory, cancellationToken).ConfigureAwait(false);
                        sequence.Advance(readSize);
                    }
                }
                catch
                {
                    sequenceRental.Dispose();
                    throw;
                }

                var dataFrame = new PooledDataFrame(sequenceRental, frameHeader.Flags);

                try
                {
                    await channel.ReceiveFrameAsync(dataFrame, cancellationToken).ConfigureAwait(false);

                    if (frameHeader.HasFlags(ChannelDataFlags.CHANNEL_END))
                        OpenChannels.TryRemove(new KeyValuePair<ChannelId, Channel>(channel.ChannelId, channel));
                }
                catch
                {
                    dataFrame.Dispose();
                    InvokeFrameDropped(in frameHeader);
                }
            }
        }

        private void DisposeChannel(Channel channel, Exception? disposeException)
        {
            OpenChannels.TryRemove(new KeyValuePair<ChannelId, Channel>(channel.ChannelId, channel));

            if (channel.IsDisposed)
                return;

            ThreadPool.QueueUserWorkItem(static async void (state) =>
            {
                try
                {
                    await state.channel.DisposeAsync(state.disposeException).ConfigureAwait(false);
                }
                catch
                {
                    // ignore
                }
            }, (channel, disposeException), preferLocal: false);
        }

        private bool InvokeChannelOpened(Channel channel)
        {
            object? stateArg = _onChannelOpenedState;
            var callback = _onChannelOpened;

            if (callback == null)
                return false;

            ThreadPool.QueueUserWorkItem(static async void (state) =>
            {
                try
                {
                    await state.callback.Invoke(state.stateArg, state.channel).ConfigureAwait(false);
                }
                catch
                {
                    // ignore
                }
            }, (callback, stateArg, channel), preferLocal: false);
            return true;
        }

        private void InvokeFrameDropped(in SphynxFrameHeader header)
        {
            if (_onFrameDropped == null)
                return;

            ThreadPool.QueueUserWorkItem(static state =>
            {
                try
                {
                    state.reader._onFrameDropped?.Invoke(state.reader._onFrameDroppedState, state.header);
                }
                catch
                {
                    // ignore
                }
            }, (reader: this, header), preferLocal: false);
        }

        private void InvokeChannelRejectReceived(ChannelId channelId)
        {
            if (_onChannelRejectReceived == null)
                return;

            ThreadPool.QueueUserWorkItem(static state =>
            {
                try
                {
                    state.reader._onChannelRejectReceived?.Invoke(state.reader._onChannelRejectReceivedState, state.channelId);
                }
                catch
                {
                    // ignore
                }
            }, (reader: this, channelId), preferLocal: false);
        }

        private void InvokeChannelRejected(ChannelId channelId)
        {
            if (_onChannelRejected == null)
                return;

            ThreadPool.QueueUserWorkItem(static state =>
            {
                try
                {
                    state.reader._onChannelRejected?.Invoke(state.reader._onChannelRejectedState, state.channelId);
                }
                catch
                {
                    // ignore
                }
            }, (reader: this, channelId), preferLocal: false);
        }

        protected virtual Channel NewChannel(ChannelId channelId) => new Channel(this, channelId);

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                UnregisterCallbacks();

                foreach (var (_, channel) in OpenChannels)
                {
                    try
                    {
                        channel.Dispose(_disposeException);
                    }
                    catch
                    {
                        // ignore
                    }
                }

                OpenChannels.Clear();
            }
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            UnregisterCallbacks();

            foreach (var (_, channel) in OpenChannels)
            {
                try
                {
                    await channel.DisposeAsync(_disposeException).ConfigureAwait(false);
                }
                catch
                {
                    // ignore
                }
            }

            OpenChannels.Clear();
        }

        private void UnregisterCallbacks()
        {
            _onChannelRejected = null;
            _onChannelRejectedState = null;
            _onFrameDropped = null;
            _onFrameDroppedState = null;
            _onChannelRejectReceived = null;
            _onChannelRejectReceivedState = null;
        }

        public struct PooledDataFrame : IDisposable
        {
            private SequencePool.Rental _bufferRental;
            private ReadOnlySequence<byte> _buffer;

            public byte DataFlags { get; }

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

            public PooledDataFrame(SequencePool.Rental bufferRental, byte dataFlags)
            {
                _bufferRental = bufferRental;
                _buffer = bufferRental.Value.AsReadOnlySequence;
                DataFlags = dataFlags;
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
            public bool HasFlags(byte flags) => (DataFlags & flags) == flags;

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

        public partial class Channel
        {
            /// <summary>
            /// A <see cref="System.Threading.Channels.Channel{T}"/> holding incoming channel frames.
            /// </summary>
            protected Channel<PooledDataFrame> FrameChannel { get; }

            protected PooledDataFrame? CurrentFrame;
            private bool _channelStarted;
            private bool _channelEnded;

            public Channel(SphynxChannelReader parent, ChannelId channelId)
            {
                Parent = parent;
                ChannelId = channelId;
                FrameChannel = CreateFrameChannel();
            }

            public int Read(Span<byte> buffer)
            {
                ThrowIfDisposed();

                int bytesRead = 0;

                while (true)
                {
                    if (CurrentFrame != null)
                    {
                        var currentFrame = CurrentFrame.Value;
                        int readCount = currentFrame.Read(buffer);

                        if (currentFrame.Position == currentFrame.Frame.Length)
                        {
                            FramesRead++;
                            currentFrame.Dispose();
                            CurrentFrame = FrameChannel.Reader.TryRead(out var frame) ? frame : null;
                        }

                        BytesRead += readCount;
                        bytesRead += readCount;

                        if (readCount != buffer.Length)
                        {
                            buffer = buffer[readCount..];
                            continue;
                        }

                        return bytesRead;
                    }

                    if (FrameChannel.Reader.TryRead(out var newFrame))
                    {
                        CurrentFrame = newFrame;
                        continue;
                    }

                    try
                    {
                        bool isCompleted = FrameChannel.Reader.WaitToReadAsync().Preserve().GetAwaiter().GetResult();

                        if (isCompleted)
                            return bytesRead;
                    }
                    catch
                    {
                        return bytesRead;
                    }
                }
            }

            public ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                if (IsDisposed)
                    return ValueTask.FromException<int>(GetDisposedException());

                int bytesRead = 0;

                while (true)
                {
                    if (CurrentFrame != null)
                    {
                        var currentFrame = CurrentFrame.Value;
                        int readCount = currentFrame.Read(buffer.Span);

                        if (currentFrame.Position == currentFrame.Frame.Length)
                        {
                            FramesRead++;
                            currentFrame.Dispose();
                            CurrentFrame = FrameChannel.Reader.TryRead(out var frame) ? frame : null;
                        }

                        BytesRead += readCount;
                        bytesRead += readCount;

                        if (readCount != buffer.Length)
                        {
                            buffer = buffer[readCount..];
                            continue;
                        }

                        return ValueTask.FromResult(bytesRead);
                    }

                    if (FrameChannel.Reader.TryRead(out var newFrame))
                    {
                        CurrentFrame = newFrame;
                        continue;
                    }

                    // Should be rare
                    return WaitAndReadAsync(buffer, bytesRead, cancellationToken);
                }

                [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
                async ValueTask<int> WaitAndReadAsync(Memory<byte> memory, int readCount, CancellationToken token)
                {
                    try
                    {
                        bool isCompleted = await FrameChannel.Reader.WaitToReadAsync(token).ConfigureAwait(false);

                        if (isCompleted)
                            return readCount;
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        return readCount;
                    }

                    return readCount + await ReadAsync(memory, token).ConfigureAwait(false);
                }
            }

            protected internal virtual ValueTask ReceiveFrameAsync(PooledDataFrame dataFrame, CancellationToken cancellationToken = default)
            {
                if (cancellationToken.IsCancellationRequested)
                    return ValueTask.FromCanceled(cancellationToken);

                if (IsDisposed)
                    return ValueTask.FromException(GetDisposedException());

                if (_channelEnded)
                    return ValueTask.FromException(new SphynxProtocolException($"No more frames expected ({nameof(ChannelId)}: {ChannelId})"));

                if (!_channelStarted && !dataFrame.HasFlags(ChannelDataFlags.CHANNEL_START))
                    return ValueTask.FromException(new SphynxProtocolException(
                        $"First frame of a channel should be CHANNEL_START ({nameof(ChannelId)}: {ChannelId})"));

                if (dataFrame.HasFlags(ChannelDataFlags.CHANNEL_START))
                {
                    if (_channelStarted)
                        return ValueTask.FromException(new SphynxProtocolException(
                            $"Channel should only contain one CHANNEL_START frame ({nameof(ChannelId)}: {ChannelId})"));

                    _channelStarted = true;
                }

                if (!dataFrame.HasFlags(ChannelDataFlags.CHANNEL_END))
                    return FrameChannel.Writer.WriteAsync(dataFrame, cancellationToken);

                return WriteFrameAndComplete(dataFrame, cancellationToken);

                [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
                async ValueTask WriteFrameAndComplete(PooledDataFrame frame, CancellationToken ct)
                {
                    Debug.Assert(frame.HasFlags(ChannelDataFlags.CHANNEL_END));
                    _channelEnded = true;

                    await FrameChannel.Writer.WriteAsync(frame, ct).ConfigureAwait(false);
                    FrameChannel.Writer.TryComplete();
                }
            }

            // OnReaderFinished
            protected internal virtual ValueTask OnReaderEndAsync()
            {
                if (FrameChannel.Writer.TryComplete(Parent.GetDisposedException()))
                    return DisposeAsync(Parent.GetDisposedException());

                return ValueTask.CompletedTask;
            }

            protected virtual Channel<PooledDataFrame> CreateFrameChannel()
            {
                return System.Threading.Channels.Channel.CreateBounded<PooledDataFrame>(new BoundedChannelOptions(capacity: 8)
                {
                    // False since the protocol reading thread can also call Dispose on this channel
                    SingleWriter = false,
                    SingleReader = true,
                    AllowSynchronousContinuations = false,
                    FullMode = BoundedChannelFullMode.Wait,
                });
            }

            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    Debug.Assert(CloseException != null);

                    if (FrameChannel.Writer.TryComplete(CloseException))
                        Parent.InvokeChannelRejected(ChannelId);

                    Parent.OpenChannels.TryRemove(new KeyValuePair<ChannelId, Channel>(ChannelId, this));
                    FrameChannel.Reader.DrainAsync(frame => frame.Dispose()).Preserve().GetAwaiter().GetResult();

                    if (CurrentFrame != null)
                    {
                        CurrentFrame.Value.Dispose();
                        CurrentFrame = null;
                    }
                }

                _channelEnded = false;
                _channelStarted = false;
            }

            protected virtual async ValueTask DisposeAsyncCore()
            {
                Debug.Assert(CloseException != null);

                if (FrameChannel.Writer.TryComplete(CloseException))
                    Parent.InvokeChannelRejected(ChannelId);

                Parent.OpenChannels.TryRemove(new KeyValuePair<ChannelId, Channel>(ChannelId, this));
                await FrameChannel.Reader.DrainAsync(frame => frame.Dispose()).ConfigureAwait(false);

                if (CurrentFrame != null)
                {
                    CurrentFrame.Value.Dispose();
                    CurrentFrame = null;
                }

                _channelEnded = false;
                _channelStarted = false;
            }
        }
    }
}
