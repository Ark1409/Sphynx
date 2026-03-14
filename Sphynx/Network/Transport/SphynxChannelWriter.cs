// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft;
using Nerdbank.Streams;
using Sphynx.Network.Packet;
using Sphynx.Storage;
using Sphynx.Utils;
using Version = Sphynx.Core.Version;

namespace Sphynx.Network.Transport
{
    public class SphynxChannelWriter : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// The currently supported protocol version.
        /// </summary>
        public static Version ProtocolVersion => V001Channel.ProtocolVersion;

        public int OpenChannelCount
        {
            get
            {
                lock (OpenChannelsLock)
                {
                    return OpenChannels.Count;
                }
            }
        }

        protected readonly Dictionary<long, Channel> OpenChannels = new();
        protected readonly object OpenChannelsLock = new();

        private long _lastChannelId;

        /// <summary>
        /// A lockable wrapper over the underlying stream to allow for serial output.
        /// </summary>
        protected StreamSynchronizer Stream { get; }

        protected readonly bool OwnsStream;
        private int _disposed;

        public SphynxChannelWriter(Stream stream, bool ownsStream = false)
        {
            Stream = new StreamSynchronizer(stream);
            OwnsStream = ownsStream;
        }

        public Channel OpenChannel(long channelId)
        {
            ThrowIfDisposed();

            lock (OpenChannelsLock)
            {
                ThrowIfDisposed();

                if (OpenChannels.ContainsKey(channelId))
                    throw new ArgumentException($"Channel '{channelId}' already opened");

                return OpenChannels[channelId] = NewChannel(channelId);
            }
        }

        public Channel OpenChannel()
        {
            ThrowIfDisposed();

            lock (OpenChannelsLock)
            {
                ThrowIfDisposed();

                var channel = NewChannel();
                return OpenChannels[channel.ChannelId] = channel;
            }
        }

        public bool TryGetChannel(long channelId, [NotNullWhen(true)] out Channel? channel)
        {
            return TryGetChannel(channelId, false, out channel);
        }

        public bool TryGetChannel(long channelId, bool createIfNotExists, [NotNullWhen(true)] out Channel? channel)
        {
            ThrowIfDisposed();

            lock (OpenChannelsLock)
            {
                ThrowIfDisposed();

                if (OpenChannels.TryGetValue(channelId, out channel))
                    return true;

                channel = createIfNotExists ? OpenChannel(channelId) : null;
            }

            return createIfNotExists;
        }

        protected virtual Channel NewChannel(long channelId) => new V001Channel(this, channelId);

        protected virtual Channel NewChannel()
        {
            long channelId;

            lock (OpenChannelsLock)
            {
                do
                {
                    channelId = ++_lastChannelId;
                } while (OpenChannels.ContainsKey(channelId));
            }

            return NewChannel(channelId);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            if (disposing)
            {
                lock (OpenChannelsLock)
                {
                    var channels = OpenChannels.Values;
                    var disposeException = new ObjectDisposedException(GetType().Name);

                    foreach (var channel in channels)
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

                    OpenChannels.Clear();
                }

                if (OwnsStream)
                    Stream.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            lock (OpenChannelsLock)
            {
                // Allow pending operations to finish.
            }

            var disposeException = new ObjectDisposedException(GetType().Name);

            // ReSharper disable InconsistentlySynchronizedField
            foreach (var channel in OpenChannels.Values)
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

            OpenChannels.Clear();

            if (OwnsStream)
                await Stream.DisposeAsync().ConfigureAwait(false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) != 0)
                ThrowDisposedException();

            [DoesNotReturn]
            [StackTraceHidden]
            [MethodImpl(MethodImplOptions.NoInlining)]
            void ThrowDisposedException() => throw new ObjectDisposedException(GetType().Name);
        }

        #region Channels

        public abstract class Channel : IAsyncDisposable, IDisposableObservable
        {
            public virtual long ChannelId { get; protected set; }

            public virtual Stream AsStream => _channelStream ??= new ChannelStream(this);
            private Stream? _channelStream;

