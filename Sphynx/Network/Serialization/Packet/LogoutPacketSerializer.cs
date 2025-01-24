// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.Network.PacketV2.Broadcast;
using Sphynx.Network.PacketV2.Request;
using Sphynx.Network.PacketV2.Response;

namespace Sphynx.Network.Serialization.Packet
{
    public class LogoutRequestPacketSerializer : RequestPacketSerializer<LogoutRequestPacket>
    {
        protected override int GetMaxSizeInternal(LogoutRequestPacket packet)
        {
            return 0;
        }

        protected override void SerializeInternal(LogoutRequestPacket packet, ref BinarySerializer serializer)
        {
        }

        protected override LogoutRequestPacket DeserializeInternal(
            ref BinaryDeserializer deserializer,
            RequestPacketInfo requestInfo)
        {
            return new LogoutRequestPacket(requestInfo.UserId, requestInfo.SessionId);
        }
    }

    public class LogoutResponsePacketSerializer : ResponsePacketSerializer<LogoutResponsePacket>
    {
        protected override int GetMaxPacketSizeInternal(LogoutResponsePacket packet)
        {
            return 0;
        }

        protected override void SerializeInternal(LogoutResponsePacket packet, ref BinarySerializer serializer)
        {
        }

        protected override LogoutResponsePacket DeserializeInternal(
            ref BinaryDeserializer deserializer,
            ResponsePacketInfo responseInfo)
        {
            return new LogoutResponsePacket(responseInfo.ErrorCode);
        }
    }

    public class LogoutBroadcastPacketSerializer : PacketSerializer<LogoutBroadcastPacket>
    {
        public override int GetMaxSize(LogoutBroadcastPacket packet)
        {
            return BinarySerializer.MaxSizeOf<SnowflakeId>();
        }

        protected override void Serialize(LogoutBroadcastPacket packet, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(packet.UserId);
        }

        protected override LogoutBroadcastPacket Deserialize(ref BinaryDeserializer deserializer)
        {
            var userId = deserializer.ReadSnowflakeId();
            return new LogoutBroadcastPacket(userId);
        }
    }
}
