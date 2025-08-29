// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.Model.Room;
using Sphynx.Network.Packet.Broadcast;
using Sphynx.Network.Packet.Request;
using Sphynx.Network.Packet.Response;
using Sphynx.Network.Serialization.Model;

namespace Sphynx.Network.Serialization.Packet
{
    public class JoinRoomRequestSerializer : RequestSerializer<JoinRoomRequest>
    {
        protected override void SerializeRequest(JoinRoomRequest packet, ref BinarySerializer serializer)
        {
            serializer.WriteGuid(packet.RoomId);
            serializer.WriteString(packet.Password);
        }

        protected override JoinRoomRequest DeserializeRequest(ref BinaryDeserializer deserializer, in RequestInfo requestInfo)
        {
            var roomId = deserializer.ReadGuid();
            string? password = deserializer.ReadString();

            return new JoinRoomRequest(requestInfo.SessionId, roomId, string.IsNullOrEmpty(password) ? null : password)
            {
                RequestTag = requestInfo.RequestTag
            };
        }
    }

    public class JoinRoomResponseSerializer : ResponseSerializer<JoinRoomResponse>
    {
        private readonly ITypeSerializer<ChatRoomInfo> _roomSerializer;

        public JoinRoomResponseSerializer(ITypeSerializer<ChatRoomInfo> roomSerializer)
        {
            _roomSerializer = roomSerializer;
        }

        protected override void SerializeResponse(JoinRoomResponse packet, ref BinarySerializer serializer)
        {
            if (packet.ErrorInfo != SphynxErrorCode.SUCCESS)
                return;

            _roomSerializer.Serialize(packet.RoomInfo!, ref serializer);
        }

        protected override JoinRoomResponse? DeserializeResponse(ref BinaryDeserializer deserializer, in ResponseInfo responseInfo)
        {
            if (responseInfo.ErrorInfo != SphynxErrorCode.SUCCESS)
            {
                return new JoinRoomResponse(responseInfo.ErrorInfo)
                {
                    RequestTag = responseInfo.RequestTag
                };
            }

            var roomInfo = _roomSerializer.Deserialize(ref deserializer)!;

            return new JoinRoomResponse(roomInfo)
            {
                RequestTag = responseInfo.RequestTag
            };
        }
    }

    public class JoinedRoomBroadcastSerializer : PacketSerializer<JoinedRoomBroadcast>
    {
        public override void Serialize(JoinedRoomBroadcast packet, ref BinarySerializer serializer)
        {
            serializer.WriteGuid(packet.RoomId);
            serializer.WriteGuid(packet.JoinerId);
        }

        public override JoinedRoomBroadcast Deserialize(ref BinaryDeserializer deserializer)
        {
            var roomId = deserializer.ReadGuid();
            var joinerId = deserializer.ReadGuid();

            return new JoinedRoomBroadcast(roomId, joinerId);
        }
    }
}
