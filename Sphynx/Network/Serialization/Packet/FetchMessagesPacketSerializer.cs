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
        protected override int GetMaxSizeInternal(FetchMessagesRequest packet)
        {
            return BinarySerializer.MaxSizeOf<SnowflakeId>() + BinarySerializer.MaxSizeOf<SnowflakeId>() +
                   BinarySerializer.MaxSizeOf<int>() + BinarySerializer.MaxSizeOf<bool>();
        }

        protected override bool SerializeInternal(FetchMessagesRequest packet, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(packet.BeforeId);
            serializer.WriteSnowflakeId(packet.RoomId);
            serializer.WriteInt32(packet.Count);
            serializer.WriteBool(packet.Inclusive);
            return true;
        }

        protected override FetchMessagesRequest DeserializeInternal(ref BinaryDeserializer deserializer, RequestInfo requestInfo)
        {
            var sinceId = deserializer.ReadSnowflakeId();
            var roomId = deserializer.ReadSnowflakeId();
            int count = deserializer.ReadInt32();
            bool inclusive = deserializer.ReadBool();

            return new FetchMessagesRequest(requestInfo.UserId, requestInfo.SessionId,
                sinceId, roomId, count, inclusive);
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

        protected override int GetMaxSizeInternal(FetchMessagesResponse packet)
        {
            if (packet.ErrorCode != SphynxErrorCode.SUCCESS)
                return 0;

            return _chatMessageSerializer.GetMaxSize(packet.Messages!);
        }

        protected override bool SerializeInternal(FetchMessagesResponse packet, ref BinarySerializer serializer)
        {
            // Only send data on success
            if (packet.ErrorCode != SphynxErrorCode.SUCCESS)
                return true;

            return _chatMessageSerializer.TrySerializeUnsafe(packet.Messages!, ref serializer);
        }

        protected override FetchMessagesResponse? DeserializeInternal(ref BinaryDeserializer deserializer, ResponseInfo responseInfo)
        {
            if (responseInfo.ErrorCode != SphynxErrorCode.SUCCESS)
                return new FetchMessagesResponse(responseInfo.ErrorCode);

            return _chatMessageSerializer.TryDeserialize(ref deserializer, out var messages)
                ? new FetchMessagesResponse(messages)
                : null;
        }
    }
}
