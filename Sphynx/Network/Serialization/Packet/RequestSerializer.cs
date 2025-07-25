// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.Network.PacketV2.Request;

namespace Sphynx.Network.Serialization.Packet
{
    public abstract class RequestSerializer<T> : PacketSerializer<T> where T : SphynxRequest
    {
        public sealed override int GetMaxSize(T packet)
        {
            return BinarySerializer.MaxSizeOf<SnowflakeId>() + BinarySerializer.MaxSizeOf<Guid>() +
                   GetMaxSizeInternal(packet);
        }

        protected abstract int GetMaxSizeInternal(T packet);

        protected sealed override bool Serialize(T packet, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(packet.UserId);
            serializer.WriteGuid(packet.SessionId);

            return SerializeInternal(packet, ref serializer);
        }

        protected abstract bool SerializeInternal(T packet, ref BinarySerializer serializer);

        protected sealed override T? Deserialize(ref BinaryDeserializer deserializer)
        {
            var userId = deserializer.ReadSnowflakeId();
            var sessionId = deserializer.ReadGuid();
            var requestInfo = new RequestInfo { UserId = userId, SessionId = sessionId };

            return DeserializeInternal(ref deserializer, requestInfo);
        }

        protected abstract T? DeserializeInternal(ref BinaryDeserializer deserializer, RequestInfo requestInfo);
    }

    public readonly struct RequestInfo
    {
        public SnowflakeId UserId { get; init; }
        public Guid SessionId { get; init; }
    }
}
