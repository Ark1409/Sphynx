// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.ModelV2;

namespace Sphynx.Network.Serialization.Model
{
    public class ChatMessageSerializer : TypeSerializer<ChatMessage>
    {
        public override void Serialize(ChatMessage model, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(model.MessageId);
            serializer.WriteSnowflakeId(model.RoomId);
            serializer.WriteSnowflakeId(model.SenderId);
            serializer.WriteString(model.Content);
            serializer.WriteDateTimeOffset(model.EditTimestamp ?? DateTimeOffset.MinValue);
        }

        public override ChatMessage Deserialize(ref BinaryDeserializer deserializer)
        {
            var msgId = deserializer.ReadSnowflakeId();
            var roomId = deserializer.ReadSnowflakeId();
            var senderId = deserializer.ReadSnowflakeId();
            string? content = deserializer.ReadString();
            var editTimestamp = deserializer.ReadDateTimeOffset();

            return new ChatMessage
            {
                MessageId = msgId,
                RoomId = roomId,
                SenderId = senderId,
                Content = content ?? string.Empty,
                EditTimestamp = editTimestamp == DateTimeOffset.MinValue ? null : editTimestamp
            };
        }
    }
}
