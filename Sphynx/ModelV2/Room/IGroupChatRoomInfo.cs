// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;

namespace Sphynx.ModelV2.Room
{
    /// <summary>
    /// Holds information about a group chat room with visibility options.
    /// </summary>
    public interface IGroupChatRoomInfo : IChatRoomInfo, IEquatable<IGroupChatRoomInfo>
    {
        /// <summary>
        /// Whether this room is public.
        /// </summary>
        bool IsPublic { get; set; }

        /// <summary>
        /// The user ID of the owner/creator of this group chat.
        /// </summary>
        SnowflakeId OwnerId { get; set; }
    }
}
