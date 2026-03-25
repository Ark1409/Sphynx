// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Sphynx.Storage;
using Version = Sphynx.Core.Version;

namespace Sphynx.Network.Transport
{
    public class PoolingChannelReader : SphynxChannelReader
    {
        /// <summary>
        /// The currently supported protocol version.
        /// </summary>
        public new static Version ProtocolVersion => PoolableChannel.ProtocolVersion;

        protected readonly IObjectPool<PoolableChannel> PooledChannels;

        public PoolingChannelReader(Stream stream, Action<Channel> onChannelOpened, bool ownsStream = false)
            : this(stream, onChannelOpened, new ObjectPool<PoolableChannel>(), ownsStream)
        {
        }

        public PoolingChannelReader(Stream stream, Action<Channel> onChannelOpened, int poolSize, bool ownsStream = false)
            : this(stream, onChannelOpened, new ObjectPool<PoolableChannel>(poolSize), ownsStream)
        {
        }

        protected PoolingChannelReader(Stream stream, Action<Channel> onChannelOpened, IObjectPool<PoolableChannel> pool, bool ownsStream = false)
            : base(stream, onChannelOpened, ownsStream)
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

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                while (PooledChannels.TryTake(out var channel))
                    Debug.Assert(channel.IsDisposed);
            }
        }

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync().ConfigureAwait(false);

            while (PooledChannels.TryTake(out var channel))
                Debug.Assert(channel.IsDisposed);
        }

        protected sealed class PoolableChannel : V001Channel
        {
            public PoolableChannel(PoolingChannelReader reader, long channelId) : base(reader, channelId)
            {
            }

            public void Reset(long? newChannelId = null)
            {
                if (!IsDisposed)
                    // Since disposing requires potentially draining the reader's buffer,
                    // it's possible that calling Dispose() here would block, which is behaviour we
                    // probably want to avoid.
                    throw new InvalidOperationException($"{GetType().Name} ({nameof(ChannelId)}: {ChannelId}) must be disposed before resetting");

                var frameChannel = (PoolableFrameChannel)FrameChannel;
                frameChannel.Reset();

                if (newChannelId != null)
                    ChannelId = newChannelId.Value;

                FramesRead = 0;
                BytesRead = 0;

                DisposeException = null;
                IsDisposed = false;
            }

            protected override Channel<PooledChannelFrame> CreateFrameChannel() => new PoolableFrameChannel(base.CreateFrameChannel());

            protected override void Dispose(bool disposing)
            {
                if (IsDisposed)
                    return;

                base.Dispose(disposing);

                if (disposing)
                {
                    Interlocked.MemoryBarrier();
                    ((PoolableFrameChannel)FrameChannel).Dispose();
                    // Interlocked.MemoryBarrier();
                    ((PoolingChannelReader)Reader).PooledChannels.Return(this);
                }
            }

            public override async ValueTask DisposeAsync()
            {
                if (IsDisposed)
                    return;

                await base.DisposeAsync().ConfigureAwait(false);

                await ((PoolableFrameChannel)FrameChannel).DisposeAsync().ConfigureAwait(false);
                // Interlocked.MemoryBarrier();
                ((PoolingChannelReader)Reader).PooledChannels.Return(this);
            }

            #region Custom Poolable Channel<T> Implementation

            private class PoolableFrameChannel : Channel<PooledChannelFrame>, IAsyncDisposable, IDisposable
            {
                private static readonly ObjectPool<CancellationTokenSource> _ctsPool = new();

                private TaskCompletionSource _tcs = new();
                private CancellationTokenSource _cts = new();
                private int _itemCount;
                private volatile bool _disposed;

                public PoolableFrameChannel(Channel<PooledChannelFrame> channel)
                {
                    base.Writer = new PoolableWriter(this, channel.Writer);
                    base.Reader = new PoolableReader(this, channel.Reader);
                }

                public void Reset()
                {
                    Dispose();
                    _disposed = false;

                    _tcs = new TaskCompletionSource();
                    _cts = new CancellationTokenSource();
                }

                public async ValueTask DisposeAsync()
                {
                    if (_disposed)
                        return;

                    _tcs.TrySetException(new ChannelClosedException());
                    await _cts.CancelAsync().ConfigureAwait(false);

                    // Drain all the currently pending items from the reader.
                    // If for some reason there are enqueued items that cannot be pulled out,
                    // wait until they can be.
                    while (Volatile.Read(ref _itemCount) > 0)
                    {
                        if (Reader.TryRead(out var item))
                            item.Dispose();
                        else
                            await Reader.WaitToReadAsync(CancellationToken.None).ConfigureAwait(false);
                    }

                    _disposed = true;

                    Debug.Assert(_itemCount == 0);
                }

                public void Dispose()
                {
                    if (_disposed)
                        return;

                    _tcs.TrySetException(new ChannelClosedException());
                    _cts.Cancel();

                    // Drain all the currently pending items from the reader.
                    // If for some reason there are enqueued items that cannot be pulled out,
                    // wait until they can be.
                    while (Volatile.Read(ref _itemCount) > 0)
                    {
                        if (Reader.TryRead(out var item))
                            item.Dispose();
                        else
                            Reader.WaitToReadAsync(CancellationToken.None).Preserve().GetAwaiter().GetResult();
                    }

                    _disposed = true;

                    Debug.Assert(_itemCount == 0);
                }

                private class PoolableWriter : ChannelWriter<PooledChannelFrame>
                {
                    private readonly PoolableFrameChannel _channel;
                    private readonly ChannelWriter<PooledChannelFrame> _writer;

                    public PoolableWriter(PoolableFrameChannel channel, ChannelWriter<PooledChannelFrame> writer)
                    {
                        _writer = writer;
                        _channel = channel;
                    }

                    public override bool TryComplete(Exception? error = null)
                    {
                        if (error == null)
                        {
                            if (!_channel._tcs.TrySetResult())
                                return false;
                        }
                        else
                        {
                            if (!_channel._tcs.TrySetException(error))
                                return false;
                        }

                        _channel._cts.Cancel();
                        return true;
                    }

                    public override bool TryWrite(PooledChannelFrame item)
                    {
                        if (_channel._tcs.Task.IsCompleted)
                            return false;

                        if (_writer.TryWrite(item))
                        {
                            Interlocked.Increment(ref _channel._itemCount);
                            return true;
                        }

                        return false;
                    }

                    public override ValueTask<bool> WaitToWriteAsync(CancellationToken cancellationToken = default)
                    {
                        if (_channel._tcs.Task.IsCompleted)
                            return ValueTask.FromResult(false);

                        if (cancellationToken.IsCancellationRequested)
                            return ValueTask.FromCanceled<bool>(cancellationToken);

                        return Core(cancellationToken);

                        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
                        async ValueTask<bool> Core(CancellationToken token)
                        {
                            if (!_ctsPool.TryTake(out var cts))
                                cts = new CancellationTokenSource();

                            var reg = token.UnsafeRegister(s => ((CancellationTokenSource)s!).Cancel(), cts);
                            var completeReg = _channel._cts.Token.UnsafeRegister(s => ((CancellationTokenSource)s!).Cancel(), cts);

                            try
                            {
                                return await _writer.WaitToWriteAsync(cts.Token).ConfigureAwait(false);
                            }
                            catch (OperationCanceledException) when (!token.IsCancellationRequested)
                            {
                                // This implies it was the completion token that cancelled us, so we don't want throw,
                                // just return false to indicate that we cannot write
                                return false;
                            }
                            finally
                            {
                                await reg.DisposeAsync().ConfigureAwait(false);
                                await completeReg.DisposeAsync().ConfigureAwait(false);

                                if (cts.TryReset())
                                    _ctsPool.Return(cts);
                            }
                        }
                    }
                }

                private class PoolableReader : ChannelReader<PooledChannelFrame>
                {
                    private readonly PoolableFrameChannel _channel;
                    private readonly ChannelReader<PooledChannelFrame> _reader;

                    public PoolableReader(PoolableFrameChannel channel, ChannelReader<PooledChannelFrame> reader)
                    {
                        _reader = reader;
                        _channel = channel;
                    }

                    public override Task Completion => _channel._tcs.Task;
                    public override int Count => _reader.Count;
                    public override bool CanCount => _reader.CanCount;
                    public override bool CanPeek => _reader.CanPeek;

                    public override bool TryRead(out PooledChannelFrame item)
                    {
                        if (_reader.TryRead(out item))
                        {
                            Interlocked.Decrement(ref _channel._itemCount);
                            return true;
                        }

                        return false;
                    }

                    public override ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            return ValueTask.FromCanceled<bool>(cancellationToken);

                        if (Volatile.Read(ref _channel._itemCount) > 0)
                            return ValueTask.FromResult(true);

                        if (Completion.IsCompleted)
                            return ValueTask.FromResult(false);

                        return Core(cancellationToken);

                        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
                        async ValueTask<bool> Core(CancellationToken token)
                        {
                            if (!_ctsPool.TryTake(out var cts))
                                cts = new CancellationTokenSource();

                            var reg = token.UnsafeRegister(s => ((CancellationTokenSource)s!).Cancel(), cts);
                            var completeReg = _channel._cts.Token.UnsafeRegister(s => ((CancellationTokenSource)s!).Cancel(), cts);

                            try
                            {
                                return await _reader.WaitToReadAsync(cts.Token).ConfigureAwait(false);
                            }
                            catch (OperationCanceledException) when (!token.IsCancellationRequested)
                            {
                                // This implies it was the completion token that cancelled us, so we don't want throw,
                                // just return whether there are more items
                                return Volatile.Read(ref _channel._itemCount) > 0;
                            }
                            finally
                            {
                                await reg.DisposeAsync().ConfigureAwait(false);
                                await completeReg.DisposeAsync().ConfigureAwait(false);

                                if (cts.TryReset())
                                    _ctsPool.Return(cts);
                            }
                        }
                    }
                }
            }

            #endregion
        }
    }
}
