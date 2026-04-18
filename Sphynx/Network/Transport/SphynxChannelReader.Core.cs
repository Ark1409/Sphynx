// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using Sphynx.Utils;

namespace Sphynx.Network.Transport
{
    //
    // This file holds the actual frame parsing implementation for the channel reader.
    //

    public partial class SphynxChannelReader
    {
        private object? _onChannelRejectedState;
        private volatile Action<object?, ChannelId>? _onChannelRejected;

        private object? _onFrameDroppedState;
        private volatile Action<object?, SphynxFrameHeader>? _onFrameDropped;

        private object? _onChannelRejectingState;
        private volatile Action<object?, ChannelId>? _onChannelRejecting;

        /// <summary>
        /// Holds a mapping of all currently active reading channels.
        /// </summary>
        protected readonly ConcurrentDictionary<ChannelId, Channel> OpenChannels = new();

        /// <summary>
        /// Registers a callback that is called when an open channel is <see cref="Channel.Dispose()">disposed</see> before the remote peer finishes
        /// sending all its data.
        /// </summary>
        /// <remarks>This is when a <see cref="SphynxFrameType.CHANNEL_REJECT"/> should be sent to the remote peer.</remarks>
        public void OnChannelRejecting(Action<object?, ChannelId> callback, object? state = null)
        {
            _onChannelRejectingState = state;
            _onChannelRejecting = callback;
        }

        /// <summary>
        /// Registers a callback for when an incoming channel frame is dropped. This can occur if <see cref="MaxOpenChannels">too many</see>
        /// channels are open, or invalid data is received. Under normal circumstances, existing channels should never lose any data.
        /// </summary>
        public void OnChannelFrameDropped(Action<object?, SphynxFrameHeader> callback, object? state = null)
        {
            _onFrameDroppedState = state;
            _onFrameDropped = callback;
        }

        /// <summary>
        /// Callback for when a <see cref="SphynxFrameType.CHANNEL_REJECT"/> is received from the remote peer.
        /// </summary>
        /// <seealso cref="SphynxFrameType.CHANNEL_REJECT"/>
        public void OnChannelRejected(Action<object?, ChannelId> callback, object? state = null)
        {
            _onChannelRejectedState = state;
            _onChannelRejected = callback;
        }

