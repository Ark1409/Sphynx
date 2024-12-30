using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using Sphynx.Model.ChatRoom;
using Sphynx.Server.Storage;

namespace Sphynx.Server.ChatRoom
{
    /// <summary>
    /// Represents a single message within a chat room.
    /// </summary>
    [BsonIgnoreExtraElements]
    public class ChatRoomMessageDbInfo : ChatRoomMessageInfo, IIdentifiable<Guid>
    {
        public const string TIMESTAMP_FIELD = "time";
        public const string EDITED_TIMESTAMP_FIELD = "edit_time";
        public const string ROOM_ID_FIELD = "room_id";
        public const string SENDER_ID_FIELD = "sender_id";
        public const string CONTENT_FIELD = "content";

        /// <inheritdoc/>
        [BsonElement(TIMESTAMP_FIELD)]
        public override DateTimeOffset Timestamp { get; set; }

        /// <inheritdoc/>
        [BsonElement(EDITED_TIMESTAMP_FIELD)]
        public override DateTimeOffset EditTimestamp { get; set; }

        /// <inheritdoc/>
        [BsonElement(ROOM_ID_FIELD)]
        public override Guid RoomId { get; set; }

        /// <inheritdoc/>
        [BsonElement(SENDER_ID_FIELD)]
        public override Guid SenderId { get; set; }

        /// <inheritdoc/>
        [BsonIgnore]
        public override Guid MessageId
        {
            get => Id;
            set => Id = value;
        }

        /// <inheritdoc/>
        [BsonElement(CONTENT_FIELD)]
        public override string Content { get; set; }

        /// <inheritdoc/>
        [BsonId(IdGenerator = typeof(CombGuidGenerator))]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; internal set; }

        /// <inheritdoc/>
        public ChatRoomMessageDbInfo(Guid roomId, Guid senderId, string content)
            : base(roomId, senderId, content)
        {
        }

        /// <inheritdoc/>
        public ChatRoomMessageDbInfo(Guid roomId, Guid senderId, DateTimeOffset timestamp, string content)
            : base(roomId, senderId, timestamp, content)
        {
        }

        /// <inheritdoc/>
        public ChatRoomMessageDbInfo(Guid roomId, Guid messageId, Guid senderId, string content)
            : base(roomId, messageId, senderId, content)
        {
        }

        /// <inheritdoc/>
        public ChatRoomMessageDbInfo(Guid roomId, Guid messageId, Guid senderId, DateTimeOffset timestamp, string content)
            : base(roomId, messageId, senderId, timestamp, content)
        {
        }

        /// <inheritdoc/>
        public ChatRoomMessageDbInfo(Guid roomId, Guid messageId, Guid senderId, DateTimeOffset timestamp, DateTimeOffset editTimestamp,
            string content)
            : base(roomId, messageId, senderId, timestamp, editTimestamp, content)
        {
        }
    }
}