            public virtual bool IsDisposed { get; protected set; }
            protected Exception? DisposeException { get; set; }
            public Action<Channel, Exception?>? OnDispose { protected get; set; }

            public abstract long BytesWritten { get; protected set; }
            public abstract long FramesWritten { get; protected set; }

            public SphynxChannelWriter Writer { get; }
            protected StreamSynchronizer Stream => Writer.Stream;

            public Channel(SphynxChannelWriter writer, long channelId)
            {
                Writer = writer;
                ChannelId = channelId;
            }

            public virtual void Write(ReadOnlySequence<byte> payload)
            {
                foreach (var segment in payload)
                    Write(segment.Span);
            }

            public virtual ValueTask WriteAsync(ReadOnlySpan<byte> span, CancellationToken cancellationToken = default)
            {
                if (cancellationToken.IsCancellationRequested)
                    return ValueTask.FromCanceled(cancellationToken);

                byte[] rentArray = ArrayPool<byte>.Shared.Rent(span.Length);
                ValueTask writeTask;

                try
                {
                    span.CopyTo(rentArray);
                    writeTask = WriteAsync(rentArray.AsMemory()[..span.Length], cancellationToken);

                    if (writeTask.IsCompleted)
                    {
                        writeTask.GetAwaiter().GetResult();

                        ArrayPool<byte>.Shared.Return(rentArray);
                        return writeTask;
                    }
                }
                catch (Exception ex)
                {
                    ArrayPool<byte>.Shared.Return(rentArray);
                    return ValueTask.FromException(ex);
                }

                return Core(writeTask, rentArray);

                static async ValueTask Core(ValueTask writeTask, byte[] rentArray)
                {
                    try
                    {
                        await writeTask.ConfigureAwait(false);
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(rentArray);
                    }
                }
            }

            public void Write(ReadOnlyMemory<byte> memory) => Write(new ReadOnlySequence<byte>(memory));

            public ValueTask WriteAsync(ReadOnlyMemory<byte> memory, CancellationToken cancellationToken = default) =>
                WriteAsync(new ReadOnlySequence<byte>(memory), cancellationToken);

            public abstract void Write(ReadOnlySpan<byte> payload);
            public abstract ValueTask WriteAsync(ReadOnlySequence<byte> payload, CancellationToken cancellationToken = default);
            public abstract void Flush();
            public abstract ValueTask FlushAsync(CancellationToken cancellationToken = default);

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
                    return ValueTask.FromException(GetDisposedException());

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
                lock (Writer.OpenChannelsLock)
                {
                    if (Writer.OpenChannels.Remove(ChannelId, out var channel))
                        Debug.Assert(channel == this);
                }
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

            public override long BytesWritten { get; protected set; }
            public override long FramesWritten { get; protected set; }

            public ReadOnlySequence<byte> FrameBuffer => FrameBufferRental.Value.AsReadOnlySequence;
            protected SequencePool.Rental FrameBufferRental;

            public V001Channel(SphynxChannelWriter writer, long channelId) : base(writer, channelId)
            {
                FrameBufferRental = SequencePool.Shared.Rent();
            }

            public override void Write(ReadOnlySpan<byte> payload)
            {
                var buffer = FrameBufferRental.Value;
                int bytesWritten = 0;

                while (bytesWritten < payload.Length)
                {
                    ThrowIfDisposed();

                    long bufferLength = buffer.Length;
                    int frameLeft = (int)(MAX_FRAME_SIZE - bufferLength);
                    long payloadLeft = payload.Length - bytesWritten;

                    int writeLength = (int)Math.Min(frameLeft, payloadLeft);

                    // Only force a flush if we are actually going to write something
                    if (writeLength > 0 && bufferLength >= MAX_FRAME_SIZE)
                    {
                        Flush();
                        Debug.Assert(buffer.Length == 0);
                    }

                    buffer.Write(payload.Slice(bytesWritten, writeLength));
                    bytesWritten += writeLength;
                }

                Debug.Assert(bytesWritten == payload.Length);
                Debug.Assert(buffer.Length <= MAX_FRAME_SIZE);

                BytesWritten += bytesWritten;
            }

