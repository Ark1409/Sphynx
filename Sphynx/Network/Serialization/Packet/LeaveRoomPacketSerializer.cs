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
        protected override int GetMaxSizeInternal(LeaveRoomRequest packet)
        {
            return BinarySerializer.MaxSizeOf<SnowflakeId>();
        }

        protected override bool SerializeInternal(LeaveRoomRequest packet, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(packet.RoomId);
            return true;
        }

        protected override LeaveRoomRequest DeserializeInternal(ref BinaryDeserializer deserializer, RequestInfo requestInfo)
        {
            var roomId = deserializer.ReadSnowflakeId();

            return new LeaveRoomRequest(requestInfo.UserId, requestInfo.SessionId, roomId);
        }
    }

    public class LeaveRoomResponseSerializer : ResponseSerializer<LeaveRoomResponse>
    {
        protected override int GetMaxSizeInternal(LeaveRoomResponse packet)
        {
            return 0;
        }

        protected override bool SerializeInternal(LeaveRoomResponse packet, ref BinarySerializer serializer)
        {
            return true;
        }

        protected override LeaveRoomResponse DeserializeInternal(
            ref BinaryDeserializer deserializer,
            ResponseInfo responseInfo)
        {
            return new LeaveRoomResponse(responseInfo.ErrorCode);
        }
    }

    public class LeftRoomBroadcastSerializer : PacketSerializer<LeftRoomBroadcast>
    {
        public override int GetMaxSize(LeftRoomBroadcast packet)
        {
            return BinarySerializer.MaxSizeOf<SnowflakeId>() + BinarySerializer.MaxSizeOf<SnowflakeId>();
        }

        protected override bool Serialize(LeftRoomBroadcast packet, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(packet.RoomId);
            serializer.WriteSnowflakeId(packet.LeaverId);
            return true;
        }

        protected override LeftRoomBroadcast Deserialize(ref BinaryDeserializer deserializer)
        {
            var roomId = deserializer.ReadSnowflakeId();
            var leaverId = deserializer.ReadSnowflakeId();

            return new LeftRoomBroadcast(roomId, leaverId);
        }
    }
}
