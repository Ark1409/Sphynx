namespace Sphynx.ChatRoom
{
    /// <summary>
    /// Represents a single message within a chat room.
    /// </summary>
    public class ChatRoomMessageInfo
    {
        /// <summary>
        /// The timestamp for this message.
        /// </summary>
        public virtual DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// The timestamp at which this message was edited.
        /// </summary>
        public virtual DateTimeOffset EditTimestamp { get; set; }

        /// <summary>
        /// THe chat room to which this message was sent.
        /// </summary>
        public virtual Guid RoomId { get; set; }

        /// <summary>
        /// The user ID of the message sender.
        /// </summary>
        public virtual Guid SenderId { get; set; }

        /// <summary>
        /// An ID for this specific message.
        /// </summary>
        public virtual Guid MessageId { get; set; }

        /// <summary>
        /// The message content.
        /// </summary>
        public virtual string Content { get; set; }

        /// <summary>
        /// Creates a new <see cref="ChatRoomMessageInfo"/>.
        /// </summary>
        /// <param name="roomId">The room that this message was sent to.</param>
        /// <param name="senderId">The user ID of the message sender.</param>
        /// <param name="content">The message content.</param>
        public ChatRoomMessageInfo(Guid roomId, Guid senderId, string content)
            : this(roomId, Guid.NewGuid(), senderId, DateTimeOffset.UtcNow, content)
        {
        }

        /// <summary>
        /// Creates a new <see cref="ChatRoomMessageInfo"/>.
        /// </summary>
        /// <param name="roomId">The room that this message was sent to.</param>
        /// <param name="senderId">The user ID of the message sender.</param>
        /// <param name="content">The message content.</param>
        /// <param name="timestamp">The timestamp for this message.</param>
        public ChatRoomMessageInfo(Guid roomId, Guid senderId, DateTimeOffset timestamp, string content)
            : this(roomId, Guid.NewGuid(), senderId, timestamp, content)
        {
        }

        /// <summary>
        /// Creates a new <see cref="ChatRoomMessageInfo"/>.
        /// </summary>
        /// <param name="roomId">The room that this message was sent to.</param>
        /// <param name="messageId">An ID for this specific message.</param>
        /// <param name="senderId">The user ID of the message sender.</param>
        /// <param name="content">The message content.</param>
        public ChatRoomMessageInfo(Guid roomId, Guid messageId, Guid senderId, string content)
            : this(roomId, messageId, senderId, DateTimeOffset.UtcNow, content)
        {
        }

        /// <inheritdoc cref="ChatRoomMessageInfo(System.Guid,System.Guid,string)"/>
        /// <param name="messageId">An ID for this specific message.</param>
        /// <param name="timestamp">The timestamp for this message.</param>
        public ChatRoomMessageInfo(Guid roomId, Guid messageId, Guid senderId, DateTimeOffset timestamp, string content)
            : this(roomId, messageId, senderId, timestamp, DateTimeOffset.MinValue, content)
        {
        }

        /// <inheritdoc cref="ChatRoomMessageInfo(System.Guid,System.Guid,System.Guid,System.DateTimeOffset,string)"/>
        /// <param name="editTimestamp">The timestamp at which this message was edited.</param>
        public ChatRoomMessageInfo(Guid roomId, Guid messageId, Guid senderId, DateTimeOffset timestamp, DateTimeOffset editTimestamp, string content)
        {
            RoomId = roomId;
            MessageId = messageId;
            Timestamp = timestamp;
            EditTimestamp = editTimestamp;
            SenderId = senderId;
            Content = content ?? string.Empty;
        }
    }
}