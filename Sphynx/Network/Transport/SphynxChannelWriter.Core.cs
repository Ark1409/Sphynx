// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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

        public partial class Channel
        {
            public short MaxFrameSize { get; protected set; } = 4 * 1024; // 4KB

            protected ReadOnlySequence<byte> FrameBuffer => FrameBufferRental.Value?.AsReadOnlySequence ?? default;
            protected SequencePool.Rental FrameBufferRental;

            public Channel(SphynxChannelWriter writer, ChannelId channelId)
            {
                Writer = writer;
                ChannelId = channelId;
                FrameBufferRental = SequencePool.Shared.Rent();
            }

            public virtual void Write(ReadOnlySpan<byte> span)
            {
                var buffer = FrameBufferRental.Value;
                int bytesWritten = 0;

                while (bytesWritten < span.Length)
                {
                    ThrowIfDisposed();

                    long bufferLength = buffer.Length;
                    int frameLeft = (int)(MaxFrameSize - bufferLength);
                    long payloadLeft = span.Length - bytesWritten;

                    int writeLength = (int)Math.Min(frameLeft, payloadLeft);

                    // Only force a flush if we are actually going to write something
                    if (writeLength > 0 && bufferLength >= MaxFrameSize)
                    {
                        Flush();
                        Debug.Assert(buffer.Length == 0);
                    }

                    buffer.Write(span.Slice(bytesWritten, writeLength));
                    bytesWritten += writeLength;
                }

                Debug.Assert(bytesWritten == span.Length);
                Debug.Assert(buffer.Length <= MaxFrameSize);

                BytesWritten += bytesWritten;
            }

            public virtual ValueTask WriteAsync(scoped ReadOnlySpan<byte> span, CancellationToken cancellationToken = default)
            {
                if (cancellationToken.IsCancellationRequested)
                    return ValueTask.FromCanceled(cancellationToken);

                // Fast path if we know the write won't cause a flush. This should cover most cases actually.
                if (span.Length < MaxFrameSize - FrameBufferRental.Value.Length)
                {
                    try
                    {
                        Write(span);
                        return ValueTask.CompletedTask;
                    }
                    catch (Exception ex)
                    {
                        return ValueTask.FromException(ex);
                    }
                }

                byte[] spanArray = ArrayPool<byte>.Shared.Rent(span.Length);
                span.CopyTo(spanArray);

                return Core(spanArray, span.Length, cancellationToken);

                async ValueTask Core(byte[] rentArray, int length, CancellationToken token)
                {
                    try
                    {
                        await WriteAsyncCore(rentArray.AsMemory()[..length], token).ConfigureAwait(false);
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(rentArray);
                    }
                }
            }

            public virtual ValueTask WriteAsync(ReadOnlyMemory<byte> memory, CancellationToken cancellationToken = default)
            {
                if (IsDisposed)
                    return ValueTask.FromException(GetDisposedException());

                if (cancellationToken.IsCancellationRequested)
                    return ValueTask.FromCanceled(cancellationToken);

                // Fast path if we know the write won't cause a flush. This should cover most cases actually.
                if (memory.Length < MaxFrameSize - FrameBufferRental.Value.Length)
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

                var buffer = FrameBufferRental.Value;
                int bytesWritten = 0;

                while (bytesWritten < memory.Length)
                {
                    ThrowIfDisposed();

                    long bufferLength = buffer.Length;
                    int frameLeft = (int)(MaxFrameSize - bufferLength);
                    long memoryLeft = memory.Length - bytesWritten;

                    int writeLength = (int)Math.Min(frameLeft, memoryLeft);

                    // Only force a flush if we are actually going to write something
                    if (writeLength > 0 && bufferLength >= MaxFrameSize)
                    {
                        await FlushAsync(CancellationToken.None).ConfigureAwait(false);
                        Debug.Assert(buffer.Length == 0);
                    }

                    buffer.Write(memory.Slice(bytesWritten, writeLength).Span);
                    bytesWritten += writeLength;
                }

                Debug.Assert(bytesWritten == memory.Length);
                Debug.Assert(buffer.Length <= MaxFrameSize);

                BytesWritten += bytesWritten;
            }

            public virtual void Flush()
            {
                ThrowIfDisposed();

                var buffer = FrameBufferRental.Value;

                if (buffer == null || buffer.Length == 0)
                    return;

                Debug.Assert(buffer.Length <= MaxFrameSize);

                SendFrame(GetDataFrameHeader(), buffer);

                buffer.Reset();
                FramesWritten++;
            }

            public virtual ValueTask FlushAsync(CancellationToken cancellationToken = default)
            {
                if (IsDisposed)
                    return ValueTask.FromException(GetDisposedException());

                if (cancellationToken.IsCancellationRequested)
                    return ValueTask.FromCanceled(cancellationToken);

                if (FrameBufferRental.Value == null || FrameBufferRental.Value.Length == 0)
                    return ValueTask.CompletedTask;

                return Core(cancellationToken);

                [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
                async ValueTask Core(CancellationToken token)
                {
                    var buffer = FrameBufferRental.Value;
                    Debug.Assert(buffer.Length <= MaxFrameSize);

                    await SendFrameAsync(GetDataFrameHeader(), buffer.AsReadOnlySequence, token).ConfigureAwait(false);

                    buffer.Reset();
                    FramesWritten++;
                }
            }

            protected void SendFrame(in SphynxFrameHeader header, ReadOnlySequence<byte> frameData)
            {
                Span<byte> frameHeaderBytes = stackalloc byte[SphynxFrameHeader.SIZE];
                header.Serialize(frameHeaderBytes);

                using (Stream.RentLock())
                {
                    var stream = Stream.Stream;

                    stream.Write(frameHeaderBytes);

                    foreach (ReadOnlyMemory<byte> segment in frameData)
                        stream.Write(segment.Span);
                }
            }

            [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
            protected async ValueTask SendFrameAsync(SphynxFrameHeader header,
                ReadOnlySequence<byte> frameData,
                CancellationToken cancellationToken = default)
            {
                using (await Stream.RentLockAsync(cancellationToken).ConfigureAwait(false))
                {
                    var stream = Stream.Stream;

                    await SphynxFrameHeader.SendAsync(in header, stream, cancellationToken).ConfigureAwait(false);

                    foreach (ReadOnlyMemory<byte> segment in frameData)
                    {
                        // We already indicated the frame length in the header, so cancellation isn't an option anymore.
                        await stream.WriteAsync(segment, CancellationToken.None).ConfigureAwait(false);
                    }
                }
            }

            private SphynxFrameHeader GetDataFrameHeader() => new()
            {
                FrameType = SphynxFrameType.CHANNEL_DATA,
                Flags = FramesWritten == 0 ? ChannelDataFlags.CHANNEL_START : (byte)0,
                ChannelId = ChannelId,
                FrameSize = (short)(FrameBufferRental.Value?.Length ?? 0),
            };

            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    try
                    {
                        Debug.Assert(CloseException != null);
                        SendChannelClose(CloseException != _closeSentinel);
                    }
                    catch
                    {
                        // This should only fail if the underlying stream is closed, at which
                        // point we don't really care about signaling our closure
                    }

                    if (FrameBufferRental.Value != null)
                    {
                        FrameBufferRental.Dispose();
                        FrameBufferRental = default;
                    }

                    lock (Writer.OpenChannelsLock)
                    {
                        if (Writer.OpenChannels.Remove(ChannelId, out var channel))
                            Debug.Assert(channel == this);
                    }
                }
            }

            public virtual async ValueTask DisposeAsyncCore()
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

                if (FrameBufferRental.Value != null)
                {
                    FrameBufferRental.Dispose();
                    FrameBufferRental = default;
                }

                lock (Writer.OpenChannelsLock)
                {
                    if (Writer.OpenChannels.Remove(ChannelId, out var channel))
                        Debug.Assert(channel == this);
                }
            }

            private void SendChannelClose(bool aborting = false)
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

                    SendFrame(abortHeader, ReadOnlySequence<byte>.Empty);
                    return;
                }

                var header = GetDataFrameHeader().WithFlags(ChannelDataFlags.CHANNEL_END);
                SendFrame(header, FrameBuffer);
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

                var header = GetDataFrameHeader().WithFlags(ChannelDataFlags.CHANNEL_END);
                return SendFrameAsync(header, FrameBuffer, cancellationToken);
            }
        }
    }
}
