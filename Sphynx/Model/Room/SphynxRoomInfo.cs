// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;

namespace Sphynx.Model.Room
{
    /// <summary>
    /// Holds information about a chat room containing Sphynx users.
    /// </summary>
    public abstract class SphynxRoomInfo : IEquatable<SphynxRoomInfo>
    {
        /// <summary>
        /// The unique ID of this room.
        /// </summary>
        public Guid RoomId { get; set; }

        /// <summary>
        /// Returns the type of this <see cref="SphynxRoomInfo"/>.
        /// </summary>
        public abstract SphynxRoomType RoomType { get; }

        /// <summary>
        /// The name of this chat room.
        /// </summary>
        public string Name { get; set; } = null!;

        public DateTimeOffset CreatedAt { get; set; }

        public SphynxRoomInfo()
        {
        }

        public SphynxRoomInfo(Guid roomId, string name)
        {
            RoomId = roomId;
            Name = name;
        }

        /// <inheritdoc/>
        public virtual bool Equals(SphynxRoomInfo? other) => RoomId == other?.RoomId;
    }
}
