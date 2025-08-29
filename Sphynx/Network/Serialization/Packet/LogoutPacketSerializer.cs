// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.Network.Packet.Broadcast;
using Sphynx.Network.Packet.Request;
using Sphynx.Network.Packet.Response;

namespace Sphynx.Network.Serialization.Packet
{
    public class LogoutRequestSerializer : RequestSerializer<LogoutRequest>
    {
        protected override void SerializeRequest(LogoutRequest packet, ref BinarySerializer serializer)
        {
            serializer.WriteBool(packet.AllSessions);
        }

        protected override LogoutRequest DeserializeRequest(ref BinaryDeserializer deserializer, in RequestInfo requestInfo)
        {
            return new LogoutRequest(requestInfo.SessionId, deserializer.ReadBool())
            {
                RequestTag = requestInfo.RequestTag
            };
        }
    }

    public class LogoutResponseSerializer : ResponseSerializer<LogoutResponse>
    {
        protected override void SerializeResponse(LogoutResponse packet, ref BinarySerializer serializer)
        {
        }

        protected override LogoutResponse DeserializeResponse(ref BinaryDeserializer deserializer, in ResponseInfo responseInfo)
        {
            return new LogoutResponse(responseInfo.ErrorInfo)
            {
                RequestTag = responseInfo.RequestTag
            };
        }
    }

    public class LogoutBroadcastSerializer : PacketSerializer<LogoutBroadcast>
    {
        public override void Serialize(LogoutBroadcast packet, ref BinarySerializer serializer)
        {
            serializer.WriteGuid(packet.UserId);
        }

        public override LogoutBroadcast Deserialize(ref BinaryDeserializer deserializer)
        {
            var userId = deserializer.ReadGuid();
            return new LogoutBroadcast(userId);
        }
    }
}
