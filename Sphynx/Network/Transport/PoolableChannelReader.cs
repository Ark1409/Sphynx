// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Sphynx.Storage;

namespace Sphynx.Network.Transport
{
    public class PoolableChannelReader : SphynxChannelReader
    {
        protected readonly IObjectPool<PoolableChannel> PooledChannels;

        public PoolableChannelReader(Stream stream, bool ownsStream = false)
            : this(stream, new ObjectPool<PoolableChannel>(), ownsStream)
        {
        }

        public PoolableChannelReader(Stream stream, int poolSize, bool ownsStream = false)
            : this(stream, new ObjectPool<PoolableChannel>(poolSize), ownsStream)
        {
        }

        protected PoolableChannelReader(Stream stream, IObjectPool<PoolableChannel> pool, bool ownsStream = false)
            : base(stream, ownsStream)
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
            // Don't need to explicitly pool the stream; disposing the stream does nothing.

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

                var frameChannel = (PoolableFrameChannel)FrameChannel;
                frameChannel.Reset();

                if (channelId.HasValue)
                    ChannelId = channelId.Value;

                FramesRead = 0;
                BytesRead = 0;

                CloseException = null;
            }

            protected override Channel<PooledDataFrame> CreateFrameChannel() => new PoolableFrameChannel(base.CreateFrameChannel());

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);

                if (disposing)
                {
                    ((PoolableFrameChannel)FrameChannel).Dispose();
                    ((PoolableChannelReader)Parent).PooledChannels.Return(this);
                }
            }

            protected override async ValueTask DisposeAsyncCore()
            {
                await base.DisposeAsyncCore().ConfigureAwait(false);

                await ((PoolableFrameChannel)FrameChannel).DisposeAsync().ConfigureAwait(false);
                ((PoolableChannelReader)Parent).PooledChannels.Return(this);
            }

            #region Custom Poolable Channel<T> Implementation

            private class PoolableFrameChannel : Channel<PooledDataFrame>, IAsyncDisposable, IDisposable
            {
                private static readonly ObjectPool<CancellationTokenSource> _ctsPool = new(Environment.ProcessorCount * 4);
                private static readonly Exception _successSentinel = new();

                private volatile Exception? _doneWriting;
                private CancellationTokenSource _doneWritingCts = new();
                private volatile int _itemCount;

                private object SyncLock => _doneWritingCts;

                public PoolableFrameChannel(Channel<PooledDataFrame> channel)
                {
                    base.Writer = new PoolableWriter(this, channel.Writer);
                    base.Reader = new PoolableReader(this, channel.Reader);
                }

                public void Reset()
                {
                    if (_doneWriting == null)
                        // Since disposing requires potentially draining the reader's buffer,
                        // it's possible that calling Dispose() here would block, which is behaviour we
                        // probably want to avoid.
                        throw new InvalidOperationException($"{GetType().Name} must be disposed before resetting");

                    _doneWritingCts = new CancellationTokenSource();
                    _doneWriting = null;
                }

                public async ValueTask DisposeAsync()
                {
                    if (_doneWriting == null)
                        Writer.TryComplete(new ObjectDisposedException(GetType().Name));

                    // Drain all the currently pending items from the reader.
                    // If for some reason there are enqueued items that cannot be pulled out,
                    // wait until they can be.
                    while (_itemCount > 0)
                    {
                        if (Reader.TryRead(out var item))
                            item.Dispose();
                        else
                            await Reader.WaitToReadAsync().ConfigureAwait(false);
                    }

                    Debug.Assert(_itemCount == 0);
                }

                public void Dispose()
                {
                    if (_doneWriting == null)
                        Writer.TryComplete(new ObjectDisposedException(GetType().Name));

                    // Drain all the currently pending items from the reader.
                    // If for some reason there are enqueued items that cannot be pulled out,
                    // wait until they can be.
                    while (_itemCount > 0)
                    {
                        if (Reader.TryRead(out var item))
                            item.Dispose();
                        else
                            Reader.WaitToReadAsync().Preserve().GetAwaiter().GetResult();
                    }

                    Debug.Assert(_itemCount == 0);
                }

                private class PoolableWriter : ChannelWriter<PooledDataFrame>
                {
                    private readonly PoolableFrameChannel _channel;
                    private readonly ChannelWriter<PooledDataFrame> _writer;

                    public PoolableWriter(PoolableFrameChannel channel, ChannelWriter<PooledDataFrame> writer)
                    {
                        _writer = writer;
                        _channel = channel;
                    }

                    public override bool TryComplete(Exception? error = null)
                    {
                        if (_channel._doneWriting != null)
                            return false;

                        lock (_channel.SyncLock)
                        {
                            if (_channel._doneWriting != null)
                                return false;

                            // Need the write here, so readers can check the item count once _doneWriting is non-null
                            _channel._itemCount = _channel._itemCount;
                            _channel._doneWriting = error ?? _successSentinel;
                        }

                        _channel._doneWritingCts.Cancel();
                        return true;
                    }

                    public override bool TryWrite(PooledDataFrame item)
                    {
                        if (_channel._doneWriting != null)
                            return false;

                        lock (_channel.SyncLock)
                        {
                            if (_channel._doneWriting != null)
                                return false;

                            if (_writer.TryWrite(item))
                            {
                                // ReSharper disable once NonAtomicCompoundOperator : We're protected by the lock
                                _channel._itemCount++;
                                return true;
                            }
                        }

                        return false;
                    }

                    public override ValueTask<bool> WaitToWriteAsync(CancellationToken cancellationToken = default)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            return ValueTask.FromCanceled<bool>(cancellationToken);

                        if (_channel._doneWriting != null)
                            return ValueTask.FromResult(false);

                        return Core(cancellationToken);

                        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
                        async ValueTask<bool> Core(CancellationToken token)
                        {
                            if (!_ctsPool.TryTake(out var cts))
                                cts = new CancellationTokenSource();

                            var reg = token.UnsafeRegister(s => ((CancellationTokenSource)s!).Cancel(), cts);
                            var writingReg = _channel._doneWritingCts.Token.UnsafeRegister(s => ((CancellationTokenSource)s!).Cancel(), cts);

                            try
                            {
                                return await _writer.WaitToWriteAsync(cts.Token).ConfigureAwait(false);
                            }
                            catch (OperationCanceledException) when (_channel._doneWriting != null)
                            {
                                // Imitate the same behaviour as normal channels
                                if (_channel._doneWriting != _successSentinel)
                                    throw _channel._doneWriting;

                                return false;
                            }
                            finally
                            {
                                await reg.DisposeAsync().ConfigureAwait(false);
                                await writingReg.DisposeAsync().ConfigureAwait(false);

                                if (cts.TryReset())
                                    _ctsPool.Return(cts);
                            }
                        }
                    }
                }

                private class PoolableReader : ChannelReader<PooledDataFrame>
                {
                    private readonly PoolableFrameChannel _channel;
                    private readonly ChannelReader<PooledDataFrame> _reader;

                    public PoolableReader(PoolableFrameChannel channel, ChannelReader<PooledDataFrame> reader)
                    {
                        _reader = reader;
                        _channel = channel;
                    }

                    // We don't use it anyway. We can save an allocation on reset here.
                    public override Task Completion => throw new NotSupportedException();
                    public override int Count => _reader.Count;
                    public override bool CanCount => _reader.CanCount;
                    public override bool CanPeek => _reader.CanPeek;

                    public override bool TryRead(out PooledDataFrame item)
                    {
                        // We are done for good
                        if (_channel._doneWriting != null && _channel._itemCount == 0)
                        {
                            item = default;
                            return false;
                        }

                        lock (_channel.SyncLock)
                        {
                            if (_channel._doneWriting != null && _channel._itemCount == 0)
                            {
                                item = default;
                                return false;
                            }

                            if (_reader.TryRead(out item))
                            {
                                // ReSharper disable once NonAtomicCompoundOperator : We're protected by the lock
                                _channel._itemCount--;
                                return true;
                            }
                        }

                        return false;
                    }

                    public override ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            return ValueTask.FromCanceled<bool>(cancellationToken);

                        if (_channel._itemCount > 0)
                            return ValueTask.FromResult(true);

                        if (_channel._doneWriting != null)
                            return ValueTask.FromResult(false);

                        return Core(cancellationToken);

                        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
                        async ValueTask<bool> Core(CancellationToken token)
                        {
                            if (!_ctsPool.TryTake(out var cts))
                                cts = new CancellationTokenSource();

                            var reg = token.UnsafeRegister(s => ((CancellationTokenSource)s!).Cancel(), cts);
                            var writeReg = _channel._doneWritingCts.Token.UnsafeRegister(s => ((CancellationTokenSource)s!).Cancel(), cts);

                            try
                            {
                                return await _reader.WaitToReadAsync(cts.Token).ConfigureAwait(false);
                            }
                            catch (OperationCanceledException) when (_channel._doneWriting != null)
                            {
                                // Imitate the same behaviour as normal channels
                                if (_channel._doneWriting != _successSentinel)
                                    throw _channel._doneWriting;

                                // Since it was the writing token that cancelled us, don't throw, just return
                                // whether we'll be able to dequeue more items.
                                return _channel._itemCount > 0;
                            }
                            finally
                            {
                                await reg.DisposeAsync().ConfigureAwait(false);
                                await writeReg.DisposeAsync().ConfigureAwait(false);

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
