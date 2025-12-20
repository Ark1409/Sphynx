// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.Model;
using Sphynx.Server.Chat.Persistence.Message;

namespace Sphynx.Server.Chat.Model
{
    public class SphynxChatMessage : IEquatable<SphynxChatMessage>
    {
        public SnowflakeId MessageId { get; set; }

        public Guid RoomId { get; set; }

        public Guid SenderId { get; set; }

        public string Content { get; set; } = null!;

        public DateTimeOffset? EditedAt { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public SphynxChatMessage()
        {
        }

        public SphynxChatMessage(Guid roomId, Guid senderId, string content)
        {
            RoomId = roomId;
            SenderId = senderId;
            Content = content;
        }

        public virtual bool Equals(SphynxChatMessage? other) => MessageId.Equals(other?.MessageId);

        public override int GetHashCode() => MessageId.GetHashCode();
    }

    public static class SphynxChatMessageExtensions
    {
        public static SphynxMessageInfo ToDto(this SphynxChatMessage msg)
        {
            return new SphynxMessageInfo
            {
                MessageId = msg.MessageId,
                RoomId = msg.RoomId,
                Content = msg.Content,
                SenderId = msg.SenderId,
                EditedAt = msg.EditedAt,
                CreatedAt = msg.CreatedAt,
            };
        }

        public static SphynxChatMessage ToDomain(this SphynxDbChatMessage msg)
        {
            return new SphynxChatMessage
            {
                MessageId = msg.MessageId,
                RoomId = msg.RoomId,
                Content = msg.Content,
                SenderId = msg.SenderId,
                EditedAt = msg.EditedAt,
                CreatedAt = msg.CreatedAt,
            };
        }

        public static SphynxDbChatMessage ToRecord(this SphynxChatMessage msg)
        {
            return new SphynxDbChatMessage
            {
                MessageId = msg.MessageId,
                RoomId = msg.RoomId,
                Content = msg.Content,
                SenderId = msg.SenderId,
                EditedAt = msg.EditedAt,
                CreatedAt = msg.CreatedAt,
            };
        }
    }
}
