using System.Diagnostics.CodeAnalysis;
using MongoDB.Bson.Serialization.Attributes;
using Sphynx.ChatRoom;
using Sphynx.Server.Storage;

namespace Sphynx.Server.ChatRoom
{
    /// <summary>
    /// Represents a single message within a chat room.
    /// </summary>
    public class ChatRoomMessageInfo : IChatRoomMessageInfo, IIdentifiable<Guid>
    {
        /// <inheritdoc/>
        [BsonElement("time")]
        public DateTimeOffset Timestamp { get; internal set; }

        /// <inheritdoc/>
        [BsonElement("room")]
        public Guid RoomId { get; internal set; }

        /// <inheritdoc/>
        [BsonElement("sender")]
        public Guid SenderId { get; internal set; }

        /// <inheritdoc/>
        [BsonElement("content")]
        public string Content { get; internal set; }

        /// <inheritdoc/>
        [BsonIgnore]
        public Guid Id => RoomId;

        /// <summary>
        /// Creates a new <see cref="ChatRoomMessageInfo"/>.
        /// </summary>
        /// <param name="roomId">The room that this message was sent to.</param>
        /// <param name="senderId">The user ID of the message sender.</param>
        /// <param name="content">The message content.</param>
        public ChatRoomMessageInfo(Guid roomId, Guid senderId, string content) : this(roomId, DateTimeOffset.UtcNow, senderId, content)
        {
        }

        /// <inheritdoc cref="ChatRoomMessageInfo(Guid, Guid, string)"/>
        /// <param name="timestamp">The timestamp for this message.</param>
        [SuppressMessage("ReSharper", "InvalidXmlDocComment")]
        public ChatRoomMessageInfo(Guid roomId, DateTimeOffset timestamp, Guid senderId, string content)
        {
            RoomId = roomId;
            Timestamp = timestamp;
            SenderId = senderId;
            Content = content ?? string.Empty;
        }
    }
}