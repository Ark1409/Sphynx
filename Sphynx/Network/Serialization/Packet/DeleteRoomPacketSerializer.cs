// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.Network.PacketV2.Broadcast;
using Sphynx.Network.PacketV2.Request;
using Sphynx.Network.PacketV2.Response;

namespace Sphynx.Network.Serialization.Packet
{
    public class DeleteRoomRequestSerializer : RequestSerializer<RoomDeleteRequest>
    {
        protected override void SerializeInternal(RoomDeleteRequest packet, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(packet.RoomId);
            serializer.WriteString(packet.Password);
        }

        protected override RoomDeleteRequest DeserializeInternal(ref BinaryDeserializer deserializer, in RequestInfo requestInfo)
        {
            var roomId = deserializer.ReadSnowflakeId();
            string? password = deserializer.ReadString();

            return new RoomDeleteRequest(requestInfo.AccessToken, roomId, password);
        }
    }

    public class RoomDeleteResponseSerializer : ResponseSerializer<RoomDeleteResponse>
    {
        protected override void SerializeInternal(RoomDeleteResponse packet, ref BinarySerializer serializer)
        {
        }

        protected override RoomDeleteResponse DeserializeInternal(ref BinaryDeserializer deserializer, in ResponseInfo responseInfo)
        {
            return new RoomDeleteResponse(responseInfo.ErrorInfo);
        }
    }

    public class RoomDeletedBroadcastSerializer : PacketSerializer<RoomDeletedBroadcast>
    {
        public override void Serialize(RoomDeletedBroadcast packet, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(packet.RoomId);
        }

        public override RoomDeletedBroadcast Deserialize(ref BinaryDeserializer deserializer)
        {
            var roomId = deserializer.ReadSnowflakeId();

            return new RoomDeletedBroadcast(roomId);
        }
    }
}
