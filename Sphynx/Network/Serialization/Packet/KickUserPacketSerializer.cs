// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.Network.Packet.Broadcast;
using Sphynx.Network.Packet.Request;
using Sphynx.Network.Packet.Response;

namespace Sphynx.Network.Serialization.Packet
{
    public class KickUserRequestSerializer : RequestSerializer<KickUserRequest>
    {
        protected override void SerializeRequest(KickUserRequest packet, ref BinarySerializer serializer)
        {
            serializer.WriteGuid(packet.RoomId);
            serializer.WriteGuid(packet.KickId);
        }

        protected override KickUserRequest DeserializeRequest(ref BinaryDeserializer deserializer, in RequestInfo requestInfo)
        {
            var roomId = deserializer.ReadGuid();
            var kickId = deserializer.ReadGuid();

            return new KickUserRequest(requestInfo.SessionId, roomId, kickId)
            {
                RequestTag = requestInfo.RequestTag
            };
        }
    }

    public class KickUserResponseSerializer : ResponseSerializer<KickUserResponse>
    {
        protected override void SerializeResponse(KickUserResponse packet, ref BinarySerializer serializer)
        {
        }

        protected override KickUserResponse DeserializeResponse(ref BinaryDeserializer deserializer, in ResponseInfo responseInfo)
        {
            return new KickUserResponse(responseInfo.ErrorInfo)
            {
                RequestTag = responseInfo.RequestTag
            };
        }
    }

    public class UserKickedBroadcastSerializer : PacketSerializer<UserKickedBroadcast>
    {
        public override void Serialize(UserKickedBroadcast packet, ref BinarySerializer serializer)
        {
            serializer.WriteGuid(packet.RoomId);
            serializer.WriteGuid(packet.KickId);
        }

        public override UserKickedBroadcast Deserialize(ref BinaryDeserializer deserializer)
        {
            var roomId = deserializer.ReadGuid();
            var kickId = deserializer.ReadGuid();

            return new UserKickedBroadcast(roomId, kickId);
        }
    }
}
