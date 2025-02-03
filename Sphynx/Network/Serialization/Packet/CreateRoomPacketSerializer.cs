// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.ModelV2.Room;
using Sphynx.Network.PacketV2.Request;
using Sphynx.Network.PacketV2.Response;

namespace Sphynx.Network.Serialization.Packet
{
    public abstract class CreateRoomRequestPacketSerializer<TRoom> : RequestPacketSerializer<TRoom>
        where TRoom : CreateRoomRequestPacket
    {
        protected sealed override int GetMaxSizeInternal(TRoom packet)
        {
            return BinarySerializer.MaxSizeOf<ChatRoomType>() + GetMaxRoomSize(packet);
        }

        protected abstract int GetMaxRoomSize(TRoom room);

        protected sealed override bool SerializeInternal(TRoom packet, ref BinarySerializer serializer)
        {
            serializer.WriteEnum(packet.RoomType);
            return SerializeRoom(packet, ref serializer);
        }

        protected abstract bool SerializeRoom(TRoom packet, ref BinarySerializer serializer);

        protected sealed override TRoom? DeserializeInternal(
            ref BinaryDeserializer deserializer,
            RequestPacketInfo requestInfo)
        {
            var roomType = deserializer.ReadEnum<ChatRoomType>();
            var roomInfo = new RequestRoomInfo { RoomType = roomType };

            return DeserializeRoom(ref deserializer, requestInfo, roomInfo);
        }

        protected abstract TRoom? DeserializeRoom(
            ref BinaryDeserializer deserializer,
            RequestPacketInfo requestInfo,
            RequestRoomInfo roomInfo);

        protected readonly struct RequestRoomInfo
        {
            public ChatRoomType RoomType { get; init; }
        }
    }

    public static class CreateRoomRequestPacketSerializer
    {
        #region Default Implementations

        public class Direct : CreateRoomRequestPacketSerializer<CreateRoomRequestPacket.Direct>
        {
            protected override int GetMaxRoomSize(CreateRoomRequestPacket.Direct room)
            {
                return BinarySerializer.MaxSizeOf<SnowflakeId>();
            }

            protected override bool SerializeRoom(
                CreateRoomRequestPacket.Direct packet,
                ref BinarySerializer serializer)
            {
                serializer.WriteSnowflakeId(packet.OtherId);
                return true;
            }

            protected override CreateRoomRequestPacket.Direct DeserializeRoom(
                ref BinaryDeserializer deserializer,
                RequestPacketInfo requestInfo,
                RequestRoomInfo roomInfo)
            {
                var otherId = deserializer.ReadSnowflakeId();

                return new CreateRoomRequestPacket.Direct(requestInfo.UserId, requestInfo.SessionId, otherId);
            }
        }

        public class Group : CreateRoomRequestPacketSerializer<CreateRoomRequestPacket.Group>
        {
            protected override int GetMaxRoomSize(CreateRoomRequestPacket.Group room)
            {
                return BinarySerializer.MaxSizeOf(room.Name) + BinarySerializer.MaxSizeOf(room.Password) +
                       BinarySerializer.MaxSizeOf<bool>();
            }

            protected override bool SerializeRoom(CreateRoomRequestPacket.Group packet, ref BinarySerializer serializer)
            {
                serializer.WriteString(packet.Name);
                serializer.WriteString(packet.Password);
                serializer.WriteBool(packet.Public);
                return true;
            }

            protected override CreateRoomRequestPacket.Group? DeserializeRoom(
                ref BinaryDeserializer deserializer,
                RequestPacketInfo requestInfo,
                RequestRoomInfo roomInfo)
            {
                string name = deserializer.ReadString();
                string password = deserializer.ReadString();
                bool isPublic = deserializer.ReadBool();

                return new CreateRoomRequestPacket.Group(requestInfo.UserId, requestInfo.SessionId,
                    name, password, isPublic);
            }
        }

        #endregion
    }

    public class CreateRoomResponsePacketSerializer : ResponsePacketSerializer<CreateRoomResponsePacket>
    {
        protected override int GetMaxSizeInternal(CreateRoomResponsePacket packet)
        {
            if (packet.ErrorCode != SphynxErrorCode.SUCCESS)
                return 0;

            return BinarySerializer.MaxSizeOf<SnowflakeId>();
        }

        protected override bool SerializeInternal(CreateRoomResponsePacket packet, ref BinarySerializer serializer)
        {
            if (packet.ErrorCode != SphynxErrorCode.SUCCESS)
                return true;

            serializer.WriteSnowflakeId(packet.RoomId!.Value);
            return true;
        }

        protected override CreateRoomResponsePacket DeserializeInternal(
            ref BinaryDeserializer deserializer,
            ResponsePacketInfo responseInfo)
        {
            if (responseInfo.ErrorCode != SphynxErrorCode.SUCCESS)
                return new CreateRoomResponsePacket(responseInfo.ErrorCode);

            var roomId = deserializer.ReadSnowflakeId();
            return new CreateRoomResponsePacket(roomId);
        }
    }
}
