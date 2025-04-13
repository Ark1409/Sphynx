// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;

namespace Sphynx.ModelV2
{
    /// <summary>
    /// Represents a single message within a chat room.
    /// </summary>
    public interface IChatMessage : IEquatable<IChatMessage>
    {
        /// <summary>
        /// An ID for this specific message.
        /// </summary>
        SnowflakeId MessageId { get; set; }

        /// <summary>
        /// The chat room to which this message was sent.
        /// </summary>
        SnowflakeId RoomId { get; set; }

        /// <summary>
        /// The user ID of the message sender.
        /// </summary>
        SnowflakeId SenderId { get; set; }

        /// <summary>
        /// The message content.
        /// </summary>
        string Content { get; set; }

        /// <summary>
        /// The timestamp at which this message was edited.
        /// </summary>
        DateTimeOffset? EditTimestamp { get; set; }
    }
}
