// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;

namespace Sphynx.Model.Room
{
    /// <summary>
    /// Holds information about a group chat room with visibility options.
    /// </summary>
    public class GroupChatRoomInfo : ChatRoomInfo, IEquatable<GroupChatRoomInfo>
    {
        /// <inheritdoc />
        public override ChatRoomType RoomType => ChatRoomType.GROUP;

        /// <summary>
        /// Whether this room is public.
        /// </summary>
        public bool IsPublic { get; set; }

        /// <summary>
        /// The user ID of the owner/creator of this group chat.
        /// </summary>
        public SnowflakeId OwnerId { get; set; }

        public GroupChatRoomInfo()
        {
        }

        public GroupChatRoomInfo(SnowflakeId roomId, string name) : base(roomId, name)
        {
        }

        /// <inheritdoc/>
        public virtual bool Equals(GroupChatRoomInfo? other) => base.Equals(other);
    }
}
