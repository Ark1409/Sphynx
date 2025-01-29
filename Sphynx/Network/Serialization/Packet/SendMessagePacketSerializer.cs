// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.Network.PacketV2.Broadcast;
using Sphynx.Network.PacketV2.Request;
using Sphynx.Network.PacketV2.Response;

namespace Sphynx.Network.Serialization.Packet
{
    public class SendMessageRequestPacketSerializer : RequestPacketSerializer<SendMessageRequestPacket>
    {
        protected override int GetMaxSizeInternal(SendMessageRequestPacket packet)
        {
            return BinarySerializer.MaxSizeOf<SnowflakeId>() + BinarySerializer.MaxSizeOf(packet.Message);
        }

        protected override bool SerializeInternal(SendMessageRequestPacket packet, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(packet.RoomId);
            serializer.WriteString(packet.Message);
            return true;
        }

        protected override SendMessageRequestPacket DeserializeInternal(
            ref BinaryDeserializer deserializer,
            RequestPacketInfo requestInfo)
        {
            var roomId = deserializer.ReadSnowflakeId();
            string message = deserializer.ReadString();

            return new SendMessageRequestPacket(requestInfo.UserId, requestInfo.SessionId, roomId, message);
        }
    }

    public class SendMessageResponsePacketSerializer : ResponsePacketSerializer<SendMessageResponsePacket>
    {
        protected override int GetMaxPacketSizeInternal(SendMessageResponsePacket packet)
        {
            return 0;
        }

        protected override bool SerializeInternal(SendMessageResponsePacket packet, ref BinarySerializer serializer)
        {
            return true;
        }

        protected override SendMessageResponsePacket DeserializeInternal(
            ref BinaryDeserializer deserializer,
            ResponsePacketInfo responseInfo)
        {
            return new SendMessageResponsePacket(responseInfo.ErrorCode);
        }
    }

    public class SendMessageBroadcastPacketSerializer : PacketSerializer<SendMessageBroadcastPacket>
    {
        public override int GetMaxSize(SendMessageBroadcastPacket packet)
        {
            return BinarySerializer.MaxSizeOf<SnowflakeId>() + BinarySerializer.MaxSizeOf<SnowflakeId>();
        }

        protected override bool Serialize(SendMessageBroadcastPacket packet, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(packet.RoomId);
            serializer.WriteSnowflakeId(packet.MessageId);
            return true;
        }

        protected override SendMessageBroadcastPacket Deserialize(ref BinaryDeserializer deserializer)
        {
            var roomId = deserializer.ReadSnowflakeId();
            var messageId = deserializer.ReadSnowflakeId();

            return new SendMessageBroadcastPacket(roomId, messageId);
        }
    }
}
