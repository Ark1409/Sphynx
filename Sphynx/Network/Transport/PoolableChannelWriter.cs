// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using Nerdbank.Streams;
using Sphynx.Storage;
using Sphynx.Utils;

namespace Sphynx.Network.Transport
{
    public class PoolableChannelWriter : DefaultChannelWriter
    {
        protected readonly IObjectPool<PoolableChannel> PooledChannels;
        protected readonly SequencePool SequencePool;

        public PoolableChannelWriter(Stream stream, SequencePool? sequencePool = null, bool ownsStream = false)
            : this(stream, new ObjectPool<PoolableChannel>(), sequencePool, ownsStream)
        {
        }

        public PoolableChannelWriter(Stream stream, int channelPoolSize, SequencePool? sequencePool = null, bool ownsStream = false)
            : this(stream, new ObjectPool<PoolableChannel>(channelPoolSize), sequencePool, ownsStream)
        {
        }

        protected PoolableChannelWriter(Stream stream, IObjectPool<PoolableChannel> pool, SequencePool? sequencePool = null, bool ownsStream = false)
            : base(stream, ownsStream)
        {
            PooledChannels = pool;
            SequencePool = sequencePool ?? SequencePool.Shared;
        }

        protected override PoolableChannel NewChannel(ChannelId channelId)
        {
            if (PooledChannels.TryTake(out var channel))
            {
                Debug.Assert(channel.IsDisposed);
                channel.Reset(channelId);
                return channel;
            }

            return new PoolableChannel(this, channelId);
        }

        public void Reset(Stream stream, bool? ownsStream = null)
        {
            if (!IsDisposed)
                // Since disposing requires potentially sending an DATA/ABORT frame(s) across the channel,
                // it's possible that calling Dispose() here would block, which is behaviour we
                // probably want to avoid.
                throw new InvalidOperationException($"{GetType().Name} must be disposed before resetting");

            Stream = new StreamSynchronizer(stream);

            if (ownsStream != null)
                OwnsStream = ownsStream.Value;

            IsDisposed = false;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                while (PooledChannels.TryTake(out var channel))
                    Debug.Assert(channel.IsDisposed);
            }
        }

        protected override async ValueTask DisposeAsyncCore()
        {
            await base.DisposeAsyncCore().ConfigureAwait(false);

            while (PooledChannels.TryTake(out var channel))
                Debug.Assert(channel.IsDisposed);
        }

        protected class PoolableChannel : DefaultChannel
        {
            protected override Sequence<byte> FrameBuffer => _frameBufferRental!.Value.Value;
            private SequencePool.Rental? _frameBufferRental;

            protected override PoolableChannelWriter Parent { get; }

            public PoolableChannel(PoolableChannelWriter parent, ChannelId channelId) : base(parent, channelId)
            {
                Parent = parent;
                _frameBufferRental = parent.SequencePool.Rent();
            }

            public virtual void Reset(ChannelId? newChannelId = null)
            {
                if (!IsDisposed)
                    // Since disposing requires potentially sending an DATA/ABORT frame across the channel,
                    // it's possible that calling Dispose() here would block, which is behaviour we
                    // probably want to avoid.
                    throw new InvalidOperationException($"{GetType().Name} ({nameof(ChannelId)}: {ChannelId}) must be disposed before resetting");

                Debug.Assert(_frameBufferRental == null);
                _frameBufferRental = Parent.SequencePool.Rent();

                if (newChannelId != null)
                    ChannelId = newChannelId.Value;

                FramesWritten = 0;
                BytesWritten = 0;

                IsDisposeReserved = 0;
                CloseException = null;
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);

                if (disposing)
                {
                    if (_frameBufferRental != null)
                    {
                        _frameBufferRental.Value.Dispose();
                        _frameBufferRental = null;
                    }

                    Parent.PooledChannels.Return(this);
                }
            }

            protected override async ValueTask DisposeAsyncCore()
            {
                await base.DisposeAsyncCore().ConfigureAwait(false);

                if (_frameBufferRental != null)
                {
                    _frameBufferRental.Value.Dispose();
                    _frameBufferRental = null;
                }

                Parent.PooledChannels.Return(this);
            }
        }
    }
}
