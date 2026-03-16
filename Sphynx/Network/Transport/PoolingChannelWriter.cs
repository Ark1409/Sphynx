// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using Sphynx.Storage;
using Version = Sphynx.Core.Version;

namespace Sphynx.Network.Transport
{
    public class PoolingChannelWriter : SphynxChannelWriter
    {
        /// <summary>
        /// The currently supported protocol version.
        /// </summary>
        public new static Version ProtocolVersion => PoolableChannel.ProtocolVersion;

        protected readonly IObjectPool<PoolableChannel> PooledChannels;

        public PoolingChannelWriter(Stream stream, bool ownsStream = false)
            : this(stream, new ObjectPool<PoolableChannel>(), ownsStream)
        {
        }

        public PoolingChannelWriter(Stream stream, int poolSize, bool ownsStream = false)
            : this(stream, new ObjectPool<PoolableChannel>(poolSize), ownsStream)
        {
        }

        protected PoolingChannelWriter(Stream stream, IObjectPool<PoolableChannel> pool, bool ownsStream = false) : base(stream, ownsStream)
        {
            PooledChannels = pool;
        }

        protected override PoolableChannel NewChannel(long channelId)
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                while (PooledChannels.TryTake(out var channel))
                    Debug.Assert(channel.IsDisposed);
            }

            base.Dispose(disposing);
        }

        public override ValueTask DisposeAsync()
        {
            while (PooledChannels.TryTake(out var channel))
                Debug.Assert(channel.IsDisposed);

            return base.DisposeAsync();
        }

        protected class PoolableChannel : V001Channel
        {
            // Don't need to explicitly pool the stream; disposing the stream does nothing.

            public PoolableChannel(PoolingChannelWriter writer, long channelId) : base(writer, channelId)
            {
            }

            public virtual void Reset(long? newChannelId = null)
            {
                if (!IsDisposed)
                    // Since disposing requires potentially sending an END frame across the channel,
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

                DisposeException = null;
                IsDisposed = false;
            }

            protected override void Dispose(bool disposing)
            {
                if (IsDisposed)
                    return;

                base.Dispose(disposing);

                if (disposing)
                {
                    // Interlocked.MemoryBarrier();
                    ((PoolingChannelWriter)Writer).PooledChannels.Return(this);
                }
            }

            public override async ValueTask DisposeAsync()
            {
                if (IsDisposed)
                    return;

                await base.DisposeAsync().ConfigureAwait(false);
                // Interlocked.MemoryBarrier();
                ((PoolingChannelWriter)Writer).PooledChannels.Return(this);
            }
        }
    }
}
