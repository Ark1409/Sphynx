// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;

namespace Sphynx.ModelV2.Room
{
    /// <summary>
    /// Holds information about a direct-message chat room.
    /// </summary>
    public class DirectChatRoomInfo : ChatRoomInfo, IEquatable<DirectChatRoomInfo>
    {
        /// <summary>
        /// Returns the user ID of one of the users within this direct-message chat room.
        /// </summary>
        public SnowflakeId UserOne { get; set; }

        /// <summary>
        /// Returns the other user within this direct-message chat room.
        /// </summary>
        public SnowflakeId UserTwo { get; set; }

        public DirectChatRoomInfo()
        {
            Name = $"{UserOne}+{UserTwo}";
        }

        public DirectChatRoomInfo(SnowflakeId userOne, SnowflakeId userTwo)
        {
            UserOne = userOne;
            UserTwo = userTwo;
            Name = $"{UserOne}+{UserTwo}";
        }

        public DirectChatRoomInfo(SnowflakeId roomId, ChatRoomType roomType) : base(roomId, roomType, string.Empty)
        {
            Name = $"{UserOne}+{UserTwo}";
        }

        public DirectChatRoomInfo(SnowflakeId roomId, ChatRoomType roomType, SnowflakeId userOne, SnowflakeId userTwo)
            : base(roomId, roomType, string.Empty)
        {
            UserOne = userOne;
            UserTwo = userTwo;
            Name = $"{UserOne}+{UserTwo}";
        }

        /// <inheritdoc/>
        public bool Equals(DirectChatRoomInfo? other) => base.Equals(other);
    }
}
