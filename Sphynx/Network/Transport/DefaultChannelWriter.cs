// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Nerdbank.Streams;
using Sphynx.Storage;
using Sphynx.Utils;

namespace Sphynx.Network.Transport
{
    public class DefaultChannelWriter : SphynxChannelWriter
    {
        /// <summary>
        /// The current number of open writing channels (including unreleased channels).
        /// </summary>
        public int ChannelCount
        {
            get
            {
                lock (ChannelsLock)
                {
                    return Channels.Count;
                }
            }
        }

        /// <summary>
        /// The number of channels that have been closed but not yet <see cref="OnChannelReleased">released</see> by the remote peer.
        /// </summary>
        public int UnreleasedChannelCount
        {
            get
            {
                lock (ChannelsLock)
                {
                    return AutoReleaseChannels ? 0 : Channels.Count(x => x.Value.IsClosed);
                }
            }
        }

        /// <summary>
        /// The collection of open and/or unreleased channels. Null/ReleasedChannel indicates the channel has not yet been released by the
        /// remote peer.
        /// </summary>
        protected readonly Dictionary<ChannelId, ChannelEntry> Channels = new();

        protected object ChannelsLock => Channels;

        /// <summary>
        /// Whether <see cref="OnChannelReleased"/> should be called automatically when a channel is closed.
        /// </summary>
        public bool AutoReleaseChannels
        {
            get => AutoReleaseChannelsValue;
            init => AutoReleaseChannelsValue = value;
        }

        protected bool AutoReleaseChannelsValue;

        /// <summary>
        /// A lockable wrapper over the underlying stream to allow for serial output.
        /// </summary>
        protected StreamSynchronizer Stream;

        protected bool OwnsStream;

        public DefaultChannelWriter(Stream stream, bool ownsStream = false)
        {
            Stream = new StreamSynchronizer(stream);
            OwnsStream = ownsStream;
        }

        /// <summary>
        /// To be called by an appropriate <see cref="SphynxChannelReader"/> once the specified <paramref name="channelId"/>
        /// has been released by the remote peer.
        /// </summary>
        public bool OnChannelReleased(ChannelId channelId, byte releaseFlags)
        {
            lock (ChannelsLock)
            {
                if (AutoReleaseChannels)
                    return false;

                if (!Channels.Remove(channelId, out var entry))
                    return false;

                if (entry.IsClosed)
                    return true;

                DefaultChannel channel = (DefaultChannel)entry.Channel;

                if (channel.IsDisposed)
                    return true;

                bool isRejecting = (releaseFlags & ChannelReleaseFlags.CHANNEL_REJECTED) != 0;

                if (!isRejecting)
                    throw new SphynxProtocolException(channel.ChannelId,
                        "Writer received release confirmation when channel write-end was not yet closed");

                // There are no race conditions here (in the pooling case) because a channel instance with the same channel ID cannot be reused
                // until it is released. Since we acquired the ChannelsLock (which the channel instance also does before returning itself to
                // the pool), the channel cannot already be disposed by the time we make it here (since we checked for disposal above).
                return channel.ForceClose(_channelRejectedException);
            }
        }

        private static readonly ChannelClosedException _channelRejectedException = new("The channel was rejected by the remote peer");

        public override Channel OpenChannel(ChannelId channelId)
        {
            ThrowIfDisposed();

            lock (ChannelsLock)
            {
                ThrowIfDisposed();

                if (Channels.ContainsKey(channelId))
                    throw new ArgumentException($"Channel '{channelId}' already opened or unreleased");

                var channel = NewChannel(channelId);
                Channels[channelId] = channel;
                return channel;
            }
        }

        public override Channel OpenChannel()
        {
            ThrowIfDisposed();

            lock (ChannelsLock)
            {
                ThrowIfDisposed();

                var channel = NewChannel();
                Channels[channel.ChannelId] = channel;
                return channel;
            }
        }

