// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Network.PacketV2.Request;

namespace Sphynx.Network.Serialization.Packet
{
    public abstract class RequestSerializer<T> : PacketSerializer<T> where T : SphynxRequest
    {
        public sealed override void Serialize(T packet, ref BinarySerializer serializer)
        {
            serializer.WriteString(packet.AccessToken);

            SerializeInternal(packet, ref serializer);
        }

        protected abstract void SerializeInternal(T packet, ref BinarySerializer serializer);

        public sealed override T? Deserialize(ref BinaryDeserializer deserializer)
        {
            string accessToken = deserializer.ReadString()!;
            var requestInfo = new RequestInfo { AccessToken = accessToken };

            return DeserializeInternal(ref deserializer, in requestInfo);
        }

        protected abstract T? DeserializeInternal(ref BinaryDeserializer deserializer, in RequestInfo requestInfo);
    }

    public readonly struct RequestInfo
    {
        public string AccessToken { get; init; }
    }
}
