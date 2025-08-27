// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Network.PacketV2.Request;

namespace Sphynx.Network.Serialization.Packet
{
    public abstract class RequestSerializer<T> : PacketSerializer<T> where T : SphynxRequest
    {
        public sealed override void Serialize(T packet, ref BinarySerializer serializer)
        {
            serializer.WriteGuid(packet.SessionId);

            SerializeRequest(packet, ref serializer);
        }

        protected abstract void SerializeRequest(T packet, ref BinarySerializer serializer);

        public sealed override T? Deserialize(ref BinaryDeserializer deserializer)
        {
            var sessionId = deserializer.ReadGuid();
            var requestInfo = new RequestInfo { SessionId = sessionId };

            return DeserializeRequest(ref deserializer, in requestInfo);
        }

        protected abstract T? DeserializeRequest(ref BinaryDeserializer deserializer, in RequestInfo requestInfo);
    }

    public readonly struct RequestInfo
    {
        public Guid SessionId { get; init; }
    }
}
