// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.Network.PacketV2.Broadcast;
using Sphynx.Network.PacketV2.Request;
using Sphynx.Network.PacketV2.Response;

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

            return new KickUserRequest(requestInfo.SessionId, roomId, kickId);
        }
    }

    public class KickUserResponseSerializer : ResponseSerializer<KickUserResponse>
    {
        protected override void SerializeInternal(KickUserResponse packet, ref BinarySerializer serializer)
        {
        }

        protected override KickUserResponse DeserializeInternal(ref BinaryDeserializer deserializer, in ResponseInfo responseInfo)
        {
            return new KickUserResponse(responseInfo.ErrorInfo);
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
