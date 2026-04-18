// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft;

namespace Sphynx.Network.Transport
{
    public partial class SphynxChannelWriter : IDisposable, IAsyncDisposable
    {
        protected bool OwnsStream;

        protected bool IsDisposed { get => _disposed != 0; set => _disposed = value ? 1 : 0; }
        private volatile int _disposed;

        /// <summary>
        /// The current number of open writing channels.
        /// </summary>
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

        private ChannelId _lastChannelId;

        public Channel OpenChannel(ChannelId channelId)
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

        public bool TryGetChannel(ChannelId channelId, [NotNullWhen(true)] out Channel? channel)
        {
            return TryGetChannel(channelId, false, out channel);
        }

        public bool TryGetChannel(ChannelId channelId, bool createIfNotExists, [NotNullWhen(true)] out Channel? channel)
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

        protected virtual Channel NewChannel(ChannelId channelId) => new Channel(this, channelId);

        protected virtual Channel NewChannel()
        {
            ChannelId channelId;

            lock (OpenChannelsLock)
            {
                do
                {
                    _lastChannelId = (_lastChannelId + 1) & ChannelId.MaxValue;
                    channelId = _lastChannelId;
                } while (OpenChannels.ContainsKey(channelId));
            }

            return NewChannel(channelId);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (OpenChannelsLock)
                {
                    var disposeException = new ObjectDisposedException(GetType().Name);

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

                    OpenChannels.Clear();
                }

                if (OwnsStream)
                    Stream.Dispose();
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            await DisposeAsyncCore().ConfigureAwait(false);
            Dispose(false);
            GC.SuppressFinalize(this);
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            lock (OpenChannelsLock)
            {
                // Allow pending operations to finish.
            }

            var disposeException = new ObjectDisposedException(GetType().Name);

            // ReSharper disable InconsistentlySynchronizedField
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

            OpenChannels.Clear();

            if (OwnsStream)
                await Stream.DisposeAsync().ConfigureAwait(false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ThrowIfDisposed()
        {
            if (IsDisposed)
                ThrowDisposedException();

            [DoesNotReturn]
            [StackTraceHidden]
            [MethodImpl(MethodImplOptions.NoInlining)]
            void ThrowDisposedException() => throw new ObjectDisposedException(GetType().Name);
        }

        public partial class Channel : IAsyncDisposable, IDisposableObservable
        {
            private static readonly ChannelClosedException _closeSentinel = new();

            public virtual ChannelId ChannelId { get; protected set; }

            public bool IsDisposed => CloseException != null;
            protected volatile ChannelClosedException? CloseException;
            public Action<Channel, Exception?>? OnDispose { protected get; set; }

            /// <summary>
            /// Number of bytes written to the underlying stream.
            /// </summary>
            public virtual long BytesWritten { get; protected set; }

            /// <summary>
            /// Number of <see cref="SphynxFrameHeader">frames</see> written to the underlying stream.
            /// </summary>
            public virtual long FramesWritten { get; protected set; }

            public SphynxChannelWriter Parent { get; }

            public virtual void Write(ReadOnlySequence<byte> payload)
            {
                foreach (var segment in payload)
                    Write(segment.Span);
            }

            public void Write(ReadOnlyMemory<byte> memory) => Write(memory.Span);

            public ValueTask WriteAsync(ReadOnlySequence<byte> payload, CancellationToken cancellationToken = default)
            {
                if (cancellationToken.IsCancellationRequested)
                    return ValueTask.FromCanceled(cancellationToken);

                if (payload.IsSingleSegment)
                    return WriteAsync(payload.First, cancellationToken);

                return Core(payload, cancellationToken);

                async ValueTask Core(ReadOnlySequence<byte> seq, CancellationToken token)
                {
                    token.ThrowIfCancellationRequested();

                    foreach (var segment in seq)
                        await WriteAsync(segment, CancellationToken.None).ConfigureAwait(false);
                }
            }

            private ChannelStream? _channelStream;

            public virtual Stream AsStream(bool leaveOpen = true)
            {
                _channelStream ??= new ChannelStream(this);
                _channelStream.LeaveOpen = leaveOpen;
                return _channelStream;
            }

            public virtual IBufferWriter<byte> AsBufferWriter() => FrameBuffer;

            public void Dispose() => Dispose(null);

            public void Dispose(Exception? disposeException)
            {
                if (!TryReserveDispose(disposeException))
                    return;

                try
                {
                    OnDispose?.Invoke(this, CloseException.InnerException ?? disposeException);
                    OnDispose = null;
                }
                catch
                {
                    // ignore
                }

                Dispose(true);
                GC.SuppressFinalize(this);
            }

            public ValueTask DisposeAsync() => DisposeAsync(null);

            public async ValueTask DisposeAsync(Exception? disposeException)
            {
                if (!TryReserveDispose(disposeException))
                    return;

                try
                {
                    OnDispose?.Invoke(this, CloseException.InnerException ?? disposeException);
                    OnDispose = null;
                }
                catch
                {
                    // ignore
                }

                await DisposeAsyncCore().ConfigureAwait(false);
                Dispose(false);
                GC.SuppressFinalize(this);
            }

            [MemberNotNull(nameof(CloseException))]
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
            public bool LeaveOpen { get; set; }

            public ChannelStream(Channel channel, bool leaveOpen = true)
            {
                _channel = channel;
                LeaveOpen = leaveOpen;
            }

            public sealed override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
                TaskToAsyncResult.Begin(WriteAsync(buffer, offset, count, default), callback, state);

            public sealed override void EndWrite(IAsyncResult asyncResult) =>
                TaskToAsyncResult.End(asyncResult);

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
                return _channel.FlushAsync(cancellationToken).AsTask();
            }

            public override void Flush()
            {
                _channel.Flush();
            }

            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

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
    }
}
