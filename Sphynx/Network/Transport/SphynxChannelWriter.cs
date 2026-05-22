// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft;
using Nerdbank.Streams;

namespace Sphynx.Network.Transport
{
    public abstract class SphynxChannelWriter : IDisposable, IAsyncDisposable
    {
        protected bool IsDisposed { get => _disposed != 0; set => _disposed = value ? 1 : 0; }
        private volatile int _disposed;

        public abstract Channel OpenChannel();
        public abstract Channel OpenChannel(ChannelId channelId);
        public abstract bool TryGetChannel(ChannelId channelId, bool createIfNotExists, [NotNullWhen(true)] out Channel? channel);

        public virtual bool TryGetChannel(ChannelId channelId, [NotNullWhen(true)] out Channel? channel)
        {
            return TryGetChannel(channelId, false, out channel);
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

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

            await DisposeAsyncCore().ConfigureAwait(false);
            Dispose(false);
            GC.SuppressFinalize(this);
        }

        protected virtual ValueTask DisposeAsyncCore()
        {
            return ValueTask.CompletedTask;
        }

        private ObjectDisposedException? _disposeException;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ThrowIfDisposed()
        {
            if (IsDisposed)
                ThrowDisposedException();

            [DoesNotReturn]
            [StackTraceHidden]
            void ThrowDisposedException() => throw GetDisposeException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected ObjectDisposedException GetDisposeException() => _disposeException ??= new ObjectDisposedException(GetType().Name);

        public abstract class Channel : IAsyncDisposable, IDisposableObservable
        {
            protected static readonly ChannelClosedException CloseSentinel = new();

            public virtual ChannelId ChannelId { get; protected set; }

            [MemberNotNullWhen(true, nameof(CloseException))]
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

            protected virtual SphynxChannelWriter Parent { get; }

            public Channel(SphynxChannelWriter parent, ChannelId channelId)
            {
                Parent = parent;
                ChannelId = channelId;
            }

            public abstract void Write(ReadOnlySpan<byte> span);
            public abstract ValueTask WriteAsync(ReadOnlyMemory<byte> memory, CancellationToken cancellationToken = default);
            public abstract ValueTask FlushAsync(CancellationToken cancellationToken = default);
            public abstract void Flush();

            public virtual void Write(ReadOnlySequence<byte> payload)
            {
                foreach (var segment in payload)
                    Write(segment.Span);
            }

            public void Write(ReadOnlyMemory<byte> memory) => Write(memory.Span);

            public virtual ValueTask WriteAsync(ReadOnlySequence<byte> payload, CancellationToken cancellationToken = default)
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

            private ChannelBufferWriter? _channelBufferWriter;

            public virtual IBufferWriter<byte> AsBufferWriter()
            {
                _channelBufferWriter ??= new ChannelBufferWriter(this);
                return _channelBufferWriter;
            }

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

            protected virtual void Dispose(bool disposing)
            {
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

            protected virtual ValueTask DisposeAsyncCore()
            {
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

        /// <summary>
        /// A stream that writes into a specific channel.
        /// </summary>
        protected class ChannelStream : Stream
        {
            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => !Channel.IsDisposed;
            public override bool CanTimeout => false;
            public override long Length => throw new NotSupportedException();

            public override long Position
            {
                get => Channel.BytesWritten;
                set => throw new NotSupportedException();
            }

            public Channel Channel { get; }
            public bool LeaveOpen { get; set; }

            public ChannelStream(Channel channel, bool leaveOpen = true)
            {
                Channel = channel;
                LeaveOpen = leaveOpen;
            }

            public sealed override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
                TaskToAsyncResult.Begin(WriteAsync(buffer, offset, count, default), callback, state);

            public sealed override void EndWrite(IAsyncResult asyncResult) =>
                TaskToAsyncResult.End(asyncResult);

            public override ValueTask WriteAsync(ReadOnlyMemory<byte> memory, CancellationToken cancellationToken = default)
            {
                return Channel.WriteAsync(memory, cancellationToken);
            }

            public override void Write(byte[] buffer, int offset, int count) => Write(new ReadOnlySpan<byte>(buffer, offset, count));
            public override void WriteByte(byte value) => Write(stackalloc byte[] { value });

            public override void Write(ReadOnlySpan<byte> span)
            {
                Channel.Write(span);
            }

            public override Task FlushAsync(CancellationToken cancellationToken)
            {
                return Channel.FlushAsync(cancellationToken).AsTask();
            }

            public override void Flush()
            {
                Channel.Flush();
            }

            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
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

        protected class ChannelBufferWriter : IBufferWriter<byte>
        {
            public Channel Channel { get; }
            private readonly Sequence<byte> _writer = new();

            public ChannelBufferWriter(Channel channel)
            {
                Channel = channel;
            }

            public void Advance(int count)
            {
                _writer.Advance(count);
                var seq = _writer.AsReadOnlySequence;
                Channel.Write(seq);
                _writer.AdvanceTo(seq.End);
            }

            public Memory<byte> GetMemory(int sizeHint = 0) => _writer.GetMemory(sizeHint);
            public Span<byte> GetSpan(int sizeHint = 0) => _writer.GetSpan(sizeHint);
        }
    }
}
