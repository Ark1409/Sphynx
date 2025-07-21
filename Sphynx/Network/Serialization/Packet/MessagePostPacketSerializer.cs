// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.Network.PacketV2.Broadcast;
using Sphynx.Network.PacketV2.Request;
using Sphynx.Network.PacketV2.Response;

namespace Sphynx.Network.Serialization.Packet
{
    public class MessagePostRequestSerializer : RequestSerializer<MessagePostRequest>
    {
        protected override int GetMaxSizeInternal(MessagePostRequest packet)
        {
            return BinarySerializer.MaxSizeOf<SnowflakeId>() + BinarySerializer.MaxSizeOf(packet.Message);
        }

        protected override bool SerializeInternal(MessagePostRequest packet, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(packet.RoomId);
            serializer.WriteString(packet.Message);
            return true;
        }

        protected override MessagePostRequest DeserializeInternal(ref BinaryDeserializer deserializer, RequestInfo requestInfo)
        {
            var roomId = deserializer.ReadSnowflakeId();
            string message = deserializer.ReadString();

            return new MessagePostRequest(requestInfo.AccessToken, roomId, message);
        }
    }

    public class MessagePostResponseSerializer : ResponseSerializer<MessagePostResponse>
    {
        protected override int GetMaxSizeInternal(MessagePostResponse packet)
        {
            return 0;
        }

        protected override bool SerializeInternal(MessagePostResponse packet, ref BinarySerializer serializer)
        {
            return true;
        }

        protected override MessagePostResponse DeserializeInternal(ref BinaryDeserializer deserializer, ResponseInfo responseInfo)
        {
            return new MessagePostResponse(responseInfo.ErrorInfo);
        }
    }

    public class MessagePostedBroadcastSerializer : PacketSerializer<MessagePostedBroadcast>
    {
        public override int GetMaxSize(MessagePostedBroadcast packet)
        {
            return BinarySerializer.MaxSizeOf<SnowflakeId>() + BinarySerializer.MaxSizeOf<SnowflakeId>();
        }

        protected override bool Serialize(MessagePostedBroadcast packet, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(packet.RoomId);
            serializer.WriteSnowflakeId(packet.MessageId);
            return true;
        }

        protected override MessagePostedBroadcast Deserialize(ref BinaryDeserializer deserializer)
        {
            var roomId = deserializer.ReadSnowflakeId();
            var messageId = deserializer.ReadSnowflakeId();

            return new MessagePostedBroadcast(roomId, messageId);
        }
    }
}
