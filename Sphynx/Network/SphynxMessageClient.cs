// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Sphynx.Network.Packet;
using Sphynx.Network.Serialization;
using Sphynx.Network.Transport;
using Sphynx.Utils;

namespace Sphynx.Network
{
    public delegate void MessageReceivedHandler(object? state, SphynxMessage message);
    public delegate void MessageDroppedHandler(object? state, SphynxMessage? message, Exception? error);

    public class SphynxMessageClient : IDisposable, IAsyncDisposable
    {
        protected SphynxChannelReader? Reader;
        protected SphynxChannelWriter? Writer;
        private volatile IMessageFormatter _formatter;

        public IMessageFormatter MessageFormatter
        {
            get => _formatter;
            set => _formatter = value;
        }

        private volatile MessageReceivedHandler? _messageReceived;
        private object? _messageReceivedState;

        private volatile MessageDroppedHandler? _messageDropped;
        private object? _messageDroppedState;

        [MemberNotNullWhen(true, nameof(Reader))]
        public bool CanRead => Reader != null;

        [MemberNotNullWhen(true, nameof(Writer))]
        public bool CanWrite => Writer != null;

        protected CancellationTokenSource DisposeCts = new();
        protected bool IsDisposed => DisposeCts.IsCancellationRequested;

        public SphynxMessageClient(Stream stream, bool ownsStream, IMessageFormatter formatter)
        {
            if (stream.CanWrite)
                Writer = new DefaultChannelWriter(stream, ownsStream);

            if (stream.CanRead)
                Reader = new DefaultChannelReader(stream, ownsStream && !CanWrite);

            if (CanWrite && CanRead)
                SphynxChannel.ConnectChannel((DefaultChannelWriter)Writer, (DefaultChannelReader)Reader);

            _formatter = formatter;
        }

        public SphynxMessageClient(SphynxChannel channel, IMessageFormatter formatter) : this(channel.Writer, channel.Reader, formatter)
        {
            ArgumentNullException.ThrowIfNull(channel);
            ArgumentNullException.ThrowIfNull(formatter);
        }

        public SphynxMessageClient(SphynxChannelWriter writer, IMessageFormatter formatter) : this(writer, null, formatter)
        {
            ArgumentNullException.ThrowIfNull(writer);
            ArgumentNullException.ThrowIfNull(formatter);
        }

        public SphynxMessageClient(SphynxChannelReader reader, IMessageFormatter formatter) : this(null, reader, formatter)
        {
            ArgumentNullException.ThrowIfNull(reader);
            ArgumentNullException.ThrowIfNull(formatter);
        }

        private SphynxMessageClient(SphynxChannelWriter? writer, SphynxChannelReader? reader, IMessageFormatter formatter)
        {
            // No argument checking done here; must be performed by the caller
            Writer = writer;
            Reader = reader;
            Reader?.OnChannelOpened(OnChannelOpened, this);
            _formatter = formatter;
        }

        public static SphynxMessageClient FromSimplex(DefaultChannelWriter writer, DefaultChannelReader reader, IMessageFormatter formatter)
        {
            ArgumentNullException.ThrowIfNull(reader);
            ArgumentNullException.ThrowIfNull(writer);
            ArgumentNullException.ThrowIfNull(formatter);

            SphynxChannel.ConnectChannel(writer, reader);
            return new SphynxMessageClient(writer, reader, formatter);
        }

        public void OnMessageReceived(MessageReceivedHandler callback, object? state = null)
        {
            _messageReceivedState = state;
            _messageReceived = callback;
        }

        public void OnMessageDropped(MessageDroppedHandler callback, object? state = null)
        {
            _messageDroppedState = state;
            _messageDropped = callback;
        }

        public void Start(CancellationToken cancellationToken = default)
        {
            if (!CanRead)
                throw new InvalidOperationException("Message client is not readable");

            Reader.Start(cancellationToken);
        }

        public Task RunAsync(CancellationToken cancellationToken = default)
        {
            if (!CanRead)
                return Task.FromException(new InvalidOperationException("Message client is not readable"));

            return Reader.RunAsync(cancellationToken);
        }

