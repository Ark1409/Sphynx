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
        protected override void SerializeInternal(KickUserRequest packet, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(packet.RoomId);
            serializer.WriteSnowflakeId(packet.KickId);
        }

        protected override KickUserRequest DeserializeInternal( ref BinaryDeserializer deserializer, in RequestInfo requestInfo)
        {
            var roomId = deserializer.ReadSnowflakeId();
            var kickId = deserializer.ReadSnowflakeId();

            return new KickUserRequest(requestInfo.AccessToken, roomId, kickId);
        }
    }

    public class KickUserResponseSerializer : ResponseSerializer<KickUserResponse>
    {
        protected override void SerializeInternal(KickUserResponse packet, ref BinarySerializer serializer)
        {
        }

        protected override KickUserResponse DeserializeInternal( ref BinaryDeserializer deserializer, in ResponseInfo responseInfo)
        {
            return new KickUserResponse(responseInfo.ErrorInfo);
        }
    }

    public class UserKickedBroadcastSerializer : PacketSerializer<UserKickedBroadcast>
    {
        public override void Serialize(UserKickedBroadcast packet, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(packet.RoomId);
            serializer.WriteSnowflakeId(packet.KickId);
        }

        public override UserKickedBroadcast Deserialize(ref BinaryDeserializer deserializer)
        {
            var roomId = deserializer.ReadSnowflakeId();
            var kickId = deserializer.ReadSnowflakeId();

            return new UserKickedBroadcast(roomId, kickId);
        }
    }
}
