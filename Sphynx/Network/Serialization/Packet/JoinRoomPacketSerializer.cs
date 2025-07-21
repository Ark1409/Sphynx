// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.ModelV2.Room;
using Sphynx.Network.PacketV2.Broadcast;
using Sphynx.Network.PacketV2.Request;
using Sphynx.Network.PacketV2.Response;
using Sphynx.Network.Serialization.Model;

namespace Sphynx.Network.Serialization.Packet
{
    public class JoinRoomRequestSerializer : RequestSerializer<JoinRoomRequest>
    {
        protected override int GetMaxSizeInternal(JoinRoomRequest packet)
        {
            return BinarySerializer.MaxSizeOf<SnowflakeId>() + BinarySerializer.MaxSizeOf(packet.Password);
        }

        protected override bool SerializeInternal(JoinRoomRequest packet, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(packet.RoomId);
            serializer.WriteString(packet.Password);
            return true;
        }

        protected override JoinRoomRequest DeserializeInternal(ref BinaryDeserializer deserializer, RequestInfo requestInfo)
        {
            var roomId = deserializer.ReadSnowflakeId();
            string password = deserializer.ReadString();

            return new JoinRoomRequest(requestInfo.AccessToken, roomId,
                string.IsNullOrEmpty(password) ? null : password);
        }
    }

    public class JoinRoomResponseSerializer : ResponseSerializer<JoinRoomResponse>
    {
        private readonly ITypeSerializer<ChatRoomInfo> _roomSerializer;

        public JoinRoomResponseSerializer(ITypeSerializer<ChatRoomInfo> roomSerializer)
        {
            _roomSerializer = roomSerializer;
        }

        protected override int GetMaxSizeInternal(JoinRoomResponse packet)
        {
            if (packet.ErrorInfo != SphynxErrorCode.SUCCESS)
                return 0;

            return _roomSerializer.GetMaxSize(packet.RoomInfo!);
        }

        protected override bool SerializeInternal(JoinRoomResponse packet, ref BinarySerializer serializer)
        {
            if (packet.ErrorInfo != SphynxErrorCode.SUCCESS)
                return true;

            return _roomSerializer.TrySerializeUnsafe(packet.RoomInfo!, ref serializer);
        }

        protected override JoinRoomResponse? DeserializeInternal(ref BinaryDeserializer deserializer, ResponseInfo responseInfo)
        {
            if (responseInfo.ErrorInfo != SphynxErrorCode.SUCCESS)
                return new JoinRoomResponse(responseInfo.ErrorInfo);

            return _roomSerializer.TryDeserialize(ref deserializer, out var room)
                ? new JoinRoomResponse(room)
                : null;
        }
    }

    public class JoinedRoomBroadcastSerializer : PacketSerializer<JoinedRoomBroadcast>
    {
        public override int GetMaxSize(JoinedRoomBroadcast packet)
        {
            return BinarySerializer.MaxSizeOf<SnowflakeId>() + BinarySerializer.MaxSizeOf<SnowflakeId>();
        }

        protected override bool Serialize(JoinedRoomBroadcast packet, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(packet.RoomId);
            serializer.WriteSnowflakeId(packet.JoinerId);
            return true;
        }

        protected override JoinedRoomBroadcast Deserialize(ref BinaryDeserializer deserializer)
        {
            var roomId = deserializer.ReadSnowflakeId();
            var joinerId = deserializer.ReadSnowflakeId();

            return new JoinedRoomBroadcast(roomId, joinerId);
        }
    }
}
