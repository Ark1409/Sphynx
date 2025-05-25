// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;

namespace Sphynx.ModelV2
{
    /// <summary>
    /// Represents a single message within a chat room.
    /// </summary>
    public class ChatMessage : IEquatable<ChatMessage>
    {
        /// <summary>
        /// An ID for this specific message.
        /// </summary>
        public SnowflakeId MessageId { get; set; }

        /// <summary>
        /// The chat room to which this message was sent.
        /// </summary>
        public SnowflakeId RoomId { get; set; }

        /// <summary>
        /// The user ID of the message sender.
        /// </summary>
        public SnowflakeId SenderId { get; set; }

        /// <summary>
        /// The message content.
        /// </summary>
        public string Content { get; set; } = null!;

        /// <summary>
        /// The timestamp at which this message was edited.
        /// </summary>
        public DateTimeOffset? EditTimestamp { get; set; }

        public ChatMessage()
        {
        }

        public ChatMessage(SnowflakeId roomId, SnowflakeId senderId, string content)
        {
            RoomId = roomId;
            SenderId = senderId;
            Content = content;
        }

        /// <inheritdoc/>
        public bool Equals(ChatMessage? other) => MessageId.Equals(other?.MessageId);

        /// <inheritdoc/>
        public override int GetHashCode() => MessageId.GetHashCode();
    }
}
