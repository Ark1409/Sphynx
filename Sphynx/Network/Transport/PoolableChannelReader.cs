using System.Diagnostics;
using System.IO.Pipelines;
using Sphynx.Storage;

namespace Sphynx.Network.Transport
{
    public class PoolableChannelReader : SphynxChannelReader
    {
        public static readonly PipeOptions DefaultPipeOptions = Channel.DefaultPipeOptions;

        protected readonly IObjectPool<PoolableChannel> PooledChannels;
        protected readonly IObjectPool<Pipe>? PipePool;

        public PoolableChannelReader(Stream stream, IObjectPool<Pipe>? pipePool = null, bool ownsStream = false)
            : this(stream, new ObjectPool<PoolableChannel>(), pipePool, ownsStream)
        {
        }

        public PoolableChannelReader(Stream stream, int channelPoolSize, IObjectPool<Pipe>? pipePool = null, bool ownsStream = false)
            : this(stream, new ObjectPool<PoolableChannel>(channelPoolSize), pipePool, ownsStream)
        {
        }

        protected PoolableChannelReader(Stream stream, IObjectPool<PoolableChannel> pool, IObjectPool<Pipe>? pipePool = null,
            bool ownsStream = false)
            : base(stream, ownsStream)
        {
            PooledChannels = pool;
            PipePool = pipePool;
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
                // Since disposing requires potentially draining the reader's buffer,
                // it's possible that calling Dispose() here would block, which is behaviour we
                // probably want to avoid.
                throw new InvalidOperationException($"{GetType().Name} must be disposed before resetting");

            RunCts = new CancellationTokenSource();
            Stream = stream;

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
            private static ObjectPool<Pipe> _pipePool = new(() => new Pipe(DefaultPipeOptions));

            private bool _usedParentPipe;

            public PoolableChannel(PoolableChannelReader parent, ChannelId channelId) : base(parent, channelId)
            {
            }

            public virtual void Reset(ChannelId? channelId = null)
            {
                if (!IsDisposed)
                    // Since disposing requires potentially draining the reader's buffer,
                    // it's possible that calling Dispose() here would block, which is behaviour we
                    // probably want to avoid.
                    throw new InvalidOperationException($"{GetType().Name} ({nameof(ChannelId)}: {ChannelId}) must be disposed before resetting");

                _usedParentPipe = false;

                if (channelId.HasValue)
                    ChannelId = channelId.Value;

                BytesRead = 0;

                CloseException = null;
            }

            protected override Pipe NewPipe()
            {
                var parent = (PoolableChannelReader)Parent;

                if (parent.PipePool?.TryTake(out var pipe) ?? false)
                {
                    _usedParentPipe = true;
                }
                else
                {
                    _usedParentPipe = false;
                    pipe = _pipePool.Take();
                }

                try
                {
                    pipe.Reset();
                }
                catch
                {
                    // The pipe was newly created. Whatever.
                }

                return pipe;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    var pipe = GetPipe();

                    base.Dispose(true);

                    var parent = (PoolableChannelReader)Parent;

                    if (_usedParentPipe)
                        parent.PipePool!.Return(pipe);
                    else
                        _pipePool.Return(pipe);

                    parent.PooledChannels.Return(this);
                }
                else
                {
                    base.Dispose(false);
                }
            }

            protected override async ValueTask DisposeAsyncCore()
            {
                var pipe = GetPipe();

                await base.DisposeAsyncCore().ConfigureAwait(false);

                var parent = (PoolableChannelReader)Parent;

                if (_usedParentPipe)
                    parent.PipePool!.Return(pipe);
                else
                    _pipePool.Return(pipe);

                parent.PooledChannels.Return(this);
            }
        }
    }
}
