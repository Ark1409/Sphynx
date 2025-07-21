// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.Network.PacketV2.Broadcast;
using Sphynx.Network.PacketV2.Request;
using Sphynx.Network.PacketV2.Response;

namespace Sphynx.Network.Serialization.Packet
{
    public class LogoutRequestSerializer : RequestSerializer<LogoutRequest>
    {
        protected override int GetMaxSizeInternal(LogoutRequest packet)
        {
            return 0;
        }

        protected override bool SerializeInternal(LogoutRequest packet, ref BinarySerializer serializer)
        {
            return true;
        }

        protected override LogoutRequest DeserializeInternal(ref BinaryDeserializer deserializer, RequestInfo requestInfo)
        {
            return new LogoutRequest(requestInfo.AccessToken);
        }
    }

    public class LogoutResponseSerializer : ResponseSerializer<LogoutResponse>
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
            return new LogoutResponse(responseInfo.ErrorInfo);
        }
    }

    public class LogoutBroadcastSerializer : PacketSerializer<LogoutBroadcast>
    {
        public override int GetMaxSize(LogoutBroadcast packet)
        {
            return BinarySerializer.MaxSizeOf<SnowflakeId>();
        }

        protected override bool Serialize(LogoutBroadcast packet, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(packet.UserId);
            return true;
        }

        protected override LogoutBroadcast Deserialize(ref BinaryDeserializer deserializer)
        {
            var userId = deserializer.ReadSnowflakeId();
            return new LogoutBroadcast(userId);
        }
    }
}
