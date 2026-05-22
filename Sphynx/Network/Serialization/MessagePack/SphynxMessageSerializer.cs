// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Sphynx.Network.Packet;
using Sphynx.Network.Transport;

namespace Sphynx.Network.Serialization.MessagePack
{
    public sealed class SphynxMessageSerializer : SphynxMessageFormatter<SphynxMessage>
    {
        private static readonly SphynxMessageType[] _msgTypes = Enum.GetValues<SphynxMessageType>();

        public override bool OwnsWriter => _formatters.Any(f => f.Value.OwnsWriter);
        public override bool OwnsReader => _formatters.Any(f => f.Value.OwnsReader);

        private readonly Dictionary<SphynxMessageType, ISphynxMessageFormatterCore> _formatters = new();

        public SphynxMessageSerializer() : base(null)
        {
        }

        protected override ValueTask WriteMessageAsync(SphynxMessage message, SphynxChannelWriter.Channel channel,
            CancellationToken cancellationToken)
        {
            if (!_formatters.TryGetValue(message.MessageType, out var formatter))
                return ValueTask.FromException(new SerializationException($"No serializer registered for message of type {message.MessageType}"));

            if (formatter.OwnsWriter)
                return formatter.WriteMessageAsync(message, channel, cancellationToken);

            return Core(formatter, message, channel, cancellationToken);

            [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
            static async ValueTask Core(ISphynxMessageFormatterCore formatter, SphynxMessage msg, SphynxChannelWriter.Channel channel,
                CancellationToken token)
            {
                Exception? writeException = null;

                try
                {
                    await formatter.WriteMessageAsync(msg, channel, token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    writeException = ex;
                    throw;
                }
                finally
                {
                    await channel.DisposeAsync(writeException).ConfigureAwait(false);
                }
            }
        }

        protected override ValueTask<SphynxMessage> ReadMessageAsync(SphynxMessageType msgType, SphynxChannelReader.Channel channel,
            CancellationToken cancellationToken)
        {
            if (!_formatters.TryGetValue(msgType, out var formatter))
                return ValueTask.FromException<SphynxMessage>(new SerializationException($"No serializer registered for message of type {msgType}"));

            if (formatter.OwnsReader)
                return formatter.ReadMessageAsync(msgType, channel, cancellationToken);

            return Core(formatter, msgType, channel, cancellationToken);

            [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
            static async ValueTask<SphynxMessage> Core(ISphynxMessageFormatterCore formatter, SphynxMessageType msgType,
                SphynxChannelReader.Channel channel,
                CancellationToken token)
            {
                Exception? writeException = null;

                try
                {
                    return await formatter.ReadMessageAsync(msgType, channel, token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    writeException = ex;
                    throw;
                }
                finally
                {
                    await channel.DisposeAsync(writeException).ConfigureAwait(false);
                }
            }
        }

        public SphynxMessageSerializer WithFormatter<T>(SphynxMessageFormatter<T> formatter)
            where T : SphynxMessage
        {
            if (formatter.MessageType != null)
            {
                _formatters[formatter.MessageType.Value] = formatter;
            }
            else
            {
                foreach (var msgType in _msgTypes)
                    _formatters[msgType] = formatter;
            }

            return this;
        }

        public SphynxMessageSerializer WithoutFormatter(SphynxMessageType messageType)
        {
            _formatters.Remove(messageType);
            return this;
        }
    }
}