            public override ValueTask WriteAsync(ReadOnlySpan<byte> span, CancellationToken cancellationToken = default)
            {
                if (cancellationToken.IsCancellationRequested)
                    return ValueTask.FromCanceled(cancellationToken);

                // Fast path if we know the write won't cause a flush. This should cover most cases actually.
                if (span.Length < MAX_FRAME_SIZE - FrameBufferRental.Value.Length)
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

                return base.WriteAsync(span, cancellationToken);
            }

            public override ValueTask WriteAsync(ReadOnlySequence<byte> payload, CancellationToken cancellationToken = default)
            {
                if (IsDisposed)
                    return ValueTask.FromException(GetDisposedException());

                if (cancellationToken.IsCancellationRequested)
                    return ValueTask.FromCanceled(cancellationToken);

                // Fast path if we know the write won't cause a flush. This should cover most cases actually.
                if (payload.Length < MAX_FRAME_SIZE - FrameBufferRental.Value.Length)
                {
                    try
                    {
                        Write(payload);
                        return ValueTask.CompletedTask;
                    }
                    catch (Exception ex)
                    {
                        return ValueTask.FromException(ex);
                    }
                }

                return Core(payload);

                async ValueTask Core(ReadOnlySequence<byte> sequence)
                {
                    var buffer = FrameBufferRental.Value;
                    int bytesWritten = 0;

                    while (bytesWritten < sequence.Length)
                    {
                        ThrowIfDisposed();

                        long bufferLength = buffer.Length;
                        int frameLeft = (int)(MAX_FRAME_SIZE - bufferLength);
                        long memoryLeft = sequence.Length - bytesWritten;

                        int writeLength = (int)Math.Min(frameLeft, memoryLeft);

                        // Only force a flush if we are actually going to write something
                        if (writeLength > 0 && bufferLength >= MAX_FRAME_SIZE)
                        {
                            await FlushAsync(CancellationToken.None).ConfigureAwait(false);
                            Debug.Assert(buffer.Length == 0);
                        }

                        buffer.Write(sequence.Slice(bytesWritten, writeLength));
                        bytesWritten += writeLength;
                    }

                    Debug.Assert(bytesWritten == sequence.Length);
                    Debug.Assert(buffer.Length <= MAX_FRAME_SIZE);

                    BytesWritten += bytesWritten;
                }
            }

            public override void Flush()
            {
                ThrowIfDisposed();

                var buffer = FrameBufferRental.Value;

                if (buffer == null || buffer.Length == 0)
                    return;

                Debug.Assert(buffer.Length <= MAX_FRAME_SIZE);

                SendFrame(CreateFrameHeader(), buffer);

                buffer.Reset();
                FramesWritten++;
            }

            public override ValueTask FlushAsync(CancellationToken cancellationToken = default)
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
                    Debug.Assert(buffer.Length <= MAX_FRAME_SIZE);

                    await SendFrameAsync(CreateFrameHeader(), buffer.AsReadOnlySequence, token).ConfigureAwait(false);

