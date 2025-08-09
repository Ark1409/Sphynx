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
        protected override void SerializeInternal(JoinRoomRequest packet, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(packet.RoomId);
            serializer.WriteString(packet.Password);
        }

        protected override JoinRoomRequest DeserializeInternal(ref BinaryDeserializer deserializer, in RequestInfo requestInfo)
        {
            var roomId = deserializer.ReadSnowflakeId();
            string password = deserializer.ReadString()!;

            return new JoinRoomRequest(requestInfo.AccessToken, roomId, string.IsNullOrEmpty(password) ? null : password);
        }
    }

    public class JoinRoomResponseSerializer : ResponseSerializer<JoinRoomResponse>
    {
        private readonly ITypeSerializer<ChatRoomInfo> _roomSerializer;

        public JoinRoomResponseSerializer(ITypeSerializer<ChatRoomInfo> roomSerializer)
        {
            _roomSerializer = roomSerializer;
        }

        protected override void SerializeInternal(JoinRoomResponse packet, ref BinarySerializer serializer)
        {
            if (packet.ErrorInfo != SphynxErrorCode.SUCCESS)
                return;

            _roomSerializer.Serialize(packet.RoomInfo!, ref serializer);
        }

        protected override JoinRoomResponse? DeserializeInternal(ref BinaryDeserializer deserializer, in ResponseInfo responseInfo)
        {
            if (responseInfo.ErrorInfo != SphynxErrorCode.SUCCESS)
                return new JoinRoomResponse(responseInfo.ErrorInfo);

            var roomInfo = _roomSerializer.Deserialize(ref deserializer)!;

            return new JoinRoomResponse(roomInfo);
        }
    }

    public class JoinedRoomBroadcastSerializer : PacketSerializer<JoinedRoomBroadcast>
    {
        public override void Serialize(JoinedRoomBroadcast packet, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(packet.RoomId);
            serializer.WriteSnowflakeId(packet.JoinerId);
        }

        public override JoinedRoomBroadcast Deserialize(ref BinaryDeserializer deserializer)
        {
            var roomId = deserializer.ReadSnowflakeId();
            var joinerId = deserializer.ReadSnowflakeId();

            return new JoinedRoomBroadcast(roomId, joinerId);
        }
    }
}
