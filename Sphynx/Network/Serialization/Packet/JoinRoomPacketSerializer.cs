// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.ModelV2.Room;
using Sphynx.Network.PacketV2.Broadcast;
using Sphynx.Network.PacketV2.Request;
using Sphynx.Network.PacketV2.Response;
using Sphynx.Network.Serialization.Model;

namespace Sphynx.Network.Serialization.Packet
{
    public class JoinRoomRequestPacketSerializer : RequestPacketSerializer<JoinRoomRequestPacket>
    {
        protected override int GetMaxSizeInternal(JoinRoomRequestPacket packet)
        {
            return BinarySerializer.MaxSizeOf<SnowflakeId>() + BinarySerializer.MaxSizeOf(packet.Password);
        }

        protected override bool SerializeInternal(JoinRoomRequestPacket packet, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(packet.RoomId);
            serializer.WriteString(packet.Password);
            return true;
        }

        protected override JoinRoomRequestPacket DeserializeInternal(
            ref BinaryDeserializer deserializer,
            RequestInfo requestInfo)
        {
            var roomId = deserializer.ReadSnowflakeId();
            string password = deserializer.ReadString();

            return new JoinRoomRequestPacket(requestInfo.UserId, requestInfo.SessionId, roomId,
                string.IsNullOrEmpty(password) ? null : password);
        }
    }

    public class JoinRoomResponsePacketSerializer : ResponsePacketSerializer<JoinRoomResponsePacket>
    {
        private readonly ITypeSerializer<IChatRoomInfo> _roomSerializer;

        public JoinRoomResponsePacketSerializer(ITypeSerializer<IChatRoomInfo> roomSerializer)
        {
            _roomSerializer = roomSerializer;
        }

        protected override int GetMaxSizeInternal(JoinRoomResponsePacket packet)
        {
            if (packet.ErrorCode != SphynxErrorCode.SUCCESS)
                return 0;

            return _roomSerializer.GetMaxSize(packet.RoomInfo!);
        }

        protected override bool SerializeInternal(JoinRoomResponsePacket packet, ref BinarySerializer serializer)
        {
            if (packet.ErrorCode != SphynxErrorCode.SUCCESS)
                return true;

            return _roomSerializer.TrySerializeUnsafe(packet.RoomInfo!, ref serializer);
        }

        protected override JoinRoomResponsePacket? DeserializeInternal(
            ref BinaryDeserializer deserializer,
            ResponseInfo responseInfo)
        {
            if (responseInfo.ErrorCode != SphynxErrorCode.SUCCESS)
                return new JoinRoomResponsePacket(responseInfo.ErrorCode);

            return _roomSerializer.TryDeserialize(ref deserializer, out var room)
                ? new JoinRoomResponsePacket(room)
                : null;
        }
    }

    public class JoinRoomBroadcastPacketSerializer : PacketSerializer<JoinRoomBroadcastPacket>
    {
        public override int GetMaxSize(JoinRoomBroadcastPacket packet)
        {
            return BinarySerializer.MaxSizeOf<SnowflakeId>() + BinarySerializer.MaxSizeOf<SnowflakeId>();
        }

        protected override bool Serialize(JoinRoomBroadcastPacket packet, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(packet.RoomId);
            serializer.WriteSnowflakeId(packet.JoinerId);
            return true;
        }

        protected override JoinRoomBroadcastPacket Deserialize(ref BinaryDeserializer deserializer)
        {
            var roomId = deserializer.ReadSnowflakeId();
            var joinerId = deserializer.ReadSnowflakeId();

            return new JoinRoomBroadcastPacket(roomId, joinerId);
        }
    }
}
