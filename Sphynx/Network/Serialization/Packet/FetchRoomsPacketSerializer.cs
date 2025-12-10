// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.Model.Room;
using Sphynx.Network.Packet.Request;
using Sphynx.Network.Packet.Response;
using Sphynx.Network.Serialization.Model;

namespace Sphynx.Network.Serialization.Packet
{
    public class FetchRoomsRequestSerializer : RequestSerializer<FetchRoomsRequest>
    {
        protected override void SerializeRequest(FetchRoomsRequest packet, ref BinarySerializer serializer)
        {
            serializer.WriteCollection(packet.RoomIds);
        }

        protected override FetchRoomsRequest DeserializeRequest(ref BinaryDeserializer deserializer, in RequestInfo requestInfo)
        {
            var roomIds = deserializer.ReadArray<Guid>();
            return new FetchRoomsRequest(requestInfo.SessionId, roomIds)
            {
                RequestTag = requestInfo.RequestTag
            };
        }
    }

    public class FetchRoomsResponseSerializer : ResponseSerializer<FetchRoomsResponse>
    {
        private readonly ITypeSerializer<ChatRoomInfo[]> _roomSerializer;

        public FetchRoomsResponseSerializer(ITypeSerializer<ChatRoomInfo> roomSerializer)
            : this(new ArraySerializer<ChatRoomInfo>(roomSerializer))
        {
        }

        public FetchRoomsResponseSerializer(ITypeSerializer<ChatRoomInfo[]> roomSerializer)
        {
            _roomSerializer = roomSerializer;
        }

        protected override void SerializeResponse(FetchRoomsResponse packet, ref BinarySerializer serializer)
        {
            // Only serialize Rooms on success
            if (packet.ErrorInfo != SphynxErrorCode.SUCCESS)
                return;

            _roomSerializer.Serialize(packet.Rooms!, ref serializer);
        }

        protected override FetchRoomsResponse? DeserializeResponse(ref BinaryDeserializer deserializer, in ResponseInfo responseInfo)
        {
            if (responseInfo.ErrorInfo != SphynxErrorCode.SUCCESS)
            {
                return new FetchRoomsResponse(responseInfo.ErrorInfo)
                {
                    RequestTag = responseInfo.RequestTag
                };
            }

            var rooms = _roomSerializer.Deserialize(ref deserializer)!;

            return new FetchRoomsResponse(rooms)
            {
                RequestTag = responseInfo.RequestTag
            };
        }
    }
}
