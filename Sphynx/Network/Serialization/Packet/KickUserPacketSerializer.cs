// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.Network.PacketV2.Broadcast;
using Sphynx.Network.PacketV2.Request;
using Sphynx.Network.PacketV2.Response;

namespace Sphynx.Network.Serialization.Packet
{
    public class KickUserRequestPacketSerializer : RequestPacketSerializer<KickUserRequest>
    {
        protected override int GetMaxSizeInternal(KickUserRequest packet)
        {
            return BinarySerializer.MaxSizeOf<SnowflakeId>() + BinarySerializer.MaxSizeOf<SnowflakeId>();
        }

        protected override bool SerializeInternal(KickUserRequest packet, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(packet.RoomId);
            serializer.WriteSnowflakeId(packet.KickId);
            return true;
        }

        protected override KickUserRequest DeserializeInternal(
            ref BinaryDeserializer deserializer,
            RequestInfo requestInfo)
        {
            var roomId = deserializer.ReadSnowflakeId();
            var kickId = deserializer.ReadSnowflakeId();

            return new KickUserRequest(requestInfo.UserId, requestInfo.SessionId, roomId, kickId);
        }
    }

    public class KickUserResponsePacketSerializer : ResponsePacketSerializer<KickUserResponsePacket>
    {
        protected override int GetMaxSizeInternal(KickUserResponsePacket packet)
        {
            return 0;
        }

        protected override bool SerializeInternal(KickUserResponsePacket packet, ref BinarySerializer serializer)
        {
            return true;
        }

        protected override KickUserResponsePacket DeserializeInternal(
            ref BinaryDeserializer deserializer,
            ResponseInfo responseInfo)
        {
            return new KickUserResponsePacket(responseInfo.ErrorCode);
        }
    }

    public class KickUserBroadcastPacketSerializer : PacketSerializer<KickUserBroadcastPacket>
    {
        public override int GetMaxSize(KickUserBroadcastPacket packet)
        {
            return BinarySerializer.MaxSizeOf<SnowflakeId>() + BinarySerializer.MaxSizeOf<SnowflakeId>();
        }

        protected override bool Serialize(KickUserBroadcastPacket packet, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(packet.RoomId);
            serializer.WriteSnowflakeId(packet.KickId);
            return true;
        }

        protected override KickUserBroadcastPacket Deserialize(ref BinaryDeserializer deserializer)
        {
            var roomId = deserializer.ReadSnowflakeId();
            var kickId = deserializer.ReadSnowflakeId();

            return new KickUserBroadcastPacket(roomId, kickId);
        }
    }
}