                    buffer.Reset();
                    FramesWritten++;
                }
            }

            protected void SendFrame(SphynxFrameHeader header, ReadOnlySequence<byte> frameData)
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

                    await SphynxFrameHeader.SendAsync(header, stream, cancellationToken).ConfigureAwait(false);

                    foreach (ReadOnlyMemory<byte> segment in frameData)
                    {
                        // We already indicated the frame length in the header, so cancellation isn't an option anymore.
                        await stream.WriteAsync(segment, CancellationToken.None).ConfigureAwait(false);
                    }
                }
            }

            protected SphynxFrameHeader CreateFrameHeader() => new()
            {
                Version = ProtocolVersion,
                FrameType = FramesWritten == 0 ? SphynxFrameType.CHANNEL_START : SphynxFrameType.CHANNEL_DATA,
                ChannelId = ChannelId,
                FrameSize = (short)(FrameBufferRental.Value?.Length ?? 0),
            };

            protected override void Dispose(bool disposing)
            {
                if (IsDisposed)
                    return;

                if (disposing)
                {
                    try
                    {
                        Flush();

                        if (FramesWritten > 0)
                            SendEndFrame(DisposeException != null);
                    }
                    catch
                    {
                        // Best effort, which should only fail if the underlying stream
                        // is "broken"
                    }

                    if (FrameBufferRental.Value != null)
                    {
                        FrameBufferRental.Dispose();
                        FrameBufferRental = default;
                    }
                }

                base.Dispose(disposing);
            }

            public override async ValueTask DisposeAsync()
            {
                if (IsDisposed)
                    return;

                try
                {
                    await FlushAsync().ConfigureAwait(false);

                    if (FramesWritten > 0)
                        await SendEndFrameAsync(DisposeException != null).ConfigureAwait(false);
                }
                catch
                {
                    // Best effort, which should only fail if the underlying stream
                    // is "broken"
                }

                if (FrameBufferRental.Value != null)
                {
                    FrameBufferRental.Dispose();
                    FrameBufferRental = default;
                }

                await base.DisposeAsync().ConfigureAwait(false);
            }

            protected void SendEndFrame(bool aborting = false)
            {
                var header = new SphynxFrameHeader
                {
                    Version = ProtocolVersion,
                    FrameType = SphynxFrameType.CHANNEL_END,
                    ChannelId = ChannelId,
                    FrameSize = 1,
                };

                byte endCode = (byte)(aborting ? 1 : 0);
                var sequence = FrameBufferRental.Value;

                if (sequence == null)
                {
                    SendFrame(header, new ReadOnlySequence<byte>([endCode]));
                }
                else
                {
                    // Sequence isn't polluted because we don't Advance()
                    sequence.GetSpan(1)[0] = endCode;
                    SendFrame(header, sequence.AsReadOnlySequence.Slice(0, 1));
                }
            }

            protected ValueTask SendEndFrameAsync(bool aborting = false, CancellationToken cancellationToken = default)
            {
                var header = new SphynxFrameHeader
                {
                    Version = ProtocolVersion,
                    FrameType = SphynxFrameType.CHANNEL_END,
                    ChannelId = ChannelId,
                    FrameSize = 1,
                };

                byte endCode = (byte)(aborting ? 1 : 0);
                var sequence = FrameBufferRental.Value;

                if (sequence == null)
                    return SendFrameAsync(header, new ReadOnlySequence<byte>([endCode]), cancellationToken);

                // Sequence isn't polluted because we don't Advance()
                sequence.GetSpan(1)[0] = endCode;
                return SendFrameAsync(header, sequence.AsReadOnlySequence.Slice(0, 1), cancellationToken);
            }
        }

        /// <summary>
        /// A stream that writes into a specific channel.
        /// </summary>
        protected class ChannelStream : Stream
        {
            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => !_channel.IsDisposed;
            public override bool CanTimeout => false;
            public override long Length => throw new NotSupportedException();

            public override long Position
            {
                get => _channel.BytesWritten;
                set => throw new NotSupportedException();
            }

            private readonly Channel _channel;

            public ChannelStream(Channel channel)
            {
                _channel = channel;
            }

            public override ValueTask WriteAsync(ReadOnlyMemory<byte> memory, CancellationToken cancellationToken = default)
            {
                return _channel.WriteAsync(memory, cancellationToken);
            }

            public override void Write(byte[] buffer, int offset, int count) => Write(new ReadOnlySpan<byte>(buffer, offset, count));
            public override void WriteByte(byte value) => Write(stackalloc byte[] { value });

            public override void Write(ReadOnlySpan<byte> span)
            {
                _channel.Write(span);
            }

            public override Task FlushAsync(CancellationToken cancellationToken)
            {
                if (_channel.IsDisposed)
                    return Task.FromException(new ObjectDisposedException(_channel.GetType().Name));

                if (_channel.BytesWritten == 0 || (_channel is V001Channel channel && channel.FrameBuffer.IsEmpty))
                    return Task.CompletedTask;

                return _channel.FlushAsync(cancellationToken).AsTask();
            }

            public override void Flush()
            {
                _channel.Flush();
            }

            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

            public override void SetLength(long value) => throw new NotSupportedException();

            // Dispose does nothing
        }

        #endregion
    }
}
