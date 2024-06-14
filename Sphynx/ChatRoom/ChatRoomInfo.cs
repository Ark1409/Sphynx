using System.Diagnostics;

namespace Sphynx.ChatRoom
{
    /// <summary>
    /// Holds information about a chat room containing Sphynx users.
    /// </summary>
    public class ChatRoomInfo : IEquatable<ChatRoomInfo?>
    {
        /// <summary>
        /// The name of this chat room.
        /// </summary>
        public virtual string Name { get; set; }

        /// <summary>
        /// The unique ID of this room.
        /// </summary>
        public virtual Guid RoomId { get; set; }

        /// <summary>
        /// A collection of the user IDs of the users within this chat room.
        /// </summary>
        public virtual HashSet<Guid> Users { get; set; }

        /// <summary>
        /// Returns the type of this <see cref="ChatRoomInfo"/>.
        /// </summary>
        public virtual ChatRoomType RoomType { get; set; }

        /// <summary>
        /// Creates new chat room information.
        /// </summary>
        /// <param name="roomId">The unique ID of this room.</param>
        /// <param name="roomType">The type of this <see cref="ChatRoomInfo"/>.</param>
        /// <param name="name">The name of this chat room.</param>
        /// <param name="userIds">A collection of the user IDs of the users within this chat room.</param>
        public ChatRoomInfo(Guid roomId, ChatRoomType roomType, string name, params Guid[] userIds)
            : this(roomId, roomType, name, (IEnumerable<Guid>)userIds)
        {
        }

        /// <summary>
        /// Creates new chat room information.
        /// </summary>
        /// <param name="roomId">The unique ID of this room.</param>
        /// <param name="roomType">The type of this <see cref="ChatRoomInfo"/>.</param>
        /// <param name="name">The name of this chat room.</param>
        /// <param name="userIds">A collection of the user IDs of the users within this chat room.</param>
        public ChatRoomInfo(Guid roomId, ChatRoomType roomType, string name, IEnumerable<Guid>? userIds = null)
        {
            RoomId = roomId;
            RoomType = roomType;
            Name = name;

            Users = new HashSet<Guid>();

            if (userIds is not null)
            {
                foreach (var userId in userIds)
                    Debug.Assert(Users.Add(userId));
            }
        }

        /// <inheritdoc/>
        public bool Equals(ChatRoomInfo? other) => RoomId == other?.RoomId && RoomType == other.RoomType;
    }
}