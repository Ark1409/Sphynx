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
        protected override void SerializeRequest(LeaveRoomRequest packet, ref BinarySerializer serializer)
        {
            serializer.WriteGuid(packet.RoomId);
        }

        protected override LeaveRoomRequest DeserializeRequest(ref BinaryDeserializer deserializer, in RequestInfo requestInfo)
        {
            var roomId = deserializer.ReadGuid();

            return new LeaveRoomRequest(requestInfo.SessionId, roomId);
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
            serializer.WriteGuid(packet.RoomId);
            serializer.WriteGuid(packet.LeaverId);
        }

        public override LeftRoomBroadcast Deserialize(ref BinaryDeserializer deserializer)
        {
            var roomId = deserializer.ReadGuid();
            var leaverId = deserializer.ReadGuid();

            return new LeftRoomBroadcast(roomId, leaverId);
        }
    }
}
