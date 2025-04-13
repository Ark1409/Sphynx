// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.ModelV2.Room;
using Sphynx.Network.PacketV2.Request;
using Sphynx.Network.PacketV2.Response;
using Sphynx.Network.Serialization.Model;

namespace Sphynx.Network.Serialization.Packet
{
    public class GetRoomsRequestPacketSerializer : RequestPacketSerializer<FetchRoomsRequest>
    {
        protected override int GetMaxSizeInternal(FetchRoomsRequest packet)
        {
            return BinarySerializer.MaxSizeOf(packet.RoomIds);
        }

        protected override bool SerializeInternal(FetchRoomsRequest packet, ref BinarySerializer serializer)
        {
            serializer.WriteCollection(packet.RoomIds);
            return true;
        }

        protected override FetchRoomsRequest DeserializeInternal(
            ref BinaryDeserializer deserializer,
            RequestInfo requestInfo)
        {
            var roomIds = deserializer.ReadArray<SnowflakeId>();
            return new FetchRoomsRequest(requestInfo.UserId, requestInfo.SessionId, roomIds);
        }
    }

    public class GetRoomsResponsePacketSerializer : ResponsePacketSerializer<GetRoomsResponsePacket>
    {
        private readonly ITypeSerializer<IChatRoomInfo[]> _roomSerializer;

        public GetRoomsResponsePacketSerializer(ITypeSerializer<IChatRoomInfo> roomSerializer)
            : this(new ArraySerializer<IChatRoomInfo>(roomSerializer))
        {
        }

        public GetRoomsResponsePacketSerializer(ITypeSerializer<IChatRoomInfo[]> roomSerializer)
        {
            _roomSerializer = roomSerializer;
        }

        protected override int GetMaxSizeInternal(GetRoomsResponsePacket packet)
        {
            if (packet.ErrorCode != SphynxErrorCode.SUCCESS)
                return 0;

            return _roomSerializer.GetMaxSize(packet.Rooms!);
        }

        protected override bool SerializeInternal(GetRoomsResponsePacket packet, ref BinarySerializer serializer)
        {
            // Only serialize Rooms on success
            if (packet.ErrorCode != SphynxErrorCode.SUCCESS)
                return true;

            return _roomSerializer.TrySerializeUnsafe(packet.Rooms!, ref serializer);
        }

        protected override GetRoomsResponsePacket? DeserializeInternal(
            ref BinaryDeserializer deserializer,
            ResponseInfo responseInfo)
        {
            if (responseInfo.ErrorCode != SphynxErrorCode.SUCCESS)
                return new GetRoomsResponsePacket(responseInfo.ErrorCode);

            return _roomSerializer.TryDeserialize(ref deserializer, out var rooms)
                ? new GetRoomsResponsePacket(rooms)
                : null;
        }
    }
}
