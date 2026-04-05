// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using Sphynx.Storage;
using Sphynx.Utils;

namespace Sphynx.Network.Transport
{
    public class PoolableChannelWriter : SphynxChannelWriter
    {
        protected readonly IObjectPool<PoolableChannel> PooledChannels;

        public PoolableChannelWriter(Stream stream, bool ownsStream = false)
            : this(stream, new ObjectPool<PoolableChannel>(), ownsStream)
        {
        }

        public PoolableChannelWriter(Stream stream, int poolSize, bool ownsStream = false)
            : this(stream, new ObjectPool<PoolableChannel>(poolSize), ownsStream)
        {
        }

        protected PoolableChannelWriter(Stream stream, IObjectPool<PoolableChannel> pool, bool ownsStream = false) : base(stream, ownsStream)
        {
            PooledChannels = pool;
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

        protected override PoolableChannel NewChannel() => (PoolableChannel)base.NewChannel();

        public void Reset(Stream stream, bool? ownsStream = null)
        {
            if (!IsDisposed)
                // Since disposing requires potentially draining the reader's buffer,
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

        protected class PoolableChannel : Channel
        {
            // Don't need to explicitly pool the stream; disposing the stream does nothing.

            public PoolableChannel(PoolableChannelWriter parent, ChannelId channelId) : base(parent, channelId)
            {
            }

            public virtual void Reset(ChannelId? newChannelId = null)
            {
                if (!IsDisposed)
                    // Since disposing requires potentially sending an DATA/ABORT frame across the channel,
                    // it's possible that calling Dispose() here would block, which is behaviour we
                    // probably want to avoid.
                    throw new InvalidOperationException($"{GetType().Name} ({nameof(ChannelId)}: {ChannelId}) must be disposed before resetting");

                if (FrameBufferRental.Value != null)
                    FrameBufferRental.Dispose();

                FrameBufferRental = SequencePool.Shared.Rent();

                if (newChannelId != null)
                    ChannelId = newChannelId.Value;

                FramesWritten = 0;
                BytesWritten = 0;

                CloseException = null;
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);

                if (disposing)
                {
                    ((PoolableChannelWriter)Parent).PooledChannels.Return(this);
                }
            }

            protected override async ValueTask DisposeAsyncCore()
            {
                await base.DisposeAsyncCore().ConfigureAwait(false);
                ((PoolableChannelWriter)Parent).PooledChannels.Return(this);
            }
        }
    }
}
