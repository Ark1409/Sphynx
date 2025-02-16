// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;

namespace Sphynx.ModelV2.Room
{
    /// <summary>
    /// Holds information about a direct-message chat room.
    /// </summary>
    public interface IDirectChatRoomInfo : IChatRoomInfo, IEquatable<IDirectChatRoomInfo>
    {
        /// <summary>
        /// Returns the user ID of one of the users within this direct-message chat room.
        /// </summary>
        public SnowflakeId UserOne { get; set; }

        /// <summary>
        /// Returns the other user within this direct-message chat room.
        /// </summary>
        public SnowflakeId UserTwo { get; set; }
    }
}
