// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Model;

namespace Sphynx.Network.Serialization.Model
{
    public class ChatMessageSerializer : TypeSerializer<SphynxChatMessage>
    {
        public override void Serialize(SphynxChatMessage model, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(model.MessageId);
            serializer.WriteGuid(model.RoomId);
            serializer.WriteGuid(model.SenderId);
            serializer.WriteString(model.Content);
            serializer.WriteDateTimeOffset(model.EditTimestamp ?? DateTimeOffset.MinValue);
        }

        public override SphynxChatMessage Deserialize(ref BinaryDeserializer deserializer)
        {
            var msgId = deserializer.ReadSnowflakeId();
            var roomId = deserializer.ReadGuid();
            var senderId = deserializer.ReadGuid();
            string? content = deserializer.ReadString();
            var editTimestamp = deserializer.ReadDateTimeOffset();

            return new SphynxChatMessage
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
