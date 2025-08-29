// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Network.Packet.Broadcast;
using Sphynx.Network.Packet.Request;
using Sphynx.Network.Packet.Response;

namespace Sphynx.Network.Serialization.Packet
{
    public class DeleteRoomRequestSerializer : RequestSerializer<RoomDeleteRequest>
    {
        protected override void SerializeRequest(RoomDeleteRequest packet, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(packet.RoomId);
            serializer.WriteString(packet.Password);
        }

        protected override RoomDeleteRequest DeserializeRequest(ref BinaryDeserializer deserializer, in RequestInfo requestInfo)
        {
            var roomId = deserializer.ReadSnowflakeId();
            string? password = deserializer.ReadString();

            return new RoomDeleteRequest(requestInfo.SessionId, roomId, password)
            {
                RequestTag = requestInfo.RequestTag
            };
        }
    }

    public class RoomDeleteResponseSerializer : ResponseSerializer<RoomDeleteResponse>
    {
        protected override void SerializeResponse(RoomDeleteResponse packet, ref BinarySerializer serializer)
        {
        }

        protected override RoomDeleteResponse DeserializeResponse(ref BinaryDeserializer deserializer, in ResponseInfo responseInfo)
        {
            return new RoomDeleteResponse(responseInfo.ErrorInfo)
            {
                RequestTag = responseInfo.RequestTag
            };
        }
    }

    public class RoomDeletedBroadcastSerializer : PacketSerializer<RoomDeletedBroadcast>
    {
        public override void Serialize(RoomDeletedBroadcast packet, ref BinarySerializer serializer)
        {
            serializer.WriteGuid(packet.RoomId);
        }

        public override RoomDeletedBroadcast Deserialize(ref BinaryDeserializer deserializer)
        {
            var roomId = deserializer.ReadGuid();

            return new RoomDeletedBroadcast(roomId);
        }
    }
}
