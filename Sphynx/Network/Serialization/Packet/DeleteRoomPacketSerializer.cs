// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.Network.PacketV2.Broadcast;
using Sphynx.Network.PacketV2.Request;
using Sphynx.Network.PacketV2.Response;

namespace Sphynx.Network.Serialization.Packet
{
    public class DeleteRoomRequestPacketSerializer : RequestPacketSerializer<RoomDeleteRequest>
    {
        protected override int GetMaxSizeInternal(RoomDeleteRequest packet)
        {
            return BinarySerializer.MaxSizeOf<SnowflakeId>() + BinarySerializer.MaxSizeOf(packet.Password);
        }

        protected override bool SerializeInternal(RoomDeleteRequest packet, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(packet.RoomId);
            serializer.WriteString(packet.Password);
            return true;
        }

        protected override RoomDeleteRequest DeserializeInternal(
            ref BinaryDeserializer deserializer,
            RequestInfo requestInfo)
        {
            var roomId = deserializer.ReadSnowflakeId();
            string password = deserializer.ReadString();

            return new RoomDeleteRequest(requestInfo.UserId, requestInfo.SessionId, roomId,
                string.IsNullOrEmpty(password) ? null : password);
        }
    }

    public class DeleteRoomResponsePacketSerializer : ResponsePacketSerializer<DeleteRoomResponsePacket>
    {
        protected override int GetMaxSizeInternal(DeleteRoomResponsePacket packet)
        {
            return 0;
        }

        protected override bool SerializeInternal(DeleteRoomResponsePacket packet, ref BinarySerializer serializer)
        {
            return true;
        }

        protected override DeleteRoomResponsePacket DeserializeInternal(
            ref BinaryDeserializer deserializer,
            ResponseInfo responseInfo)
        {
            return new DeleteRoomResponsePacket(responseInfo.ErrorCode);
        }
    }

    public class DeleteRoomBroadcastPacketSerializer : PacketSerializer<DeleteRoomBroadcastPacket>
    {
        public override int GetMaxSize(DeleteRoomBroadcastPacket packet)
        {
            return BinarySerializer.MaxSizeOf<SnowflakeId>();
        }

        protected override bool Serialize(DeleteRoomBroadcastPacket packet, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(packet.RoomId);
            return true;
        }

        protected override DeleteRoomBroadcastPacket Deserialize(ref BinaryDeserializer deserializer)
        {
            var roomId = deserializer.ReadSnowflakeId();

            return new DeleteRoomBroadcastPacket(roomId);
        }
    }
}
