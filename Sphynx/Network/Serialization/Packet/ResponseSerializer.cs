// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.Network.Packet.Response;

namespace Sphynx.Network.Serialization.Packet
{
    public abstract class ResponseSerializer<T> : PacketSerializer<T> where T : SphynxResponse
    {
        public sealed override void Serialize(T packet, ref BinarySerializer serializer)
        {
            serializer.WriteGuid(packet.RequestTag);
            serializer.WriteEnum(packet.ErrorInfo.ErrorCode);
            serializer.WriteString(packet.ErrorInfo.Message);

            SerializeResponse(packet, ref serializer);
        }

        protected abstract void SerializeResponse(T packet, ref BinarySerializer serializer);

        public sealed override T? Deserialize(ref BinaryDeserializer deserializer)
        {
            var requestTag = deserializer.ReadGuid();
            var errorCode = deserializer.ReadEnum<SphynxErrorCode>();
            string? errorMessage = deserializer.ReadString();

            var responseInfo = new ResponseInfo { RequestTag = requestTag, ErrorInfo = new SphynxErrorInfo(errorCode, errorMessage) };

            return DeserializeResponse(ref deserializer, responseInfo);
        }

        protected abstract T? DeserializeResponse(ref BinaryDeserializer deserializer, in ResponseInfo responseInfo);
    }

    public readonly struct ResponseInfo
    {
        public Guid RequestTag { get; init; }
        public SphynxErrorInfo ErrorInfo { get; init; }
    }
}
