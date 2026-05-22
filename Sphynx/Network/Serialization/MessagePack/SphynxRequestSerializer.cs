// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Sphynx.Network.Packet;
using Sphynx.Network.Packet.Request;
using Sphynx.Network.Transport;

namespace Sphynx.Network.Serialization.MessagePack
{
    public sealed class SphynxRequestSerializer : SphynxRequestFormatter<SphynxRequest>
    {
        private static readonly SphynxRequestType[] _reqTypes = Enum.GetValues<SphynxRequestType>();

        public override bool OwnsWriter => _formatters.Any(f => f.Value.OwnsWriter);
        public override bool OwnsReader => _formatters.Any(f => f.Value.OwnsReader);

        private readonly Dictionary<SphynxRequestType, ISphynxRequestFormatterCore> _formatters = new();

        public SphynxRequestSerializer() : base(null)
        {
        }

        protected override ValueTask WriteRequestAsync(SphynxRequest request, SphynxChannelWriter.Channel channel,
            CancellationToken cancellationToken)
        {
            if (!_formatters.TryGetValue(request.RequestType, out var formatter))
                return ValueTask.FromException(new SerializationException($"No serializer registered for request of type {request.RequestType}"));

            if (formatter.OwnsWriter)
                return formatter.WriteRequestAsync(request, channel, cancellationToken);

            return Core(formatter, request, channel, cancellationToken);

            [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
            static async ValueTask Core(ISphynxRequestFormatterCore formatter, SphynxRequest msg, SphynxChannelWriter.Channel channel,
                CancellationToken token)
            {
                Exception? writeException = null;

                try
                {
                    await formatter.WriteRequestAsync(msg, channel, token).ConfigureAwait(false);
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

        protected override ValueTask<SphynxRequest> ReadRequestAsync(SphynxRequestHeader header, SphynxChannelReader.Channel channel,
            CancellationToken cancellationToken)
        {
            if (!_formatters.TryGetValue(header.RequestType, out var formatter))
                return ValueTask.FromException<SphynxRequest>(
                    new SerializationException($"No serializer registered for request of type {header.RequestType}"));

            if (formatter.OwnsReader)
                return formatter.ReadRequestAsync(header, channel, cancellationToken);

            return Core(formatter, header, channel, cancellationToken);

            [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
            static async ValueTask<SphynxRequest> Core(ISphynxRequestFormatterCore formatter, SphynxRequestHeader header,
                SphynxChannelReader.Channel channel, CancellationToken token)
            {
                Exception? readException = null;

                try
                {
                    return await formatter.ReadRequestAsync(header, channel, token).ConfigureAwait(false);
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

        public SphynxRequestSerializer WithFormatter<T>(SphynxRequestFormatter<T> formatter)
            where T : SphynxRequest
        {
            if (formatter.RequestType != null)
            {
                _formatters[formatter.RequestType.Value] = formatter;
            }
            else
            {
                foreach (var reqTypes in _reqTypes)
                    _formatters[reqTypes] = formatter;
            }

            return this;
        }

        public SphynxRequestSerializer WithoutFormatter(SphynxRequestType requestType)
        {
            _formatters.Remove(requestType);
            return this;
        }
    }
}
