using System.Diagnostics;
using System.IO.Pipelines;
using Sphynx.Storage;

namespace Sphynx.Network.Transport
{
    public class PoolableChannelReader : DefaultChannelReader
    {
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

        protected PoolableChannelReader(Stream stream, IObjectPool<PoolableChannel> pool, IObjectPool<Pipe>? pipePool = null, bool ownsStream = false)
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

        public virtual void Reset(Stream stream, bool? ownsStream = null)
        {
            if (!IsDisposed)
                // Since disposing requires potentially draining the reader's buffer,
                // it's possible that calling Dispose() here would block, which is behaviour we
                // probably want to avoid.
                throw new InvalidOperationException($"{GetType().Name} must be disposed before resetting");

            RunTask = null;
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

        protected class PoolableChannel : DefaultChannel
        {
            private static readonly ObjectPool<Pipe> _pipePool = new(() => new Pipe(ChannelPipeReader.DefaultPipeOptions));
            private bool _usedParentPipe;

            protected override PoolableChannelReader Parent { get; }

            public PoolableChannel(PoolableChannelReader parent, ChannelId channelId) : base(parent, channelId)
            {
                Parent = parent;
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

                IsDisposeReserved = 0;
                CloseException = null;
            }

            protected override Pipe NewPipe()
            {
                if (Parent.PipePool?.TryTake(out var pipe) ?? false)
                {
                    _usedParentPipe = true;
                }
                else
                {
                    pipe = _pipePool.Take();
                    _usedParentPipe = false;
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

                    if (_usedParentPipe)
                        Parent.PipePool!.Return(pipe);
                    else
                        _pipePool.Return(pipe);

                    Parent.PooledChannels.Return(this);
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

                if (_usedParentPipe)
                    Parent.PipePool!.Return(pipe);
                else
                    _pipePool.Return(pipe);

                Parent.PooledChannels.Return(this);
            }

        }
    }
}
