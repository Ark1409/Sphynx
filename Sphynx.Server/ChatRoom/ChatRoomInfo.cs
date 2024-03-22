using System.Diagnostics;
using MongoDB.Bson.Serialization.Attributes;
using Sphynx.ChatRoom;
using Sphynx.Server.Storage;

namespace Sphynx.Server.ChatRoom
{
    /// <summary>
    /// Holds information about a chat room containing Sphynx users.
    /// </summary>
    public abstract class ChatRoomInfo : IChatRoomInfo, IIdentifiable<Guid>
    {
        /// <inheritdoc/>
        public abstract string Name { get; internal set; }

        /// <inheritdoc/>
        public abstract Guid RoomId { get; internal set; }

        /// <inheritdoc/>
        public virtual Guid Id => RoomId;

        /// <inheritdoc/>
        public abstract ICollection<Guid> Users { get; internal set; }

        /// <inheritdoc/>
        public abstract ChatRoomType RoomType { get; internal set; }

        protected readonly HashSet<Guid> _users;

        /// <summary>
        /// Creates new chat room information.
        /// </summary>
        /// <param name="roomId">The unique ID of this room.</param>
        /// <param name="roomType">Returns the type of this <see cref="ChatRoomInfo"/>.</param>
        /// <param name="name">The name of this chat room.</param>
        /// <param name="userIds">A collection of the user IDs of the users within this chat room.</param>
        public ChatRoomInfo(Guid roomId, ChatRoomType roomType, string name, IEnumerable<Guid>? userIds = null)
        {
            RoomId = roomId;
            RoomType = roomType;
            Name = name;

            _users = new HashSet<Guid>();
            
            if (userIds is not null)
            {
                foreach (var userId in userIds)
                    Debug.Assert(_users.Add(userId));
            }
        }

        /// <inheritdoc/>
        public bool Equals(IChatRoomInfo? other) => RoomId == other?.RoomId && RoomType == other.RoomType;
    }
}