        public override bool TryGetChannel(ChannelId channelId, bool createIfNotExists, [NotNullWhen(true)] out Channel? channel)
        {
            ThrowIfDisposed();

            lock (ChannelsLock)
            {
                ThrowIfDisposed();

                if (Channels.TryGetValue(channelId, out var entry))
                    // TODO: Review semantics
                    channel = entry.Channel;
                else if (createIfNotExists)
                    channel = OpenChannel(channelId);
                else
                    channel = null;

                return channel != null;
            }
        }

        private ChannelId _lastChannelId;

        protected DefaultChannel NewChannel()
        {
            ChannelId channelId;

            lock (ChannelsLock)
            {
                do
                {
                    _lastChannelId = (_lastChannelId + 1) & ChannelId.MaxValue;
                    channelId = _lastChannelId;
                } while (Channels.ContainsKey(channelId));
            }

            return NewChannel(channelId);
        }

        protected virtual DefaultChannel NewChannel(ChannelId channelId) => new(this, channelId);

        /// <summary>
        /// Sends a protocol-level <see cref="SphynxFrameType.CHANNEL_RELEASE"/> frame across to the remote peer.
        /// </summary>
        /// <param name="releaseId">The channel ID to reject</param>
        public void SendRelease(ChannelId releaseId, byte flags = ChannelReleaseFlags.NONE)
            => SendFrame(new SphynxFrameHeader(SphynxFrameType.CHANNEL_RELEASE, flags, releaseId, 0), ReadOnlySequence<byte>.Empty);

        /// <summary>
        /// Sends a protocol-level <see cref="SphynxFrameType.CHANNEL_RELEASE"/> frame across to the remote peer.
        /// </summary>
        /// <param name="releaseId">The channel ID to reject</param>
        /// <returns>A task representing the send operation.</returns>
        public ValueTask SendReleaseAsync(ChannelId releaseId, byte flags = ChannelReleaseFlags.NONE)
            => SendFrameAsync(new SphynxFrameHeader(SphynxFrameType.CHANNEL_RELEASE, flags, releaseId, 0), ReadOnlySequence<byte>.Empty);

