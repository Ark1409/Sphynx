// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using Sphynx.Utils;

namespace Sphynx.Network.Transport
{
    public delegate ValueTask ChannelReleasedHandler(object? state, ChannelId channelId, byte releasedFlags);
    public delegate ValueTask ChannelReleasingHandler(object? state, ChannelId channelId, byte releasingFlags);
    public delegate ValueTask ChannelFrameDroppedHandler(object? state, SphynxFrameHeader header);

    public class DefaultChannelReader : SphynxChannelReader
    {
        private object? _onChannelOpenedState;
        private volatile ChannelOpenedHandler? _onChannelOpened;

        private object? _onChannelReleasedState;
        private volatile ChannelReleasedHandler? _onChannelReleased;

        private object? _onChannelFrameDroppedState;
        private volatile ChannelFrameDroppedHandler? _onChannelFrameDropped;

        private object? _onChannelReleasingState;
        private volatile ChannelReleasingHandler? _onChannelReleasing;

        /// <summary>
        /// The current number of open channels.
        /// </summary>
        public int OpenChannelCount => OpenChannels.Count;

        protected readonly ConcurrentDictionary<ChannelId, DefaultChannel> OpenChannels = new();

        protected bool OwnsStream;
        protected Stream Stream;

        public DefaultChannelReader(Stream stream, bool ownsStream = false)
        {
            Stream = stream;
            OwnsStream = ownsStream;
        }

        /// <summary>
        /// Callback for when a new channel is opened.
        /// </summary>
        public override void OnChannelOpened(ChannelOpenedHandler callback, object? state = null)
        {
            _onChannelOpenedState = state;
            _onChannelOpened = callback;
        }

        /// <summary>
        /// Callback for when a <see cref="SphynxFrameType.CHANNEL_RELEASE"/> is received from the remote peer.
        /// </summary>
        /// <seealso cref="SphynxFrameType.CHANNEL_RELEASE"/>
        public void OnChannelReleased(ChannelReleasedHandler callback, object? state = null)
        {
            _onChannelReleasedState = state;
            _onChannelReleased = callback;
        }

        /// <summary>
        /// Registers a callback that is called when an open channel is <see cref="SphynxChannelReader.Channel.Dispose()">disposed</see>
        /// before the remote peer finishes sending all its data.
        /// </summary>
        /// <remarks>This is when a <see cref="SphynxFrameType.CHANNEL_RELEASE"/> should be sent to the remote peer.</remarks>
        public void OnChannelReleasing(ChannelReleasingHandler callback, object? state = null)
        {
            _onChannelReleasingState = state;
            _onChannelReleasing = callback;
        }

        /// <summary>
        /// Registers a callback for when an incoming channel frame is dropped. This can occur if <see cref="SphynxChannelReader.MaxOpenChannels">too many</see>
        /// channels are open, or invalid data is received. Under normal circumstances, existing channels should never lose any data.
        /// </summary>
        public void OnChannelFrameDropped(ChannelFrameDroppedHandler callback, object? state = null)
        {
            _onChannelFrameDroppedState = state;
            _onChannelFrameDropped = callback;
        }

        private static readonly ChannelClosedException _channelAbortedException = new("The channel was aborted by the remote peer");
        private static readonly EndOfStreamException _streamEndException = new();

