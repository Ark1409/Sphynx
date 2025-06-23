// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;

namespace Sphynx.ModelV2.Room
{
    /// <summary>
    /// Holds information about a chat room containing Sphynx users.
    /// </summary>
    public abstract class ChatRoomInfo : IEquatable<ChatRoomInfo>
    {
        /// <summary>
        /// The unique ID of this room.
        /// </summary>
        public SnowflakeId RoomId { get; set; }

        /// <summary>
        /// Returns the type of this <see cref="ChatRoomInfo"/>.
        /// </summary>
        public ChatRoomType RoomType { get; set; }

        /// <summary>
        /// The name of this chat room.
        /// </summary>
        public string Name { get; set; } = null!;

        public ChatRoomInfo()
        {
        }

        public ChatRoomInfo(SnowflakeId roomId, ChatRoomType roomType, string name)
        {
            RoomId = roomId;
            RoomType = roomType;
            Name = name;
        }

        /// <inheritdoc/>
        public bool Equals(ChatRoomInfo? other) => RoomId == other?.RoomId;
    }
}