        protected void SendFrame(in SphynxFrameHeader header, in ReadOnlySequence<byte> data)
        {
            if (data.Length != header.FrameSize)
                ThrowInvalidFrameException(in header);

            Span<byte> frameHeaderBytes = stackalloc byte[SphynxFrameHeader.SIZE];
            header.Serialize(frameHeaderBytes);

            using (Stream.RentLock())
            {
                var stream = Stream.Stream;

                stream.Write(frameHeaderBytes);

                foreach (ReadOnlyMemory<byte> segment in data)
                    stream.Write(segment.Span);
            }

            [DoesNotReturn]
            static void ThrowInvalidFrameException(in SphynxFrameHeader header) =>
                throw new SphynxProtocolException(header.ChannelId, "Header size and data size do not match");
        }

        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
        protected async ValueTask SendFrameAsync(SphynxFrameHeader header, ReadOnlySequence<byte> data, CancellationToken cancellationToken = default)
        {
            if (data.Length != header.FrameSize)
                ThrowInvalidFrameException(in header);

            using (await Stream.RentLockAsync(cancellationToken).ConfigureAwait(false))
            {
                var stream = Stream.Stream;

                await SphynxFrameHeader.SendAsync(in header, stream, cancellationToken).ConfigureAwait(false);

                foreach (ReadOnlyMemory<byte> segment in data)
                    // We already indicated the frame length in the header, so cancellation isn't an option anymore.
                    await stream.WriteAsync(segment, CancellationToken.None).ConfigureAwait(false);
            }

            [DoesNotReturn]
            static void ThrowInvalidFrameException(in SphynxFrameHeader header) =>
                throw new SphynxProtocolException(header.ChannelId, "Header size and data size do not match");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (ChannelsLock)
                {
                    // Allow pending operations to finish.
                }

                var disposeException = GetDisposeException();

                foreach (var (_, entry) in Channels)
                {
                    try
                    {
                        if (!entry.IsClosed)
                            entry.Channel.Dispose(disposeException);
                    }
                    catch
                    {
                        // ignore
                    }
                }

                Channels.Clear();

                if (OwnsStream)
                {
                    try
                    {
                        Stream.Dispose();
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }
        }

        protected override async ValueTask DisposeAsyncCore()
        {
            lock (ChannelsLock)
            {
                // Allow pending operations to finish.
            }

            var disposeException = GetDisposeException();

            foreach (var (_, entry) in Channels)
            {
                try
                {
                    if (!entry.IsClosed)
                        await entry.Channel.DisposeAsync(disposeException).ConfigureAwait(false);
                }
                catch
                {
                    // ignore
                }
            }

            Channels.Clear();

            if (OwnsStream)
            {
                try
                {
                    await Stream.DisposeAsync().ConfigureAwait(false);
                }
                catch
                {
                    // ignore
                }
            }
        }

        protected struct ChannelEntry
        {
            public Channel? Channel { get; internal set; }
            public ChannelId ChannelId { get; }

            [MemberNotNullWhen(false, nameof(Channel))]
            public bool IsClosed => Channel == null;

            public ChannelEntry(DefaultChannel channel)
            {
                Channel = channel;
                ChannelId = channel.ChannelId;
            }

            public static implicit operator ChannelEntry(DefaultChannel channel) => new(channel);
        }

        protected internal class DefaultChannel : Channel
        {
            protected const short DEFAULT_FRAME_SIZE = 1024; // 1KB

            public short MaxFrameSize
            {
                get => _maxFrameSize;
                set => _maxFrameSize = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
            }

            private short _maxFrameSize = DEFAULT_FRAME_SIZE;

            protected virtual Sequence<byte> FrameBuffer
            {
                get
                {
                    if (_frameBufferRental == null)
                        ThrowIfDisposed();

                    _frameBufferRental ??= SequencePool.Shared.Rent();
                    return _frameBufferRental.Value.Value;
                }
            }

            private SequencePool.Rental? _frameBufferRental;

            protected override DefaultChannelWriter Parent { get; }

            public DefaultChannel(DefaultChannelWriter parent, ChannelId channelId) : base(parent, channelId)
            {
                Parent = parent;
            }

            public override void Write(ReadOnlySpan<byte> span)
            {
                var buffer = FrameBuffer;
                int bytesWritten = 0;

                while (bytesWritten < span.Length)
                {
                    ThrowIfDisposed();

                    long bufferLength = buffer.Length;

                    if (bufferLength >= MaxFrameSize)
                    {
                        Flush();
                        Debug.Assert(buffer.Length == 0);
                        bufferLength = buffer.Length;
                    }

                    Debug.Assert(bufferLength < MaxFrameSize);

                    int frameLeft = (int)(MaxFrameSize - bufferLength);
                    long spanLeft = span.Length - bytesWritten;
                    int writeLength = (int)Math.Min(frameLeft, spanLeft);

                    buffer.Write(span.Slice(bytesWritten, writeLength));
                    bytesWritten += writeLength;
                }

                Debug.Assert(bytesWritten == span.Length);
                Debug.Assert(buffer.Length <= MaxFrameSize);
            }

            public override ValueTask WriteAsync(ReadOnlyMemory<byte> memory, CancellationToken cancellationToken = default)
            {
                if (IsDisposed)
                    return ValueTask.FromException(GetDisposedException());

                if (cancellationToken.IsCancellationRequested)
                    return ValueTask.FromCanceled(cancellationToken);

                // Fast path if we know the write won't cause a flush. This should cover most cases actually.
                if (memory.Length + FrameBuffer.Length <= MaxFrameSize)
                {
                    try
                    {
                        Write(memory);
                        return ValueTask.CompletedTask;
                    }
                    catch (Exception ex)
                    {
                        return ValueTask.FromException(ex);
                    }
                }

                return WriteAsyncCore(memory, cancellationToken);
            }

            private async ValueTask WriteAsyncCore(ReadOnlyMemory<byte> memory, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var buffer = FrameBuffer;
                int bytesWritten = 0;

                while (bytesWritten < memory.Length)
                {
                    ThrowIfDisposed();

                    long bufferLength = buffer.Length;

                    if (bufferLength >= MaxFrameSize)
                    {
                        await FlushAsync(CancellationToken.None).ConfigureAwait(false);
                        Debug.Assert(buffer.Length == 0);
                        bufferLength = buffer.Length;
                    }

                    Debug.Assert(bufferLength < MaxFrameSize);

                    int frameLeft = (int)(MaxFrameSize - bufferLength);
                    long memoryLeft = memory.Length - bytesWritten;
                    int writeLength = (int)Math.Min(frameLeft, memoryLeft);

                    buffer.Write(memory.Slice(bytesWritten, writeLength).Span);
                    bytesWritten += writeLength;
                }

                Debug.Assert(bytesWritten == memory.Length);
                Debug.Assert(buffer.Length <= MaxFrameSize);
            }

            public sealed override void Flush()
            {
                ThrowIfDisposed();
                FlushCore();
            }

            protected virtual void FlushCore(bool closing = false)
            {
                if (FrameBuffer.Length == 0 && !closing)
                    return;

                var buffer = FrameBuffer;
                long bufferLength = buffer.Length;

                // Write the data in chunks of MaxFrameSize
                for (int frame = 0; frame * MaxFrameSize < bufferLength; frame++)
                {
                    var bufferSeq = buffer.AsReadOnlySequence;
                    short frameSize = (short)Math.Min(buffer.Length, MaxFrameSize);

                    var header = new SphynxFrameHeader
                    {
                        FrameType = SphynxFrameType.CHANNEL_DATA,
                        Flags = FramesWritten == 0 ? ChannelDataFlags.CHANNEL_START : ChannelDataFlags.NONE,
                        ChannelId = ChannelId,
                        FrameSize = frameSize,
                    };

                    bool isLastFlush = closing && (frame + 1) * MaxFrameSize >= bufferLength;

                    if (isLastFlush)
                        header = header.WithFlags(ChannelDataFlags.CHANNEL_END);

                    SendFrame(in header, bufferSeq.Slice(0, frameSize));
                    buffer.AdvanceTo(bufferSeq.GetPosition(frameSize));
                }

                Debug.Assert(buffer.Length == 0);
                buffer.Reset();
            }

            public sealed override ValueTask FlushAsync(CancellationToken cancellationToken = default)
            {
                if (IsDisposed)
                    return ValueTask.FromException(GetDisposedException());

                if (cancellationToken.IsCancellationRequested)
                    return ValueTask.FromCanceled(cancellationToken);

                if (FrameBuffer.Length == 0)
                    return ValueTask.CompletedTask;

                return FlushAsyncCore(cancellationToken: cancellationToken);
            }

            [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
            protected virtual async ValueTask FlushAsyncCore(bool closing = false, CancellationToken cancellationToken = default)
            {
                if (FrameBuffer.Length == 0 && !closing)
                    return;

                cancellationToken.ThrowIfCancellationRequested();

                var buffer = FrameBuffer;
                long bufferLength = buffer.Length;

                // Write the data in chunks of MaxFrameSize
                for (long frame = 0; frame * MaxFrameSize < bufferLength; frame++)
                {
                    var bufferSeq = buffer.AsReadOnlySequence;
                    short frameSize = (short)Math.Min(buffer.Length, MaxFrameSize);

                    var header = new SphynxFrameHeader
                    {
                        FrameType = SphynxFrameType.CHANNEL_DATA,
                        Flags = FramesWritten == 0 ? ChannelDataFlags.CHANNEL_START : ChannelDataFlags.NONE,
                        ChannelId = ChannelId,
                        FrameSize = frameSize,
                    };

                    bool isLastFlush = closing && (frame + 1) * MaxFrameSize >= bufferLength;

                    if (isLastFlush)
                        header = header.WithFlags(ChannelDataFlags.CHANNEL_END);

                    await SendFrameAsync(header, bufferSeq.Slice(0, frameSize), CancellationToken.None).ConfigureAwait(false);
                    buffer.AdvanceTo(bufferSeq.GetPosition(frameSize));
                }

                Debug.Assert(buffer.Length == 0);
                buffer.Reset();
            }

            protected void SendFrame(in SphynxFrameHeader header, in ReadOnlySequence<byte> frameData)
            {
                Parent.SendFrame(in header, in frameData);
                BytesWritten += SphynxFrameHeader.SIZE + frameData.Length;
                FramesWritten++;
            }

            [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
            protected async ValueTask SendFrameAsync(SphynxFrameHeader header, ReadOnlySequence<byte> frameData,
                CancellationToken cancellationToken = default)
            {
                await Parent.SendFrameAsync(header, frameData, cancellationToken).ConfigureAwait(false);
                BytesWritten += SphynxFrameHeader.SIZE + frameData.Length;
                FramesWritten++;
            }

            public override IBufferWriter<byte> AsBufferWriter() => FrameBuffer;

            protected internal virtual bool ForceClose(Exception? closeException)
            {
                return Interlocked.CompareExchange(ref CloseException, ToCloseException(closeException), null) == null;
            }

            protected int IsDisposeReserved;

            protected override bool TryReserveDispose(Exception? disposeException)
            {
                Interlocked.CompareExchange(ref CloseException, ToCloseException(disposeException), null);
                return Interlocked.Exchange(ref IsDisposeReserved, 1) == 0;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    try
                    {
                        Debug.Assert(CloseException != null);
                        SendClose(CloseException != CloseSentinel);
                    }
                    catch
                    {
                        // This should only fail if the underlying stream is closed, at which
                        // point we don't really care about signaling our closure
                    }

                    if (_frameBufferRental != null)
                    {
                        _frameBufferRental.Value.Dispose();
                        _frameBufferRental = null;
                    }

                    lock (Parent.ChannelsLock)
                    {
                        bool channelClosed = Parent.AutoReleaseChannels
                            ? Parent.Channels.Remove(ChannelId, out var entry)
                            : Parent.Channels.TryGetValue(ChannelId, out entry);

                        if (channelClosed)
                        {
                            Debug.Assert(entry.Channel == this);
                            entry.Channel = null;
                        }
                    }
                }
            }

            protected override async ValueTask DisposeAsyncCore()
            {
                try
                {
                    Debug.Assert(CloseException != null);
                    await SendCloseAsync(CloseException != CloseSentinel).ConfigureAwait(false);
                }
                catch
                {
                    // This should only fail if the underlying stream is closed, at which
                    // point we don't really care about signaling our closure
                }

                if (_frameBufferRental != null)
                {
                    _frameBufferRental.Value.Dispose();
                    _frameBufferRental = null;
                }

                lock (Parent.ChannelsLock)
                {
                    bool channelClosed = Parent.AutoReleaseChannels
                        ? Parent.Channels.Remove(ChannelId, out var entry)
                        : Parent.Channels.TryGetValue(ChannelId, out entry);

                    if (channelClosed)
                    {
                        Debug.Assert(entry.Channel == this);
                        entry.Channel = null;
                    }
                }
            }

            protected void SendClose(bool aborting = false)
            {
                if (aborting)
                {
                    if (FramesWritten == 0)
                        return;

                    var abortHeader = new SphynxFrameHeader
                    {
                        FrameType = SphynxFrameType.CHANNEL_ABORT,
                        ChannelId = ChannelId,
                        FrameSize = 0
                    };

                    SendFrame(in abortHeader, ReadOnlySequence<byte>.Empty);
                    return;
                }

                FlushCore(closing: true);
            }

            protected ValueTask SendCloseAsync(bool aborting = false, CancellationToken cancellationToken = default)
            {
                if (aborting)
                {
                    if (FramesWritten == 0)
                        return ValueTask.CompletedTask;

                    var abortHeader = new SphynxFrameHeader
                    {
                        FrameType = SphynxFrameType.CHANNEL_ABORT,
                        ChannelId = ChannelId,
                        FrameSize = 0
                    };

                    return SendFrameAsync(abortHeader, ReadOnlySequence<byte>.Empty, cancellationToken);
                }

                return FlushAsyncCore(closing: true, cancellationToken);
            }
        }
    }
}
