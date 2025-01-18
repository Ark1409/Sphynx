// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.Network.PacketV2.Response;

namespace Sphynx.Network.Serialization.Packet
{
    public abstract class ResponsePacketSerializer<T> : PacketSerializer<T> where T : SphynxResponsePacket
    {
        protected sealed override int GetMaxPacketSize(T packet)
        {
            return BinarySerializer.MaxSizeOf<SphynxErrorCode>() + GetMaxPacketSizeInternal(packet);
        }

        protected abstract int GetMaxPacketSizeInternal(T packet);

        protected sealed override void Serialize(T packet, ref BinarySerializer serializer)
        {
            serializer.WriteEnum(packet.ErrorCode);

            SerializeInternal(packet, ref serializer);
        }

        protected abstract void SerializeInternal(T packet, ref BinarySerializer serializer);

        protected sealed override T Deserialize(ref BinaryDeserializer deserializer)
        {
            var errorCode = deserializer.ReadEnum<SphynxErrorCode>();
            var responseInfo = new ResponsePacketInfo { ErrorCode = errorCode };

            return DeserializeInternal(ref deserializer, responseInfo);
        }

        protected abstract T DeserializeInternal(ref BinaryDeserializer deserializer, ResponsePacketInfo responseInfo);
    }

    public readonly struct ResponsePacketInfo
    {
        public SphynxErrorCode ErrorCode { get; init; }
    }
}
