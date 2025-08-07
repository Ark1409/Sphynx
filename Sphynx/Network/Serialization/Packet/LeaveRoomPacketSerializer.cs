// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.Network.PacketV2.Broadcast;
using Sphynx.Network.PacketV2.Request;
using Sphynx.Network.PacketV2.Response;

namespace Sphynx.Network.Serialization.Packet
{
    public class LeaveRoomRequestSerializer : RequestSerializer<LeaveRoomRequest>
    {
        protected override void SerializeInternal(LeaveRoomRequest packet, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(packet.RoomId);
        }

        protected override LeaveRoomRequest DeserializeInternal(ref BinaryDeserializer deserializer, in RequestInfo requestInfo)
        {
            var roomId = deserializer.ReadSnowflakeId();

            return new LeaveRoomRequest(requestInfo.AccessToken, roomId);
        }
    }

    public class LeaveRoomResponseSerializer : ResponseSerializer<LeaveRoomResponse>
    {
        protected override void SerializeInternal(LeaveRoomResponse packet, ref BinarySerializer serializer)
        {
        }

        protected override LeaveRoomResponse DeserializeInternal(ref BinaryDeserializer deserializer, in ResponseInfo responseInfo)
        {
            return new LeaveRoomResponse(responseInfo.ErrorInfo);
        }
    }

    public class LeftRoomBroadcastSerializer : PacketSerializer<LeftRoomBroadcast>
    {
        public override void Serialize(LeftRoomBroadcast packet, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(packet.RoomId);
            serializer.WriteSnowflakeId(packet.LeaverId);
        }

        public override LeftRoomBroadcast Deserialize(ref BinaryDeserializer deserializer)
        {
            var roomId = deserializer.ReadSnowflakeId();
            var leaverId = deserializer.ReadSnowflakeId();

            return new LeftRoomBroadcast(roomId, leaverId);
        }
    }
}