        protected virtual async Task ReadChannelsAsync(CancellationToken cancellationToken)
        {
            // NOTE: We don't want to split this into multiple async methods, as that would cause unnecessary allocations.

            while (true)
            {
                var frameHeader = await SphynxFrameHeader.ReceiveAsync(Stream, allowInvalid: true, cancellationToken).ConfigureAwait(false);
                bool channelExists = OpenChannels.TryGetValue(frameHeader.ChannelId, out var channel);

                if (!frameHeader.IsValid())
                {
                    // Don't make any assumptions about the header's contents if the versions aren't the same
                    bool sameVersion = frameHeader.Version == SphynxFrameHeader.ProtocolVersion;

                    if (sameVersion && channelExists && frameHeader.FrameType == SphynxFrameType.CHANNEL_DATA)
                    {
                        var protocolException = new SphynxProtocolException(
                            $"Invalid frame received {frameHeader.FrameType} ({nameof(channel.ChannelId)}: {channel!.ChannelId})");
                        DisposeChannel(channel, protocolException);
                    }

                    InvokeChannelFrameDropped(in frameHeader);

                    // We'll still take the size hint, even if the frame itself is invalid
                    if (sameVersion && frameHeader.FrameSize > 0)
                        await Stream.SkipAsync(frameHeader.FrameSize, cancellationToken).ConfigureAwait(false);

                    continue;
                }

                Debug.Assert(Enum.IsDefined(frameHeader.FrameType));

                if (frameHeader.FrameType == SphynxFrameType.CHANNEL_REJECT)
                {
                    InvokeChannelRejected(frameHeader.ChannelId);

                    Debug.Assert(frameHeader.FrameSize == 0);
                    continue;
                }

                if (frameHeader.FrameType == SphynxFrameType.CHANNEL_ABORT)
                {
                    if (channelExists)
                    {
                        var abortException = new ChannelClosedException("The channel was aborted by the remote peer");
                        DisposeChannel(channel!, abortException);
                    }

                    Debug.Assert(frameHeader.FrameSize == 0);
                    continue;
                }

                Debug.Assert(frameHeader.FrameType == SphynxFrameType.CHANNEL_DATA);

                if (frameHeader.HasFlags(ChannelDataFlags.CHANNEL_START))
                {
                    if (channelExists)
                    {
                        var protocolException = new SphynxProtocolException("A duplicate channel with the same ID was opened");
                        DisposeChannel(channel!, protocolException);
                    }

                    if (_onChannelOpened == null)
                    {
                        InvokeChannelRejecting(frameHeader.ChannelId);
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
                            InvokeChannelRejecting(frameHeader.ChannelId);
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

                    if (!InvokeChannelOpened(channel))
                    {
                        DisposeChannel(channel, null);
                        InvokeChannelFrameDropped(in frameHeader);

                        await Stream.SkipAsync(frameHeader.FrameSize, cancellationToken).ConfigureAwait(false);
                        continue;
                    }
                }

                if (!channelExists)
                {
                    InvokeChannelFrameDropped(in frameHeader);

                    await Stream.SkipAsync(frameHeader.FrameSize, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                Debug.Assert(channelExists && channel != null);

                // Now read the actual channel data

                await using var channelWriter = await channel.RentChannelWriterAsync(cancellationToken).ConfigureAwait(false);

                if (channelWriter.IsCompleted)
                {
                    InvokeChannelFrameDropped(in frameHeader);
                    continue;
                }

                // Split the frame into SEGMENT_SIZE segments
                const int SEGMENT_SIZE = SphynxChannelWriter.Channel.DEFAULT_FRAME_SIZE;

                for (int segment = 0; segment * SEGMENT_SIZE < frameHeader.FrameSize; segment++)
                {
                    int bytesLeft = frameHeader.FrameSize - segment * SEGMENT_SIZE;
                    int readSize = Math.Min(SEGMENT_SIZE, bytesLeft);
                    var memory = channelWriter.GetMemory(readSize)[..readSize];

                    int readCount = await Stream.ReadAtLeastAsync(memory, memory.Length, false, cancellationToken).ConfigureAwait(false);
                    channelWriter.Advance(readCount);

                    if (readCount < memory.Length)
                    {
                        InvokeChannelFrameDropped(in frameHeader);

                        var endException = new EndOfStreamException();
                        await channelWriter.CompleteAsync(endException).ConfigureAwait(false);
                        throw endException;
                    }
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

        private void DisposeChannel(Channel channel, Exception? disposeException)
        {
            OpenChannels.TryRemove(new KeyValuePair<ChannelId, Channel>(channel.ChannelId, channel));

            if (channel.IsDisposed)
                return;

            ThreadPool.QueueUserWorkItem(static async void (state) =>
            {
                try
                {
                    await state.channel.DisposeAsync(state.disposeException).ConfigureAwait(false);
                }
                catch
                {
                    // ignore
                }
            }, (channel, disposeException), preferLocal: false);
        }

        private bool InvokeChannelOpened(Channel channel)
        {
            var callback = _onChannelOpened;
            object? callbackState = _onChannelOpenedState;

            if (callback == null)
                return false;

            ThreadPool.QueueUserWorkItem(static async void (state) =>
            {
                try
                {
                    await state.callback.Invoke(state.stateArg, state.channel).ConfigureAwait(false);
                }
                catch
                {
                    // ignore
                }
            }, (callback, stateArg: callbackState, channel), preferLocal: false);
            return true;
        }

        private void InvokeChannelFrameDropped(in SphynxFrameHeader header)
        {
            if (_onFrameDropped == null)
                return;

            ThreadPool.QueueUserWorkItem(static state =>
            {
                try
                {
                    state.reader._onFrameDropped?.Invoke(state.reader._onFrameDroppedState, state.header);
                }
                catch
                {
                    // ignore
                }
            }, (reader: this, header), preferLocal: false);
        }

        private void InvokeChannelRejected(ChannelId channelId)
        {
            if (_onChannelRejected == null)
                return;

            ThreadPool.QueueUserWorkItem(static state =>
            {
                try
                {
                    state.reader._onChannelRejected?.Invoke(state.reader._onChannelRejectedState, state.channelId);
                }
                catch
                {
                    // ignore
                }
            }, (reader: this, channelId), preferLocal: false);
        }

        private void InvokeChannelRejecting(ChannelId channelId)
        {
            if (_onChannelRejecting == null)
                return;

            ThreadPool.QueueUserWorkItem(static state =>
            {
                try
                {
                    state.reader._onChannelRejecting?.Invoke(state.reader._onChannelRejectingState, state.channelId);
                }
                catch
                {
                    // ignore
                }
            }, (reader: this, channelId), preferLocal: false);
        }

        protected virtual Channel NewChannel(ChannelId channelId) => new Channel(this, channelId);

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                UnregisterCallbacks();

                foreach (var (_, channel) in OpenChannels)
                {
                    try
                    {
                        channel.Dispose(_disposeException);
                    }
                    catch
                    {
                        // ignore
                    }
                }

                OpenChannels.Clear();
            }
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            UnregisterCallbacks();

            foreach (var (_, channel) in OpenChannels)
            {
                try
                {
                    await channel.DisposeAsync(_disposeException).ConfigureAwait(false);
                }
                catch
                {
                    // ignore
                }
            }

            OpenChannels.Clear();
        }

        private void UnregisterCallbacks()
        {
            _onChannelRejecting = null;
            _onChannelRejectingState = null;
            _onFrameDropped = null;
            _onFrameDroppedState = null;
            _onChannelRejected = null;
            _onChannelRejectedState = null;
        }

        public partial class Channel
        {
            protected internal readonly struct ChannelWriterRent : IDisposable, IAsyncDisposable, IBufferWriter<byte>
            {
                public bool IsCompleted => _channel._writerCompleted;

                private readonly PipeWriter _writer;
                private readonly Channel _channel;
                private readonly SemaphoreSlim _writerLock;

                internal ChannelWriterRent(Channel channel, SemaphoreSlim writerLock)
                {
                    _channel = channel;
                    _writer = _channel.GetPipe().Writer;

                    _writerLock = writerLock;
                    Debug.Assert(writerLock.CurrentCount == 0);
                }

                public void Advance(int count) => _writer.Advance(count);
                public Memory<byte> GetMemory(int sizeHint = 0) => _writer.GetMemory(sizeHint);
                public Span<byte> GetSpan(int sizeHint = 0) => _writer.GetSpan(sizeHint);
                public ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default) => _writer.FlushAsync(cancellationToken);

                public void Complete(Exception? ex = null)
                {
                    _writer.Complete(ex);
                    _channel.Parent.OpenChannels.TryRemove(new KeyValuePair<ChannelId, Channel>(_channel.ChannelId, _channel));
                    _channel._writerCompleted = true;
                }

                public async ValueTask CompleteAsync(Exception? ex = null)
                {
                    await _writer.CompleteAsync(ex).ConfigureAwait(false);
                    _channel.Parent.OpenChannels.TryRemove(new KeyValuePair<ChannelId, Channel>(_channel.ChannelId, _channel));
                }

                public void Dispose()
                {
                    if (IsCompleted)
                    {
                        _writerLock.Release();
                        return;
                    }

                    try
                    {
                        var flushTask = FlushAsync();

                        if (!flushTask.IsCompleted)
                            flushTask = flushTask.Preserve();

                        var result = flushTask.GetAwaiter().GetResult();

                        if (result.IsCompleted && !_channel._writerCompleted)
                            Complete(_channel.CloseException);
                    }
                    catch
                    {
                        // If we have problems flushing, we don't really care; it's likely the underlying pipe
                        // is somehow "broken" anyway
                    }
                    finally
                    {
                        _writerLock.Release();
                    }
                }

                public ValueTask DisposeAsync()
                {
                    if (IsCompleted)
                    {
                        _writerLock.Release();
                        return ValueTask.CompletedTask;
                    }

                    return Core(this);

                    static async ValueTask Core(ChannelWriterRent thisRef)
                    {
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
        }

        public partial class Channel
        {
            protected internal static readonly PipeOptions DefaultPipeOptions = new(
                pauseWriterThreshold: SphynxChannelWriter.Channel.DEFAULT_FRAME_SIZE * 4,
                resumeWriterThreshold: SphynxChannelWriter.Channel.DEFAULT_FRAME_SIZE * 2,
                minimumSegmentSize: SphynxChannelWriter.Channel.DEFAULT_FRAME_SIZE,
                useSynchronizationContext: false);

            private volatile Pipe? _pipe;
            private readonly SemaphoreSlim _pipeWriterLock = new(1, 1);

            /// <summary>
            /// The <see cref="PipeReader"/> for incoming channel data.
            /// </summary>
            protected PipeReader ChannelReader => GetPipe().Reader;

            public Channel(SphynxChannelReader parent, ChannelId channelId)
            {
                Parent = parent;
                ChannelId = channelId;
            }

            // Read semantics copied from: PipeReaderStream.cs
            //
            // Copyright (c) .NET Foundation and Contributors
            //
            // All rights reserved.
            //
            // Permission is hereby granted, free of charge, to any person obtaining a copy
            // of this software and associated documentation files (the "Software"), to deal
            // in the Software without restriction, including without limitation the rights
            // to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
            // copies of the Software, and to permit persons to whom the Software is
            // furnished to do so, subject to the following conditions:
            //
            // The above copyright notice and this permission notice shall be included in all
            // copies or substantial portions of the Software.

            public int Read(Span<byte> buffer)
            {
                ThrowIfDisposed();

                var task = ChannelReader.ReadAsync();
                var result = task.IsCompletedSuccessfully ? task.Result : task.Preserve().GetAwaiter().GetResult();
                return HandleReadResult(result, buffer);
            }

            public virtual ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                if (IsDisposed)
                    return ValueTask.FromException<int>(GetDisposedException());

                if (ChannelReader.TryRead(out var result))
                    return ValueTask.FromResult(HandleReadResult(result, buffer.Span));

                return Core(buffer, cancellationToken);

                async ValueTask<int> Core(Memory<byte> memory, CancellationToken token)
                {
                    var readResult = await ChannelReader.ReadAsync(token).ConfigureAwait(false);
                    return HandleReadResult(readResult, memory.Span);
                }
            }

            private int HandleReadResult(ReadResult result, Span<byte> buffer)
            {
                if (result.IsCanceled)
                    throw new OperationCanceledException(null, CloseException);

                ReadOnlySequence<byte> sequence = result.Buffer;
                long bufferLength = sequence.Length;
                SequencePosition consumed = sequence.Start;

                try
                {
                    if (bufferLength != 0)
                    {
                        int actual = (int)Math.Min(bufferLength, buffer.Length);

                        ReadOnlySequence<byte> slice = actual == bufferLength ? sequence : sequence.Slice(0, actual);
                        consumed = slice.End;
                        slice.CopyTo(buffer);

                        BytesRead += actual;
                        return actual;
                    }

                    if (result.IsCompleted)
                        return 0;
                }
                finally
                {
                    ChannelReader.AdvanceTo(consumed);
                }

                // This is a buggy PipeReader implementation that returns 0 byte reads even though the PipeReader
                // isn't completed or canceled?
                throw new InvalidOperationException("0 byte read occured when channel was not completed");
            }

            /// <summary>
            /// Rent exclusive access to this channel's <see cref="PipeWriter"/>.
            /// </summary>
            /// <param name="cancellationToken">A cancellation token for the wait operation.</param>
            /// <returns>The rented channel writer. You should call <see cref="ChannelWriterRent.Dispose"/> once you are done.</returns>
            protected internal ChannelWriterRent RentChannelWriter(CancellationToken cancellationToken = default)
            {
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
                var waitTask = _pipeWriterLock.WaitAsync(cancellationToken);

                if (waitTask.IsCompletedSuccessfully)
                    return ValueTask.FromResult(new ChannelWriterRent(this, _pipeWriterLock));

                if (waitTask.IsCanceled)
                    return ValueTask.FromCanceled<ChannelWriterRent>(cancellationToken);

                return Core(waitTask);

                async ValueTask<ChannelWriterRent> Core(Task task)
                {
                    await task.ConfigureAwait(false);
                    return new ChannelWriterRent(this, _pipeWriterLock);
                }
            }

            private ChannelPipeReader? _channelPipeReader;

            public virtual PipeReader AsPipeReader(bool leaveOpen = true)
            {
                _channelPipeReader ??= new ChannelPipeReader(this, ChannelReader, leaveOpen);
                _channelPipeReader.LeaveOpen = leaveOpen;
                return _channelPipeReader;
            }

            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    Debug.Assert(CloseException != null);

                    try
                    {
                        ChannelReader.Complete(CloseException);
                    }
                    catch
                    {
                        // We don't care if (for some reason) it was completed elsewhere, only that it was
                    }

                    using (var writer = RentChannelWriter())
                    {
                        if (!writer.IsCompleted)
                        {
                            Parent.InvokeChannelRejecting(ChannelId);
                            writer.Complete(CloseException);
                        }
                    }
                }

                _pipe = null;
            }

            protected virtual async ValueTask DisposeAsyncCore()
            {
                Debug.Assert(CloseException != null);

                try
                {
                    await ChannelReader.CompleteAsync(CloseException).ConfigureAwait(false);
                }
                catch
                {
                    // We don't care if (for some reason) it was completed elsewhere, only that it was
                }

                await using (var writer = await RentChannelWriterAsync().ConfigureAwait(false))
                {
                    if (!writer.IsCompleted)
                    {
                        Parent.InvokeChannelRejecting(ChannelId);
                        await writer.CompleteAsync(CloseException).ConfigureAwait(false);
                    }
                }

                _pipe = null;
            }

            protected virtual Pipe NewPipe()
            {
                return new Pipe(DefaultPipeOptions);
            }

            private object PipeInitLock => _pipeWriterLock;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private protected Pipe GetPipe()
            {
                if (_pipe != null)
                    return _pipe;

                return InitPipe();

                [MethodImpl(MethodImplOptions.NoInlining)]
                Pipe InitPipe()
                {
                    lock (PipeInitLock)
                    {
                        return _pipe ?? (_pipe = NewPipe());
                    }
                }
            }
        }
    }
}