        public ValueTask SendMessageAsync(SphynxMessage message, CancellationToken cancellationToken = default)
        {
            if (IsDisposed)
                return ValueTask.FromException(GetDisposedException());

            if (cancellationToken.IsCancellationRequested)
                return ValueTask.FromCanceled(cancellationToken);

            if (!CanWrite)
                return ValueTask.FromException(new InvalidOperationException("Message client is not writable"));

            return Core(message, cancellationToken);

            [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
            async ValueTask Core(SphynxMessage msg, CancellationToken token)
            {
                var channel = Writer.OpenChannel();

                try
                {
                    await _formatter.SerializeAsync(msg, channel, token).ConfigureAwait(false);

                    if (!_formatter.OwnsWriter && !channel.IsDisposed)
                        await channel.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception ex) when (!_formatter.OwnsWriter && !channel.IsDisposed)
                {
                    await channel.DisposeAsync(ex).ConfigureAwait(false);
                    throw;
                }
            }
        }

        private static ValueTask OnChannelOpened(object? state, SphynxChannelReader.Channel channel)
        {
            return ((SphynxMessageClient)state!).OnChannelOpenedAsync(channel);
        }

        [AsyncStateMachine(typeof(PoolingAsyncValueTaskMethodBuilder))]
        private async ValueTask OnChannelOpenedAsync(SphynxChannelReader.Channel channel)
        {
            SphynxMessage message;

            try
            {
                message = await _formatter.DeserializeAsync(channel, DisposeCts.Token).ConfigureAwait(false);

                if (!_formatter.OwnsReader && !channel.IsDisposed)
                    await channel.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                InvokeMessageDropped(null, ex);

                if (!_formatter.OwnsReader && !channel.IsDisposed)
                    await channel.DisposeAsync(ex).ConfigureAwait(false);

                throw;
            }

            if (!InvokeMessageReceived(message))
            {
                if (!InvokeMessageDropped(message, null))
                {
                    // Default behaviour for uncaught messages
                    await DisposeMessageAsync(message).ConfigureAwait(false);
                }
            }

            static ValueTask DisposeMessageAsync(SphynxMessage msg)
            {
                if (msg is IAsyncDisposable asyncDisposable)
                    return asyncDisposable.DisposeAsync();

                if (msg is IDisposable disposable)
                {
                    try
                    {
                        disposable.Dispose();
                    }
                    catch (Exception ex)
                    {
                        return ValueTask.FromException(ex);
                    }
                }

                return ValueTask.CompletedTask;
            }
        }

        private bool InvokeMessageReceived(SphynxMessage message)
        {
            var callback = _messageReceived;
            object? callbackState = _messageReceivedState;

            if (callback == null)
                return false;

            return ThreadPoolHelper.QueueUserWorkItem(static void (state) =>
            {
                try
                {
                    state.callback.Invoke(state.callbackState, state.message);
                }
                catch
                {
                    // ignore
                }
            }, (message, callback, callbackState));
        }

        private bool InvokeMessageDropped(SphynxMessage? message, Exception? error)
        {
            var callback = _messageDropped;
            object? callbackState = _messageDroppedState;

            if (callback == null)
                return false;

            return ThreadPoolHelper.QueueUserWorkItem(static void (state) =>
            {
                try
                {
                    state.callback.Invoke(state.callbackState, state.message, state.error);
                }
                catch
                {
                    // ignore
                }
            }, (message, error, callback, callbackState));
        }

        private ObjectDisposedException GetDisposedException() => new(GetType().Name);

        public void Dispose()
        {
            if (IsDisposed)
                return;

            DisposeCts.Cancel();
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            Reader?.Dispose();
            Writer?.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            if (IsDisposed)
                return;

            await DisposeCts.CancelAsync().ConfigureAwait(false);
            await DisposeAsyncCore().ConfigureAwait(false);
            Dispose(false);
            GC.SuppressFinalize(this);
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (Writer != null) await Writer.DisposeAsync().ConfigureAwait(false);
            if (Reader != null) await Reader.DisposeAsync().ConfigureAwait(false);
        }
    }

    public class PoolableMessageClient : SphynxMessageClient
    {
        private PoolableSphynxChannel _channel;

        public PoolableMessageClient(PoolableSphynxChannel channel, IMessageFormatter formatter)
            : base(channel, formatter)
        {
            _channel = channel;
        }

        public void Reset(PoolableSphynxChannel channel)
        {
            DisposeCts = new CancellationTokenSource();
            _channel = channel;
        }

        public void Reset(Stream stream, bool? ownsStream = null)
        {
            DisposeCts = new CancellationTokenSource();
            _channel.Reset(stream, ownsStream);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _channel.Dispose();
        }

        protected override ValueTask DisposeAsyncCore()
        {
            return _channel.DisposeAsync();
        }
    }
}
