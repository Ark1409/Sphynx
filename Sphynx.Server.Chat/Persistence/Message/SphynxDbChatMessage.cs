// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Sphynx.Core;
using Sphynx.Server.Persistence;

namespace Sphynx.Server.Chat.Persistence.Message
{
    [BsonIgnoreExtraElements]
    public class SphynxDbChatMessage : IEquatable<SphynxDbChatMessage>
    {
        [BsonId]
        [BsonSerializer(typeof(SnowflakeIdSerializer))]
        public SnowflakeId MessageId { get; set; }

        // metaField
        [BsonElement("room_id")]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid RoomId { get; set; }

        [BsonElement("sender_id")]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid SenderId { get; set; }

        [BsonElement("content")]
        public string Content { get; set; } = null!;

        [BsonElement("edited_at")]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTimeOffset? EditedAt { get; set; }

        // timeField
        [BsonElement("created_at")]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTimeOffset CreatedAt { get; set; }

        public SphynxDbChatMessage()
        {
        }

        public SphynxDbChatMessage(Guid roomId, Guid senderId, string content)
        {
            RoomId = roomId;
            SenderId = senderId;
            Content = content;
        }

        /// <inheritdoc/>
        public virtual bool Equals(SphynxDbChatMessage? other) => MessageId.Equals(other?.MessageId);

        /// <inheritdoc/>
        public override int GetHashCode() => MessageId.GetHashCode();
    }
}
