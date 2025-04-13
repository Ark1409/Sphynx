// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.Network.PacketV2.Broadcast;
using Sphynx.Network.PacketV2.Request;
using Sphynx.Network.PacketV2.Response;

namespace Sphynx.Network.Serialization.Packet
{
    public class LogoutRequestPacketSerializer : RequestPacketSerializer<LogoutRequest>
    {
        protected override int GetMaxSizeInternal(LogoutRequest packet)
        {
            return 0;
        }

        protected override bool SerializeInternal(LogoutRequest packet, ref BinarySerializer serializer)
        {
            return true;
        }

        protected override LogoutRequest DeserializeInternal(
            ref BinaryDeserializer deserializer,
            RequestInfo requestInfo)
        {
            return new LogoutRequest(requestInfo.UserId, requestInfo.SessionId);
        }
    }

    public class LogoutResponsePacketSerializer : ResponsePacketSerializer<LogoutResponse>
    {
        protected override int GetMaxSizeInternal(LogoutResponse packet)
        {
            return 0;
        }

        protected override bool SerializeInternal(LogoutResponse packet, ref BinarySerializer serializer)
        {
            return true;
        }

        protected override LogoutResponse DeserializeInternal(
            ref BinaryDeserializer deserializer,
            ResponseInfo responseInfo)
        {
            return new LogoutResponse(responseInfo.ErrorCode);
        }
    }

    public class LogoutBroadcastPacketSerializer : PacketSerializer<LogoutBroadcastPacket>
    {
        public override int GetMaxSize(LogoutBroadcastPacket packet)
        {
            return BinarySerializer.MaxSizeOf<SnowflakeId>();
        }

        protected override bool Serialize(LogoutBroadcastPacket packet, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(packet.UserId);
            return true;
        }

        protected override LogoutBroadcastPacket Deserialize(ref BinaryDeserializer deserializer)
        {
            var userId = deserializer.ReadSnowflakeId();
            return new LogoutBroadcastPacket(userId);
        }
    }
}
