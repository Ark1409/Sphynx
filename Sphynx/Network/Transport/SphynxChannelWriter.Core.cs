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
    public partial class SphynxChannelWriter
    {
        protected readonly Dictionary<ChannelId, Channel> OpenChannels = new();
        protected readonly object OpenChannelsLock = new();

        /// <summary>
        /// A lockable wrapper over the underlying stream to allow for serial output.
        /// </summary>
        protected StreamSynchronizer Stream { get; set; }

        public SphynxChannelWriter(Stream stream, bool ownsStream = false)
        {
            Stream = new StreamSynchronizer(stream);
            OwnsStream = ownsStream;
        }

        /// <summary>
        /// Sends a protocol-level <see cref="SphynxFrameType.CHANNEL_REJECT"/> frame across to the remote peer.
        /// </summary>
        /// <param name="rejectId">The channel ID to reject</param>
        public void SendReject(ChannelId rejectId)
            => SendFrame(new SphynxFrameHeader(SphynxFrameType.CHANNEL_REJECT, rejectId, 0), ReadOnlySequence<byte>.Empty);

        /// <summary>
        /// Sends a protocol-level <see cref="SphynxFrameType.CHANNEL_REJECT"/> frame across to the remote peer.
        /// </summary>
        /// <param name="rejectId">The channel ID to reject</param>
        /// <returns>A task representing the send operation.</returns>
        public ValueTask SendRejectAsync(ChannelId rejectId)
            => SendFrameAsync(new SphynxFrameHeader(SphynxFrameType.CHANNEL_REJECT, rejectId, 0), ReadOnlySequence<byte>.Empty);

        private void SendFrame(in SphynxFrameHeader header, in ReadOnlySequence<byte> data)
        {
            if (data.Length != header.FrameSize)
                ThrowInvalidFrameException();

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
            static void ThrowInvalidFrameException() => throw new SphynxProtocolException("Header size and data size do not match");
        }

        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
        private async ValueTask SendFrameAsync(SphynxFrameHeader header, ReadOnlySequence<byte> data, CancellationToken cancellationToken = default)
        {
            if (data.Length != header.FrameSize)
                ThrowInvalidFrameException();

            using (await Stream.RentLockAsync(cancellationToken).ConfigureAwait(false))
            {
                var stream = Stream.Stream;

                await SphynxFrameHeader.SendAsync(in header, stream, cancellationToken).ConfigureAwait(false);

                foreach (ReadOnlyMemory<byte> segment in data)
                    // We already indicated the frame length in the header, so cancellation isn't an option anymore.
                    await stream.WriteAsync(segment, CancellationToken.None).ConfigureAwait(false);
            }

            [DoesNotReturn]
            static void ThrowInvalidFrameException() => throw new SphynxProtocolException("Header size and data size do not match");
        }

        public partial class Channel
        {
            public const short DEFAULT_FRAME_SIZE = 4 * 1024; // 4KB

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
                    if(IsDisposed && _frameBufferRental == null)
                        ThrowIfDisposed();

                    _frameBufferRental ??= SequencePool.Shared.Rent();
                    return _frameBufferRental.Value.Value;
                }
            }

            private SequencePool.Rental? _frameBufferRental;

            public Channel(SphynxChannelWriter parent, ChannelId channelId)
            {
                Parent = parent;
                ChannelId = channelId;
            }

            public virtual void Write(ReadOnlySpan<byte> span)
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

            public virtual ValueTask WriteAsync(ReadOnlyMemory<byte> memory, CancellationToken cancellationToken = default)
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

            public void Flush()
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
                    header = isLastFlush ? header.WithFlags(ChannelDataFlags.CHANNEL_END) : header;

                    SendFrame(in header, bufferSeq.Slice(0, frameSize));
                    buffer.AdvanceTo(bufferSeq.GetPosition(frameSize));
                }

                Debug.Assert(buffer.Length == 0);
                buffer.Reset();
            }

            public virtual ValueTask FlushAsync(CancellationToken cancellationToken = default)
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
                    header = isLastFlush ? header.WithFlags(ChannelDataFlags.CHANNEL_END) : header;

                    await SendFrameAsync(header, bufferSeq.Slice(0, frameSize), CancellationToken.None).ConfigureAwait(false);
                    buffer.AdvanceTo(bufferSeq.GetPosition(frameSize));
                }

                Debug.Assert(buffer.Length == 0);
                buffer.Reset();
            }

            protected void SendFrame(in SphynxFrameHeader header, in ReadOnlySequence<byte> frameData)
            {
                Parent.SendFrame(in header, in frameData);
                BytesWritten += frameData.Length;
                FramesWritten++;
            }

            protected async ValueTask SendFrameAsync(SphynxFrameHeader header, ReadOnlySequence<byte> frameData,
                CancellationToken cancellationToken = default)
            {
                await Parent.SendFrameAsync(header, frameData, cancellationToken).ConfigureAwait(false);
                BytesWritten += frameData.Length;
                FramesWritten++;
            }

            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    try
                    {
                        Debug.Assert(CloseException != null);
                        SendClose(CloseException != _closeSentinel);
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

                    lock (Parent.OpenChannelsLock)
                    {
                        if (Parent.OpenChannels.Remove(ChannelId, out var channel))
                            Debug.Assert(channel == this);
                    }
                }
            }

            protected virtual async ValueTask DisposeAsyncCore()
            {
                try
                {
                    Debug.Assert(CloseException != null);
                    await SendCloseAsync(CloseException != _closeSentinel).ConfigureAwait(false);
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

                lock (Parent.OpenChannelsLock)
                {
                    if (Parent.OpenChannels.Remove(ChannelId, out var channel))
                        Debug.Assert(channel == this);
                }
            }

            private void SendClose(bool aborting = false)
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

            private ValueTask SendCloseAsync(bool aborting = false, CancellationToken cancellationToken = default)
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
