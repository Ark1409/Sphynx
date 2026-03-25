// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Sphynx.Network.Packet;
using Sphynx.Network.Serialization;
using Sphynx.Network.Transport;

namespace Sphynx.Network
{
    public class SphynxMessageClient : IDisposable, IAsyncDisposable
    {
        private readonly ConcurrentDictionary<long, SphynxChannelWriter.Channel> _openWriteChannels = new();

        private readonly SphynxChannelReader? _reader;
        private readonly SphynxChannelWriter? _writer;
        private volatile IMessageFormatter _formatter;

        public IMessageFormatter MessageFormatter
        {
            get => _formatter;
            set => _formatter = value;
        }

        public Action<SphynxMessage>? MessageReceived { private get; set; }

        public bool CanRead => _reader != null;
        public bool CanWrite => _writer != null;

        private readonly CancellationTokenSource _disposeCts = new();
        private bool IsDisposed => _disposeCts.IsCancellationRequested;

        public SphynxMessageClient(Stream stream, IMessageFormatter messageFormatter)
        {
            ArgumentNullException.ThrowIfNull(stream);
            ArgumentNullException.ThrowIfNull(messageFormatter);

            if (!stream.CanRead && !stream.CanWrite)
                throw new ArgumentException("Stream must be readable or writable", nameof(stream));

            if (stream.CanRead)
                _reader = new SphynxChannelReader(stream, OnChannelOpened);

            if (stream.CanWrite)
                _writer = new SphynxChannelWriter(stream);

            _formatter = messageFormatter;
        }

        public SphynxMessageClient(SphynxChannelReader inputChannel, IMessageFormatter messageFormatter)
        {
            ArgumentNullException.ThrowIfNull(inputChannel);
            ArgumentNullException.ThrowIfNull(messageFormatter);

            _reader = inputChannel;
            _reader.ChannelDropReceived = OnChannelDropReceived;
            _reader.ChannelOpened = OnChannelOpened;

            _formatter = messageFormatter;
        }

        public SphynxMessageClient(SphynxChannelWriter outputChannel, IMessageFormatter messageFormatter)
        {
            ArgumentNullException.ThrowIfNull(outputChannel);
            ArgumentNullException.ThrowIfNull(messageFormatter);

            _writer = outputChannel;
            _formatter = messageFormatter;
        }

        public SphynxMessageClient(SphynxChannelReader inputChannel, SphynxChannelWriter outputChannel, IMessageFormatter messageFormatter)
        {
            ArgumentNullException.ThrowIfNull(inputChannel);
            ArgumentNullException.ThrowIfNull(outputChannel);
            ArgumentNullException.ThrowIfNull(messageFormatter);

            _reader = inputChannel;
            _reader.ChannelDropReceived = OnChannelDropReceived;
            _reader.ChannelOpened = OnChannelOpened;

            _writer = outputChannel;
            _formatter = messageFormatter;
        }

        public Task RunAsync(CancellationToken cancellationToken = default)
        {
            if (!CanRead)
                return Task.FromException(new InvalidOperationException("Message client is not readable"));

            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);

            return _reader!.RunAsync(cancellationToken);
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
            async ValueTask Core(SphynxMessage msg, CancellationToken ct)
            {
                var channel = _writer!.OpenChannel();

                try
                {
                    if (!_openWriteChannels.TryAdd(channel.ChannelId, channel))
                        ThrowNonUniqueChannelException();

                    await _formatter.SerializeAsync(msg, channel, ct).ConfigureAwait(false);

                    // Ideally we'd only want to remove it once the channel's been disposed, so that CHANNEL_DROPs are still acted upon,
                    // but that would require an extra delegate allocation for each serialize call.
                    _openWriteChannels.TryRemove(new KeyValuePair<long, SphynxChannelWriter.Channel>(channel.ChannelId, channel));

                    if (_formatter.OwnsWriter || channel.IsDisposed)
                        return;

                    await channel.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _openWriteChannels.TryRemove(new KeyValuePair<long, SphynxChannelWriter.Channel>(channel.ChannelId, channel));

                    if (!channel.IsDisposed)
                        await channel.DisposeAsync(ex).ConfigureAwait(false);

                    Throw(ex);
                }
            }

            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.NoInlining)]
            static void Throw(Exception ex) => throw ex;

            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.NoInlining)]
            static void ThrowNonUniqueChannelException() => throw new ChannelClosedException("Could not generate a unique channel ID");
        }

        private void OnChannelOpened(SphynxChannelReader.Channel channel)
        {
            var openTask = OnChannelOpenedAsync(channel);

            if (openTask.IsCompleted)
                openTask.GetAwaiter().GetResult();
            else
                openTask.GetAwaiter().OnCompleted(() => openTask.GetAwaiter().GetResult());
        }

        private readonly struct MessageCallbackState
        {
            public SphynxMessage Message { get; init; }
            public Action<SphynxMessage> Callback { get; init; }
        }

        [AsyncStateMachine(typeof(PoolingAsyncValueTaskMethodBuilder))]
        private async ValueTask OnChannelOpenedAsync(SphynxChannelReader.Channel channel)
        {
            SphynxMessage message;

            try
            {
                message = await _formatter.DeserializeAsync(channel, _disposeCts.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (!channel.IsDisposed)
                    await channel.DisposeAsync(ex).ConfigureAwait(false);

                throw;
            }

            if (!_formatter.OwnsReader)
                await channel.DisposeAsync().ConfigureAwait(false);

            if (MessageReceived == null)
            {
                // Discard the message in this case. If needed, we can expose an API for this.
                if (message is IAsyncDisposable asyncDisposable)
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                else if (message is IDisposable disposable)
                    disposable.Dispose();

                return;
            }

            var state = new MessageCallbackState
            {
                Message = message,
                Callback = MessageReceived
            };

            ThreadPool.QueueUserWorkItem(static state => state.Callback.Invoke(state.Message), state, preferLocal: false);
        }

        private void OnChannelDropReceived(long channelId)
        {
            if (_openWriteChannels.TryRemove(channelId, out var channel) && !channel.IsDisposed)
            {
                try
                {
                    _ = channel.DisposeAsync(new ChannelClosedException());
                }
                catch
                {
                    // ignore
                }
            }
        }

        private ObjectDisposedException GetDisposedException() => new(GetType().Name);

        public void Dispose()
        {
            if (IsDisposed)
                return;

            _disposeCts.Cancel();
            _reader?.Dispose();
            _writer?.Dispose();

            foreach (var (_, channel) in _openWriteChannels)
                Debug.Assert(channel.IsDisposed);

            _openWriteChannels.Clear();
        }

        public async ValueTask DisposeAsync()
        {
            if (IsDisposed)
                return;

            await _disposeCts.CancelAsync().ConfigureAwait(false);

            if (_reader != null) await _reader.DisposeAsync().ConfigureAwait(false);
            if (_writer != null) await _writer.DisposeAsync().ConfigureAwait(false);

            foreach (var (_, channel) in _openWriteChannels)
                Debug.Assert(channel.IsDisposed);

            _openWriteChannels.Clear();
        }
    }
}
