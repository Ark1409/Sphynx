// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Model;

namespace Sphynx.Network.Serialization.Model
{
    public class ChatMessageSerializer : TypeSerializer<SphynxMessageInfo>
    {
        public override void Serialize(SphynxMessageInfo model, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(model.MessageId);
            serializer.WriteGuid(model.RoomId);
            serializer.WriteGuid(model.SenderId);
            serializer.WriteString(model.Content);
            serializer.WriteDateTimeOffset(model.EditedAt ?? DateTimeOffset.MinValue);
        }

        public override SphynxMessageInfo Deserialize(ref BinaryDeserializer deserializer)
        {
            var msgId = deserializer.ReadSnowflakeId();
            var roomId = deserializer.ReadGuid();
            var senderId = deserializer.ReadGuid();
            string? content = deserializer.ReadString();
            var editTimestamp = deserializer.ReadDateTimeOffset();

            return new SphynxMessageInfo
            {
                MessageId = msgId,
                RoomId = roomId,
                SenderId = senderId,
                Content = content ?? string.Empty,
                EditedAt = editTimestamp == DateTimeOffset.MinValue ? null : editTimestamp
            };
        }
    }
}
