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
            return BinarySerializer.MaxSizeOf(packet.AccessToken) + GetMaxSizeInternal(packet);
        }

        protected abstract int GetMaxSizeInternal(T packet);

        protected sealed override bool Serialize(T packet, ref BinarySerializer serializer)
        {
            serializer.WriteString(packet.AccessToken);

            return SerializeInternal(packet, ref serializer);
        }

        protected abstract bool SerializeInternal(T packet, ref BinarySerializer serializer);

        protected sealed override T? Deserialize(ref BinaryDeserializer deserializer)
        {
            var accessToken = deserializer.ReadString();
            var requestInfo = new RequestInfo { AccessToken = accessToken };

            return DeserializeInternal(ref deserializer, requestInfo);
        }

        protected abstract T? DeserializeInternal(ref BinaryDeserializer deserializer, RequestInfo requestInfo);
    }

    public readonly struct RequestInfo
    {
        public string AccessToken { get; init; }
    }
}
