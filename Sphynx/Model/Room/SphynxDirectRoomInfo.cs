// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;

namespace Sphynx.Model.Room
{
    /// <summary>
    /// Holds information about a direct-message chat room.
    /// </summary>
    public class SphynxDirectRoomInfo : SphynxRoomInfo, IEquatable<SphynxDirectRoomInfo>
    {
        /// <inheritdoc />
        public override SphynxRoomType RoomType => SphynxRoomType.DIRECT_MSG;

        /// <summary>
        /// Returns the user ID of one of the users within this direct-message chat room.
        /// </summary>
        public Guid UserA { get; set; }

        /// <summary>
        /// Returns the other user within this direct-message chat room.
        /// </summary>
        public Guid UserB { get; set; }

        public SphynxDirectRoomInfo()
        {
        }

        public SphynxDirectRoomInfo(Guid userA, Guid userB)
        {
            UserA = userA;
            UserB = userB;
        }

        public SphynxDirectRoomInfo(Guid roomId) : base(roomId)
        {
        }

        public SphynxDirectRoomInfo(Guid roomId, Guid userA, Guid userB)
            : base(roomId)
        {
            UserA = userA;
            UserB = userB;
        }

        /// <inheritdoc/>
        public virtual bool Equals(SphynxDirectRoomInfo? other) => base.Equals(other);
    }
}
