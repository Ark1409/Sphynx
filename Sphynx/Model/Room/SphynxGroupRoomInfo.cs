// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;

namespace Sphynx.Model.Room
{
    /// <summary>
    /// Holds information about a group chat room with visibility options.
    /// </summary>
    public class SphynxGroupRoomInfo : SphynxRoomInfo, IEquatable<SphynxGroupRoomInfo>
    {
        /// <inheritdoc />
        public override SphynxRoomType RoomType => SphynxRoomType.GROUP;

        /// <summary>
        /// Whether this room is public.
        /// </summary>
        public bool IsPublic { get; set; }

        /// <summary>
        /// The user ID of the owner/creator of this group chat.
        /// </summary>
        public Guid OwnerId { get; set; }

        public string Name { get; set; } = null!;

        public string? Password { get; set; }
        public string? PasswordSalt { get; set; }

        public SphynxGroupRoomInfo()
        {
        }

        public SphynxGroupRoomInfo(Guid roomId) : base(roomId)
        {
        }

        /// <inheritdoc/>
        public virtual bool Equals(SphynxGroupRoomInfo? other) => base.Equals(other);
    }
}
