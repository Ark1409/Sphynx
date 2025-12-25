// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;

namespace Sphynx.Model
{
    /// <summary>
    /// Represents a single message within a chat room.
    /// </summary>
    public class SphynxMessageInfo : IEquatable<SphynxMessageInfo>
    {
        /// <summary>
        /// An ID for this specific message.
        /// </summary>
        public SnowflakeId MessageId { get; set; }

        /// <summary>
        /// The chat room to which this message was sent.
        /// </summary>
        public Guid RoomId { get; set; }

        /// <summary>
        /// The user ID of the message sender.
        /// </summary>
        public Guid SenderId { get; set; }

        /// <summary>
        /// The message content.
        /// </summary>
        public string Content { get; set; } = null!;

        /// <summary>
        /// The most recent timestamp at which this message was edited.
        /// </summary>
        public DateTimeOffset? EditedAt { get; set; }

        /// <summary>
        /// The timestamp at which this message was created.
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; }

        public SphynxMessageInfo()
        {
        }

        public SphynxMessageInfo(Guid roomId, Guid senderId, string content)
        {
            RoomId = roomId;
            SenderId = senderId;
            Content = content;
        }

        /// <inheritdoc/>
        public virtual bool Equals(SphynxMessageInfo? other) => RoomId == other?.RoomId && MessageId.Equals(other?.MessageId);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(RoomId.GetHashCode(), MessageId.GetHashCode());
    }
}
