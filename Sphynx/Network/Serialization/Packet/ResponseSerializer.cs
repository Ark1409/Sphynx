// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.Network.PacketV2.Response;

namespace Sphynx.Network.Serialization.Packet
{
    public abstract class ResponseSerializer<T> : PacketSerializer<T> where T : SphynxResponse
    {
        public sealed override void Serialize(T packet, ref BinarySerializer serializer)
        {
            serializer.WriteEnum(packet.ErrorInfo.ErrorCode);
            serializer.WriteString(packet.ErrorInfo.Message);

            SerializeInternal(packet, ref serializer);
        }

        protected abstract void SerializeInternal(T packet, ref BinarySerializer serializer);

        public sealed override T? Deserialize(ref BinaryDeserializer deserializer)
        {
            var errorCode = deserializer.ReadEnum<SphynxErrorCode>();
            string? errorMessage = deserializer.ReadString();

            var responseInfo = new ResponseInfo { ErrorInfo = new SphynxErrorInfo(errorCode, errorMessage) };

            return DeserializeInternal(ref deserializer, responseInfo);
        }

        protected abstract T? DeserializeInternal(ref BinaryDeserializer deserializer, in ResponseInfo responseInfo);
    }

    public readonly struct ResponseInfo
    {
        public SphynxErrorInfo ErrorInfo { get; init; }
    }
}