        protected sealed override async Task ReadChannelsAsync(CancellationToken cancellationToken)
        {
            // NOTE: We don't want to split this into multiple async methods, as that would cause unnecessary allocations.
            try
            {
                while (true)
                {
                    var frameHeader = await SphynxFrameHeader.ReceiveAsync(Stream, skipInvalid: false, cancellationToken).ConfigureAwait(false);
                    bool channelExists = OpenChannels.TryGetValue(frameHeader.ChannelId, out var channel);

                    if (!frameHeader.IsValid())
                    {
                        ChannelId? channelId = frameHeader.Version == SphynxFrameHeader.ProtocolVersion ? frameHeader.ChannelId : null;
                        throw new SphynxProtocolException(channelId, $"Invalid frame received {frameHeader.FrameType}");
                    }

                    Debug.Assert(Enum.IsDefined(frameHeader.FrameType));

                    if (frameHeader.FrameType == SphynxFrameType.CHANNEL_ABORT)
                    {
                        if (channelExists)
                            InvokeChannelDispose(channel!, _channelAbortedException);

                        Debug.Assert(frameHeader.FrameSize == 0);
                        continue;
                    }

                    if (frameHeader.FrameType == SphynxFrameType.CHANNEL_RELEASE)
                    {
                        InvokeChannelReleased(frameHeader.ChannelId, frameHeader.Flags);

                        Debug.Assert(frameHeader.FrameSize == 0);
                        continue;
                    }

                    Debug.Assert(frameHeader.FrameType == SphynxFrameType.CHANNEL_DATA);

                    if (frameHeader.HasFlags(ChannelDataFlags.CHANNEL_START))
                    {
                        if (channelExists)
                            throw new SphynxProtocolException(channel!.ChannelId, "A duplicate channel with the same ID was opened");

                        var onChannelOpened = _onChannelOpened;
                        object? onChannelOpenedState = _onChannelOpenedState;

                        if (onChannelOpened == null)
                        {
                            InvokeChannelReleasing(frameHeader.ChannelId);
                            InvokeChannelFrameDropped(in frameHeader);

                            await Stream.SkipAsync(frameHeader.FrameSize, cancellationToken).ConfigureAwait(false);
                            continue;
                        }

                        // If we're going to have to store this channel
                        if (!frameHeader.HasFlags(ChannelDataFlags.CHANNEL_END))
                        {
                            // Just drop it if we're at our max
                            if (OpenChannelCount >= MaxOpenChannels)
                            {
                                InvokeChannelReleasing(frameHeader.ChannelId);
                                InvokeChannelFrameDropped(in frameHeader);

                                await Stream.SkipAsync(frameHeader.FrameSize, cancellationToken).ConfigureAwait(false);
                                continue;
                            }

                            channel = OpenChannels.GetOrAdd(frameHeader.ChannelId, static (id, reader) => reader.NewChannel(id), this);
                        }
                        else
                        {
                            channel = NewChannel(frameHeader.ChannelId);
                        }

                        channelExists = true;
                        bool invoked = InvokeChannelOpened(channel, onChannelOpened, onChannelOpenedState);
                        Debug.Assert(invoked);
                    }
                    else
                    {
                        if (!channelExists)
                        {
                            InvokeChannelFrameDropped(in frameHeader);

                            await Stream.SkipAsync(frameHeader.FrameSize, cancellationToken).ConfigureAwait(false);
                            continue;
                        }
                    }

                    Debug.Assert(channelExists && channel != null);

                    // Now read the actual channel data
                    await using var channelWriter = await channel.RentChannelWriterAsync(cancellationToken).ConfigureAwait(false);

                    if (channelWriter.IsCompleted)
                    {
                        InvokeChannelFrameDropped(in frameHeader);

                        await Stream.SkipAsync(frameHeader.FrameSize, cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    int bytesRead = 0;

                    while (bytesRead < frameHeader.FrameSize)
                    {
                        int bytesLeft = frameHeader.FrameSize - bytesRead;
                        var memory = channelWriter.GetMemory();
                        int readSize = Math.Min(memory.Length, bytesLeft);
                        memory = memory[..readSize];

                        // It could technically be the case that this unnecessarily stalls the reader's Dispose(Async) if we end up blocking for data
                        // but 1. even if we were to cancel eagerly, we would have to skip frameHeader.FrameSize bytes from the stream anyway,
                        // and 2. once we've canceled, there's no way to know how many bytes were already read from the stream as well
                        int readCount = await Stream.ReadAtLeastAsync(memory, memory.Length, false, cancellationToken).ConfigureAwait(false);
                        channelWriter.Advance(readCount);

                        if (readCount < memory.Length)
                        {
                            InvokeChannelFrameDropped(in frameHeader);

                            await channelWriter.CompleteAsync(_streamEndException).ConfigureAwait(false);
                            throw _streamEndException;
                        }

                        bytesRead += readCount;
                    }

                    try
                    {
                        await channelWriter.FlushAsync(cancellationToken).ConfigureAwait(false);

                        if (frameHeader.HasFlags(ChannelDataFlags.CHANNEL_END))
                            await channelWriter.CompleteAsync().ConfigureAwait(false);
                    }
                    catch
                    {
                        InvokeChannelFrameDropped(in frameHeader);
                    }
                }
            }
            catch (Exception ex)
            {
                await CompleteChannels(ex).ConfigureAwait(false);
                throw;
            }
        }

        private async ValueTask CompleteChannels(Exception? ex)
        {
            foreach (var (_, channel) in OpenChannels)
            {
                // They could be in the middle of disposing themselves
                if (channel.IsDisposed)
                    continue;

                await using var writer = await channel.RentChannelWriterAsync().ConfigureAwait(false);

                if (!writer.IsCompleted)
                    await writer.CompleteAsync(ex);
            }
        }

        protected virtual DefaultChannel NewChannel(ChannelId channelId) => new(this, channelId);

        private bool InvokeChannelOpened(Channel channel, ChannelOpenedHandler? callback = null, object? callbackState = null)
        {
            callback ??= _onChannelOpened;
            callbackState ??= _onChannelOpenedState;

            if (callback == null)
                return false;

            return ThreadPoolHelper.QueueUserWorkItem(static async void (state) =>
            {
                try
                {
                    await state.callback.Invoke(state.callbackState, state.channel).ConfigureAwait(false);
                }
                catch
                {
                    // ignore
                }
            }, (callback, callbackState, channel));
        }

        private void InvokeChannelFrameDropped(in SphynxFrameHeader header)
        {
            var callback = _onChannelFrameDropped;
            object? callbackState = _onChannelFrameDroppedState;

            if (callback == null)
                return;

            ThreadPoolHelper.QueueUserWorkItem(static async void (state) =>
            {
                try
                {
                    await state.callback.Invoke(state.callbackState, state.header).ConfigureAwait(false);
                }
                catch
                {
                    // ignore
                }
            }, (callback, callbackState, header));
        }

        protected void InvokeChannelReleased(ChannelId channelId, byte releasedFlags)
        {
            var callback = _onChannelReleased;
            object? callbackState = _onChannelReleasedState;

            if (callback == null)
                return;

            ThreadPoolHelper.QueueUserWorkItem(static async void (state) =>
            {
                try
                {
                    await state.callback.Invoke(state.callbackState, state.channelId, state.releasedFlags).ConfigureAwait(false);
                }
                catch
                {
                    // ignore
                }
            }, (callback, callbackState, channelId, releasedFlags));
        }

        protected void InvokeChannelReleasing(ChannelId channelId, byte releasingFlags = ChannelReleaseFlags.CHANNEL_REJECTED)
        {
            var callback = _onChannelReleasing;
            object? callbackState = _onChannelReleasingState;

            if (callback == null)
                return;

            ThreadPoolHelper.QueueUserWorkItem(static async void (state) =>
            {
                try
                {
                    await state.callback.Invoke(state.callbackState, state.channelId, state.releasingFlags).ConfigureAwait(false);
                }
                catch
                {
                    // ignore
                }
            }, (callback, callbackState, channelId, releasingFlags));
        }

        protected virtual void InvokeChannelDispose(DefaultChannel channel, Exception? disposeException)
        {
            OpenChannels.TryRemove(new KeyValuePair<ChannelId, DefaultChannel>(channel.ChannelId, channel));

            if (channel.IsDisposed)
                return;

            channel.ForceClose(disposeException);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                UnregisterCallbacks();
                _ = DisposeChannels(async: false);
                OpenChannels.Clear();

                if (OwnsStream)
                {
                    try
                    {
                        Stream.Dispose();
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }
        }

        protected override async ValueTask DisposeAsyncCore()
        {
            UnregisterCallbacks();
            await DisposeChannels(async: true).ConfigureAwait(false);
            OpenChannels.Clear();

            if (OwnsStream)
            {
                try
                {
                    await Stream.DisposeAsync().ConfigureAwait(false);
                }
                catch
                {
                    // ignore
                }
            }
        }

        private void UnregisterCallbacks()
        {
            _onChannelOpenedState = null;
            _onChannelOpened = null;
            _onChannelReleasingState = null;
            _onChannelReleasing = null;
            _onChannelFrameDroppedState = null;
            _onChannelFrameDropped = null;
            _onChannelReleasedState = null;
            _onChannelReleased = null;
        }

        private async ValueTask DisposeChannels(bool async = false)
        {
            if (OpenChannelCount == 0)
                return;

            var closeException = GetDisposedException();

            foreach (var (_, channel) in OpenChannels)
            {
                try
                {
                    if (async)
                        await channel.DisposeAsync(closeException).ConfigureAwait(false);
                    else
                        channel.Dispose(closeException);
                }
                catch
                {
                    // ignore
                }
            }

            Debug.Assert(OpenChannelCount == 0);
        }

        protected internal class DefaultChannel : Channel
        {
            protected internal readonly struct ChannelWriterRent : IDisposable, IAsyncDisposable, IBufferWriter<byte>
            {
                public bool IsCompleted => _channel._pipeWriterCompleted;

                private readonly DefaultChannel _channel;
                private readonly SemaphoreSlim? _writerLock;
                private readonly PipeWriter? _writer;

                private PipeWriter Writer
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get
                    {
                        if (IsCompleted)
                            ThrowCompletedException(in this);

                        return _writer!;

                        [DoesNotReturn]
                        void ThrowCompletedException(in ChannelWriterRent thisRef) => throw thisRef._channel.CloseException ?? CloseSentinel;
                    }
                }

                internal ChannelWriterRent(DefaultChannel channel)
                {
                    _channel = channel;
                    Debug.Assert(IsCompleted);
                }

                internal ChannelWriterRent(DefaultChannel channel, SemaphoreSlim writerLock)
                {
                    _channel = channel;
                    _writer = _channel.GetPipe().Writer;
                    _writerLock = writerLock;

                    Debug.Assert(writerLock.CurrentCount == 0);
                }

                public void Advance(int count) => Writer.Advance(count);
                public Memory<byte> GetMemory(int sizeHint = 0) => Writer.GetMemory(sizeHint);
                public Span<byte> GetSpan(int sizeHint = 0) => Writer.GetSpan(sizeHint);
                public ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default) => Writer.FlushAsync(cancellationToken);

                public void Complete(Exception? ex = null)
                {
                    Writer.Complete(ex);
                    Writer.CancelPendingFlush();

                    _channel.Parent.OpenChannels.TryRemove(new KeyValuePair<ChannelId, DefaultChannel>(_channel.ChannelId, _channel));
                    // We still need to invoke this even if the channel wasn't stored
                    _channel.Parent.InvokeChannelReleasing(_channel.ChannelId,
                        ex == null ? ChannelReleaseFlags.NONE : ChannelReleaseFlags.CHANNEL_REJECTED);

                    _channel._pipeWriterCompleted = true;
                }

                public async ValueTask CompleteAsync(Exception? ex = null)
                {
                    await Writer.CompleteAsync(ex).ConfigureAwait(false);
                    Writer.CancelPendingFlush();

                    _channel.Parent.OpenChannels.TryRemove(new KeyValuePair<ChannelId, DefaultChannel>(_channel.ChannelId, _channel));
                    // We still need to invoke this even if the channel wasn't stored
                    _channel.Parent.InvokeChannelReleasing(_channel.ChannelId,
                        ex == null ? ChannelReleaseFlags.NONE : ChannelReleaseFlags.CHANNEL_REJECTED);

                    _channel._pipeWriterCompleted = true;
                }

                public void Dispose()
                {
                    try
                    {
                        if (IsCompleted)
                            return;

                        var flushTask = FlushAsync();
                        var result = flushTask.IsCompletedSuccessfully ? flushTask.Result : flushTask.Preserve().GetAwaiter().GetResult();

                        if (result.IsCompleted)
                            Complete(_channel.CloseException);
                    }
                    catch
                    {
                        // If we have problems flushing, we don't really care; it's likely the underlying pipe
                        // is somehow "broken" anyway
                    }
                    finally
                    {
                        _writerLock?.Release();
                    }
                }

                public ValueTask DisposeAsync()
                {
                    if (IsCompleted)
                    {
                        _writerLock?.Release();
                        return ValueTask.CompletedTask;
                    }

                    return Core(this);

                    static async ValueTask Core(ChannelWriterRent thisRef)
                    {
                        Debug.Assert(thisRef._writer != null && thisRef._writerLock != null);

                        try
                        {
                            var result = await thisRef.FlushAsync().ConfigureAwait(false);

                            if (result.IsCompleted)
                                await thisRef.CompleteAsync(thisRef._channel.CloseException).ConfigureAwait(false);
                        }
                        catch
                        {
                            // If we have problems flushing, we don't really care; it's likely the underlying pipe
                            // is somehow "broken" anyway
                        }
                        finally
                        {
                            thisRef._writerLock.Release();
                        }
                    }
                }
            }

            private volatile Pipe? _pipe;
            private readonly SemaphoreSlim _pipeWriterLock = new(1, 1);
            private volatile bool _pipeWriterCompleted;

            /// <summary>
            /// The <see cref="PipeReader"/> for incoming channel data.
            /// </summary>
            protected PipeReader ChannelReader => GetPipe().Reader;

            protected override DefaultChannelReader Parent { get; }

            public DefaultChannel(DefaultChannelReader parent, ChannelId channelId) : base(parent, channelId)
            {
                Parent = parent;
            }

            public override int ReadAtLeast(Memory<byte> buffer, int minSize = -1, bool throwOnEnd = false)
            {
                ThrowIfDisposed();

                if (minSize == -1)
                    minSize = buffer.Length;

                if (ChannelReader.TryRead(out var result) && result.Buffer.Length >= minSize)
                    return HandleReadResult(result, buffer.Span, throwOnEnd, throwOnEnd);

                return Core(this, buffer, minSize, throwOnEnd);

                static int Core(DefaultChannel channel, Memory<byte> buffer, int size, bool throwOnEnd)
                {
                    Debug.Assert(size >= 0);

                    var readTask = channel.ReadAtLeastAsync(buffer, size, throwOnEnd);
                    return readTask.IsCompletedSuccessfully ? readTask.Result : readTask.Preserve().GetAwaiter().GetResult();
                }
            }

            public override ValueTask<int> ReadAtLeastAsync(Memory<byte> buffer, int minSize = -1, bool throwOnEnd = false,
                CancellationToken cancellationToken = default)
            {
                if (IsDisposed)
                    return ValueTask.FromException<int>(GetDisposedException());

                if (minSize == -1)
                    minSize = buffer.Length;

                if (ChannelReader.TryRead(out var result) && result.Buffer.Length >= minSize)
                    return ValueTask.FromResult(HandleReadResult(result, buffer.Span, throwOnEnd, throwOnEnd));

                return Core(this, buffer, minSize, throwOnEnd, cancellationToken);

                static async ValueTask<int> Core(DefaultChannel channel, Memory<byte> buffer, int size, bool throwOnEnd, CancellationToken token)
                {
                    Debug.Assert(size >= 0);

                    var readResult = await channel.ChannelReader.ReadAtLeastAsync(size, token).ConfigureAwait(false);
                    return channel.HandleReadResult(readResult, buffer.Span, throwOnEnd, throwOnEnd);
                }
            }

            // Read semantics copied from: https://github.com/dotnet/dotnet/blob/main/src/runtime/src/libraries/System.IO.Pipelines/src/System/IO/Pipelines/PipeReaderStream.cs

            public override int Read(Span<byte> buffer)
            {
                ThrowIfDisposed();

                var task = ChannelReader.ReadAsync();
                var result = task.IsCompletedSuccessfully ? task.Result : task.Preserve().GetAwaiter().GetResult();
                return HandleReadResult(result, buffer);
            }

            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                if (IsDisposed)
                    return ValueTask.FromException<int>(GetDisposedException());

                if (cancellationToken.IsCancellationRequested)
                    return ValueTask.FromCanceled<int>(cancellationToken);

                if (ChannelReader.TryRead(out var result))
                    return ValueTask.FromResult(HandleReadResult(result, buffer.Span));

                return Core(buffer, cancellationToken);

                async ValueTask<int> Core(Memory<byte> memory, CancellationToken token)
                {
                    var readResult = await ChannelReader.ReadAsync(token).ConfigureAwait(false);
                    return HandleReadResult(readResult, memory.Span);
                }
            }

            private int HandleReadResult(ReadResult result, Span<byte> buffer, bool throwOnCancel = false, bool throwOnComplete = false)
            {
                if (result.IsCanceled)
                    return throwOnCancel ? ThrowEndException() : 0;

                ReadOnlySequence<byte> resultBuffer = result.Buffer;
                long resultLength = resultBuffer.Length;
                SequencePosition consumed = resultBuffer.Start;

                try
                {
                    if (resultLength != 0)
                    {
                        int copyLength = (int)Math.Min(resultLength, buffer.Length);

                        ReadOnlySequence<byte> resultSlice = copyLength == resultLength ? resultBuffer : resultBuffer.Slice(0, copyLength);
                        resultSlice.CopyTo(buffer);
                        consumed = resultSlice.End;

                        BytesRead += copyLength;
                        return copyLength;
                    }

                    if (result.IsCompleted)
                        return throwOnComplete ? ThrowEndException() : 0;
                }
                finally
                {
                    ChannelReader.AdvanceTo(consumed);
                }

                // This is a buggy PipeReader implementation that returns 0 byte reads even though the PipeReader
                // isn't completed or canceled?
                return ThrowImplementationException();

                [DoesNotReturn]
                int ThrowEndException() => throw CloseException ?? CloseSentinel;

                [DoesNotReturn]
                int ThrowImplementationException() => throw new InvalidOperationException("0 byte read occured when channel was not completed");
            }

            /// <summary>
            /// Rent exclusive access to this channel's <see cref="PipeWriter"/>.
            /// </summary>
            /// <param name="cancellationToken">A cancellation token for the wait operation.</param>
            /// <returns>The rented channel writer. You should call <see cref="ChannelWriterRent.Dispose"/> once you are done.</returns>
            protected internal ChannelWriterRent RentChannelWriter(CancellationToken cancellationToken = default)
            {
                if (_pipeWriterCompleted)
                    return new ChannelWriterRent(this);

                _pipeWriterLock.Wait(cancellationToken);
                return new ChannelWriterRent(this, _pipeWriterLock);
            }

            /// <summary>
            /// Rent exclusive access to this channel's <see cref="PipeWriter"/>.
            /// </summary>
            /// <param name="cancellationToken">A cancellation token for the wait operation.</param>
            /// <returns>The rented channel writer. You should call <see cref="ChannelWriterRent.DisposeAsync"/> once you are done.</returns>
            protected internal ValueTask<ChannelWriterRent> RentChannelWriterAsync(CancellationToken cancellationToken = default)
            {
                if (_pipeWriterCompleted)
                    return ValueTask.FromResult(new ChannelWriterRent(this));

                var waitTask = _pipeWriterLock.WaitAsync(cancellationToken);

                if (waitTask.IsCompletedSuccessfully)
                    return ValueTask.FromResult(new ChannelWriterRent(this, _pipeWriterLock));

                if (waitTask.IsCanceled)
                    return ValueTask.FromCanceled<ChannelWriterRent>(cancellationToken);

                if (waitTask.IsFaulted)
                    return ValueTask.FromException<ChannelWriterRent>(waitTask.Exception!);

                return Core(waitTask);

                async ValueTask<ChannelWriterRent> Core(Task task)
                {
                    await task.ConfigureAwait(false);
                    return new ChannelWriterRent(this, _pipeWriterLock);
                }
            }

            private DefaultPipeReader? _defaultPipeReader;

            public override PipeReader AsPipeReader(bool leaveOpen = true)
            {
                _defaultPipeReader ??= new DefaultPipeReader(this, leaveOpen);
                _defaultPipeReader.LeaveOpen = leaveOpen;
                return _defaultPipeReader;
            }

            protected internal virtual bool ForceClose(Exception? closeException)
            {
                if (Interlocked.CompareExchange(ref CloseException, ToCloseException(closeException), null) == null)
                {
                    ChannelReader.CancelPendingRead();
                    return true;
                }

                return false;
            }

            protected int IsDisposeReserved;

            protected override bool TryReserveDispose(Exception? disposeException)
            {
                Interlocked.CompareExchange(ref CloseException, ToCloseException(disposeException), null);
                return Interlocked.Exchange(ref IsDisposeReserved, 1) == 0;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    Debug.Assert(CloseException != null);

                    try
                    {
                        ChannelReader.Complete(CloseException == CloseSentinel ? null : CloseException);
                        ChannelReader.CancelPendingRead();
                        GetPipe().Writer.CancelPendingFlush();
                    }
                    catch
                    {
                        // We don't care if (for some reason) it was completed elsewhere, only that it was
                    }

                    if (!_pipeWriterCompleted)
                    {
                        using var writer = RentChannelWriter();

                        if (!writer.IsCompleted)
                            writer.Complete(CloseException);
                    }
                }

                _pipe = null;
                _pipeWriterCompleted = false;
            }

