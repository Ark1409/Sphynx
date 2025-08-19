// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.ModelV2;
using Sphynx.Network.PacketV2.Request;
using Sphynx.Network.PacketV2.Response;
using Sphynx.Network.Serialization.Model;

namespace Sphynx.Network.Serialization.Packet
{
    public class FetchMessagesRequestSerializer : RequestSerializer<FetchMessagesRequest>
    {
        protected override void SerializeInternal(FetchMessagesRequest packet, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(packet.BeforeId);
            serializer.WriteSnowflakeId(packet.RoomId);
            serializer.WriteInt32(packet.Count);
            serializer.WriteBool(packet.Inclusive);
        }

        protected override FetchMessagesRequest DeserializeInternal(ref BinaryDeserializer deserializer, in RequestInfo requestInfo)
        {
            var sinceId = deserializer.ReadSnowflakeId();
            var roomId = deserializer.ReadSnowflakeId();
            int count = deserializer.ReadInt32();
            bool inclusive = deserializer.ReadBool();

            return new FetchMessagesRequest(requestInfo.AccessToken, sinceId, roomId, count, inclusive);
        }
    }

    public class FetchMessagesResponseSerializer : ResponseSerializer<FetchMessagesResponse>
    {
        private readonly ITypeSerializer<ChatMessage[]> _chatMessageSerializer;

        public FetchMessagesResponseSerializer(ITypeSerializer<ChatMessage> chatMessageSerializer)
            : this(new ArraySerializer<ChatMessage>(chatMessageSerializer))
        {
        }

        public FetchMessagesResponseSerializer(ITypeSerializer<ChatMessage[]> chatMessageSerializer)
        {
            _chatMessageSerializer = chatMessageSerializer;
        }

        protected override void SerializeInternal(FetchMessagesResponse packet, ref BinarySerializer serializer)
        {
            // Only send data on success
            if (packet.ErrorInfo != SphynxErrorCode.SUCCESS)
                return;

            _chatMessageSerializer.Serialize(packet.Messages!, ref serializer);
        }

        protected override FetchMessagesResponse? DeserializeInternal(ref BinaryDeserializer deserializer, in ResponseInfo responseInfo)
        {
            if (responseInfo.ErrorInfo != SphynxErrorCode.SUCCESS)
                return new FetchMessagesResponse(responseInfo.ErrorInfo);

            var messages = _chatMessageSerializer.Deserialize(ref deserializer)!;

            return new FetchMessagesResponse(messages);
        }
    }
}
