// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.ModelV2;

namespace Sphynx.Network.Serialization.Model
{
    public class ChatMessageSerializer : TypeSerializer<IChatMessage>
    {
        public override int GetMaxSize(IChatMessage model)
        {
            return BinarySerializer.MaxSizeOf<SnowflakeId>() + BinarySerializer.MaxSizeOf<SnowflakeId>() +
                   BinarySerializer.MaxSizeOf<SnowflakeId>() + BinarySerializer.MaxSizeOf(model.Content) +
                   BinarySerializer.MaxSizeOf<DateTimeOffset>();
        }

        protected override bool Serialize(IChatMessage model, ref BinarySerializer serializer)
        {
            serializer.WriteSnowflakeId(model.MessageId);
            serializer.WriteSnowflakeId(model.RoomId);
            serializer.WriteSnowflakeId(model.SenderId);
            serializer.WriteString(model.Content);
            serializer.WriteDateTimeOffset(model.EditTimestamp ?? DateTimeOffset.MinValue);
            return true;
        }

        protected override IChatMessage Deserialize(ref BinaryDeserializer deserializer)
        {
            var msgId = deserializer.ReadSnowflakeId();
            var roomId = deserializer.ReadSnowflakeId();
            var senderId = deserializer.ReadSnowflakeId();
            string content = deserializer.ReadString();
            var editTimestamp = deserializer.ReadDateTimeOffset();

            return new DummyChatMessage
            {
                MessageId = msgId,
                RoomId = roomId,
                SenderId = senderId,
                Content = content,
                EditTimestamp = editTimestamp == DateTimeOffset.MinValue ? null : editTimestamp
            };
        }

        private class DummyChatMessage : IChatMessage
        {
            public SnowflakeId MessageId { get; set; }
            public SnowflakeId RoomId { get; set; }
            public SnowflakeId SenderId { get; set; }
            public string Content { get; set; } = null!;
            public DateTimeOffset? EditTimestamp { get; set; }

            public bool Equals(IChatMessage? other) => MessageId == other?.MessageId && RoomId == other.RoomId;
        }
    }
}
