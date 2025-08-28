// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.Network.Packet.Broadcast;
using Sphynx.Network.Packet.Request;
using Sphynx.Network.Packet.Response;

namespace Sphynx.Network.Serialization.Packet
{
    public class MessagePostRequestSerializer : RequestSerializer<MessagePostRequest>
    {
        protected override void SerializeRequest(MessagePostRequest packet, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(packet.RoomId);
            serializer.WriteString(packet.Message);
        }

        protected override MessagePostRequest DeserializeRequest(ref BinaryDeserializer deserializer, in RequestInfo requestInfo)
        {
            var roomId = deserializer.ReadSnowflakeId();
            string message = deserializer.ReadString() ?? string.Empty;

            return new MessagePostRequest(requestInfo.SessionId, roomId, message);
        }
    }

    public class MessagePostResponseSerializer : ResponseSerializer<MessagePostResponse>
    {
        protected override void SerializeInternal(MessagePostResponse packet, ref BinarySerializer serializer)
        {
        }

        protected override MessagePostResponse DeserializeInternal(ref BinaryDeserializer deserializer, in ResponseInfo responseInfo)
        {
            return new MessagePostResponse(responseInfo.ErrorInfo);
        }
    }

    public class MessagePostedBroadcastSerializer : PacketSerializer<MessagePostedBroadcast>
    {
        public override void Serialize(MessagePostedBroadcast packet, ref BinarySerializer serializer)
        {
            serializer.WriteGuid(packet.RoomId);
            serializer.WriteSnowflakeId(packet.MessageId);
        }

        public override MessagePostedBroadcast Deserialize(ref BinaryDeserializer deserializer)
        {
            var roomId = deserializer.ReadGuid();
            var messageId = deserializer.ReadSnowflakeId();

            return new MessagePostedBroadcast(roomId, messageId);
        }
    }
}
