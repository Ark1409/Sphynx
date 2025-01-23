// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.Serialization;
using Sphynx.Core;
using Sphynx.ModelV2;
using Sphynx.Network.PacketV2.Request;
using Sphynx.Network.PacketV2.Response;
using Sphynx.Network.Serialization.Model;

namespace Sphynx.Network.Serialization.Packet
{
    public class GetMessagesRequestPacketSerializer : RequestPacketSerializer<GetMessagesRequestPacket>
    {
        protected override int GetMaxPacketSizeInternal(GetMessagesRequestPacket packet)
        {
            return BinarySerializer.MaxSizeOf<SnowflakeId>() + BinarySerializer.MaxSizeOf<SnowflakeId>() +
                   BinarySerializer.MaxSizeOf<int>() + BinarySerializer.MaxSizeOf<bool>();
        }

        protected override void SerializeInternal(GetMessagesRequestPacket packet, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(packet.SinceId);
            serializer.WriteSnowflakeId(packet.RoomId);
            serializer.WriteInt32(packet.Count);
            serializer.WriteBool(packet.Inclusive);
        }

        protected override GetMessagesRequestPacket DeserializeInternal(
            ref BinaryDeserializer deserializer,
            RequestPacketInfo requestInfo)
        {
            var sinceId = deserializer.ReadSnowflakeId();
            var roomId = deserializer.ReadSnowflakeId();
            int count = deserializer.ReadInt32();
            bool inclusive = deserializer.ReadBool();

            return new GetMessagesRequestPacket(requestInfo.UserId, requestInfo.SessionId,
                sinceId, roomId, count, inclusive);
        }
    }

    public class GetMessagesResponsePacketSerializer : ResponsePacketSerializer<GetMessagesResponsePacket>
    {
        private readonly ITypeSerializer<IChatMessage[]> _chatMessageSerializer;

        public GetMessagesResponsePacketSerializer(ITypeSerializer<IChatMessage[]> chatMessageSerializer)
        {
            _chatMessageSerializer = chatMessageSerializer;
        }

        protected override int GetMaxPacketSizeInternal(GetMessagesResponsePacket packet)
        {
            if (packet.ErrorCode != SphynxErrorCode.SUCCESS)
                return 0;

            return _chatMessageSerializer.GetMaxSize(packet.Messages!);
        }

        protected override void SerializeInternal(GetMessagesResponsePacket packet, ref BinarySerializer serializer)
        {
            // Only send data on success
            if (packet.ErrorCode != SphynxErrorCode.SUCCESS)
                return;

            if (!_chatMessageSerializer.TrySerializeUnsafe(packet.Messages!, ref serializer))
                throw new SerializationException($"Could not serialize {nameof(GetMessagesResponsePacket)}");
        }

        protected override GetMessagesResponsePacket DeserializeInternal(
            ref BinaryDeserializer deserializer,
            ResponsePacketInfo responseInfo)
        {
            if (responseInfo.ErrorCode != SphynxErrorCode.SUCCESS)
                return new GetMessagesResponsePacket(responseInfo.ErrorCode);

            if (_chatMessageSerializer.TryDeserialize(ref deserializer, out var messages))
                return new GetMessagesResponsePacket(messages);

            throw new SerializationException($"Could not deserialize {nameof(GetMessagesResponsePacket)}");
        }
    }
}
