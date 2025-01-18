// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;

namespace Sphynx.ModelV2.Room
{
    /// <summary>
    /// Holds information about a chat room containing Sphynx users.
    /// </summary>
    public interface IChatRoomInfo : IEquatable<IChatRoomInfo>
    {
        /// <summary>
        /// The unique ID of this room.
        /// </summary>
        SnowflakeId RoomId { get; set; }

        /// <summary>
        /// Returns the type of this <see cref="IChatRoomInfo"/>.
        /// </summary>
        ChatRoomType RoomType { get; set; }

        /// <summary>
        /// The name of this chat room.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// A collection of the user IDs of the users within this chat room.
        /// </summary>
        ISet<SnowflakeId> Users { get; set; }
    }
}
