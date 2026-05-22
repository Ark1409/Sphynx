// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Sphynx.Network.Packet;
using Sphynx.Network.Packet.Response;
using Sphynx.Network.Transport;

namespace Sphynx.Network.Serialization.MessagePack
{
    public sealed class SphynxResponseSerializer : SphynxResponseFormatter<SphynxResponse>
    {
        private static readonly SphynxRequestType[] _respTypes = Enum.GetValues<SphynxRequestType>();

        public override bool OwnsWriter => _formatters.Any(f => f.Value.OwnsWriter);
        public override bool OwnsReader => _formatters.Any(f => f.Value.OwnsReader);

        private readonly Dictionary<SphynxRequestType, ISphynxResponseFormatterCore> _formatters = new();

        public SphynxResponseSerializer() : base(null)
        {
        }

        protected override ValueTask WriteResponseAsync(SphynxResponse response, SphynxChannelWriter.Channel channel,
            CancellationToken cancellationToken)
        {
            if (!_formatters.TryGetValue(response.ResponseType, out var formatter))
                return ValueTask.FromException(new SerializationException($"No serializer registered for response of type {response.ResponseType}"));

            if (formatter.OwnsWriter)
                return formatter.WriteResponseAsync(response, channel, cancellationToken);

            return Core(formatter, response, channel, cancellationToken);

            [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
            static async ValueTask Core(ISphynxResponseFormatterCore formatter, SphynxResponse msg, SphynxChannelWriter.Channel channel,
                CancellationToken token)
            {
                Exception? writeException = null;

                try
                {
                    await formatter.WriteResponseAsync(msg, channel, token).ConfigureAwait(false);
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

        protected override ValueTask<SphynxResponse> ReadResponseAsync(SphynxResponseHeader header, SphynxChannelReader.Channel channel,
            CancellationToken cancellationToken)
        {
            if (!_formatters.TryGetValue(header.ResponseType, out var formatter))
                return ValueTask.FromException<SphynxResponse>(
                    new SerializationException($"No serializer registered for response of type {header.ResponseType}"));

            if (formatter.OwnsReader)
                return formatter.ReadResponseAsync(header, channel, cancellationToken);

            return Core(formatter, header, channel, cancellationToken);

            [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
            static async ValueTask<SphynxResponse> Core(ISphynxResponseFormatterCore formatter, SphynxResponseHeader header,
                SphynxChannelReader.Channel channel, CancellationToken token)
            {
                Exception? readException = null;

                try
                {
                    return await formatter.ReadResponseAsync(header, channel, token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    readException = ex;
                    throw;
                }
                finally
                {
                    await channel.DisposeAsync(readException).ConfigureAwait(false);
                }
            }
        }

        public SphynxResponseSerializer WithFormatter<T>(SphynxResponseFormatter<T> formatter)
            where T : SphynxResponse
        {
            if (formatter.ResponseType != null)
            {
                _formatters[formatter.ResponseType.Value] = formatter;
            }
            else
            {
                foreach (var respTypes in _respTypes)
                    _formatters[respTypes] = formatter;
            }

            return this;
        }

        public SphynxResponseSerializer WithoutFormatter(SphynxRequestType responseType)
        {
            _formatters.Remove(responseType);
            return this;
        }
    }
}