            protected override async ValueTask DisposeAsyncCore()
            {
                Debug.Assert(CloseException != null);

                try
                {
                    await ChannelReader.CompleteAsync(CloseException == CloseSentinel ? null : CloseException).ConfigureAwait(false);
                    ChannelReader.CancelPendingRead();
                    GetPipe().Writer.CancelPendingFlush();
                }
                catch
                {
                    // We don't care if (for some reason) it was completed elsewhere, only that it was
                }

                if (!_pipeWriterCompleted)
                {
                    await using var writer = await RentChannelWriterAsync().ConfigureAwait(false);

                    if (!writer.IsCompleted)
                        await writer.CompleteAsync(CloseException).ConfigureAwait(false);
                }

                _pipe = null;
                _pipeWriterCompleted = false;
            }

            protected virtual Pipe NewPipe()
            {
                return new Pipe(ChannelPipeReader.DefaultPipeOptions);
            }

            private object PipeInitLock => _pipeWriterLock;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private protected Pipe GetPipe()
            {
                return _pipe ?? InitPipe();

                [MethodImpl(MethodImplOptions.NoInlining)]
                Pipe InitPipe()
                {
                    lock (PipeInitLock)
                    {
                        return _pipe ?? (_pipe = NewPipe());
                    }
                }
            }

            private class DefaultPipeReader : ChannelPipeReader
            {
                protected override PipeReader Reader
                {
                    get
                    {
                        var channel = (DefaultChannel)Channel;
                        channel.ThrowIfDisposed();
                        return channel.ChannelReader;
                    }
                }

                public DefaultPipeReader(DefaultChannel channel, bool leaveOpen) : base(channel, leaveOpen)
                {
                }
            }
        }
    }
}
