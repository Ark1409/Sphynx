// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Network.Packet;
using Sphynx.Network.Transport;

namespace Sphynx.Network.Serialization
{
    public interface IMessageFormatter
    {
        /// <summary>
        /// Whether ownership of the provided <see cref="SphynxChannelWriter.Channel"/> should be given to this formatter
        /// after <see cref="SerializeAsync"/> has been called.
        /// </summary>
        bool OwnsWriter { get; }

        /// <summary>
        /// Whether ownership of the provided <see cref="SphynxChannelReader.Channel"/> should be given to this formatter after
        /// <see cref="DeserializeAsync"/> has been called.
        /// </summary>
        bool OwnsReader { get; }

        ValueTask SerializeAsync(SphynxMessage message, SphynxChannelWriter.Channel channel, CancellationToken cancellationToken = default);
        ValueTask<SphynxMessage> DeserializeAsync(SphynxChannelReader.Channel channel, CancellationToken cancellationToken = default);
    }

    public interface IMessageFormatter<T> : IMessageFormatter where T : SphynxMessage
    {
        /// <summary>
        /// The (possibly polymorphic) CLR message type accepted by this formatter.
        /// </summary>
        Type FormatterType => typeof(T);

        ValueTask SerializeAsync(T message, SphynxChannelWriter.Channel channel, CancellationToken cancellationToken = default);
        new ValueTask<T> DeserializeAsync(SphynxChannelReader.Channel channel, CancellationToken cancellationToken = default);
    }

    public abstract class MessageFormatter<T> : IMessageFormatter<T> where T : SphynxMessage
    {
        public Type FormatterType => typeof(T);
        public virtual bool OwnsWriter => false;
        public virtual bool OwnsReader => false;

        public abstract ValueTask SerializeAsync(T message, SphynxChannelWriter.Channel channel, CancellationToken cancellationToken = default);
        public abstract ValueTask<T> DeserializeAsync(SphynxChannelReader.Channel channel, CancellationToken cancellationToken = default);

        ValueTask IMessageFormatter.SerializeAsync(SphynxMessage message, SphynxChannelWriter.Channel channel, CancellationToken cancellationToken)
            => SerializeAsync((T)message, channel, cancellationToken);

        ValueTask<SphynxMessage> IMessageFormatter.DeserializeAsync(SphynxChannelReader.Channel channel, CancellationToken cancellationToken)
        {
            var task = DeserializeAsync(channel, cancellationToken);

            if (task.IsCompletedSuccessfully)
                return ValueTask.FromResult<SphynxMessage>(task.Result);

            return Await(task);

            static async ValueTask<SphynxMessage> Await(ValueTask<T> t) => await t.ConfigureAwait(false);
        }
    }
}
