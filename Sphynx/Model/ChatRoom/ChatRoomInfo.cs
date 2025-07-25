namespace Sphynx.Model.ChatRoom
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
        public virtual ISet<Guid> Users { get; set; }

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

            Users = userIds as ISet<Guid> ?? userIds?.ToHashSet() ?? new HashSet<Guid>();
        }

        /// <inheritdoc/>
        public bool Equals(ChatRoomInfo? other)
        {
            if (other is null || RoomId != other.RoomId || RoomType != other.RoomType) return false;
            if (Users.Count != other.Users.Count) return false;

            return Users.All(user => other.Users.Contains(user));
        }

        /// <summary>
        /// Holds information about a direct-message chat room.
        /// </summary>
        public class Direct : ChatRoomInfo
        {
            /// <summary>
            /// Returns the user ID of one of the users within this direct-message chat room.
            /// </summary>
            /// <remarks>Null if this information has not yet been populated.</remarks>
            public Guid? UserOne => Users.Count == 0 ? null : Users.First();

            /// <summary>
            /// Returns the other user within this direct-message chat room.
            /// </summary>
            /// <remarks>Null if this information has not yet been populated.</remarks>
            public Guid? UserTwo
            {
                get
                {
                    if (Users.Count < 2) return null;

                    using var enumerator = Users.GetEnumerator();
                    enumerator.MoveNext();
                    enumerator.MoveNext();

                    return enumerator.Current;
                }
            }

            /// <summary>
            /// Creates new chat room information.
            /// </summary>
            /// <param name="roomId">The unique ID of this room.</param>
            /// <param name="name">The name of this chat room.</param>
            /// <param name="userOne">The user ID of one of the users within this direct-message chat room.</param>
            /// <param name="userTwo">The user ID of the other user within this direct-message chat room.</param>
            // TODO: Decide on naming convention for DMs
            public Direct(Guid roomId, string name, Guid userOne, Guid userTwo)
                : base(roomId, ChatRoomType.DIRECT_MSG, name, userOne, userTwo)
            {
            }

            /// <summary>
            /// Creates new chat room information.
            /// </summary>
            /// <param name="name">The name of this chat room.</param>
            /// <param name="userOne">The user ID of one of the users within this direct-message chat room.</param>
            /// <param name="userTwo">The user ID of the other user within this direct-message chat room.</param>
            public Direct(string name, Guid userOne, Guid userTwo)
                : this(default, name, userOne, userTwo)
            {
            }
        }

        /// <summary>
        /// Holds information about a group chat room with visibility options.
        /// </summary>
        public class Group : ChatRoomInfo
        {
            /// <summary>
            /// Whether this room is public.
            /// </summary>
            public virtual bool Public { get; set; }

            /// <summary>
            /// The user ID of the owner/creator of this group chat.
            /// </summary>
            public virtual Guid OwnerId { get; set; }

            /// <summary>
            /// The password for this group chat.
            /// </summary>
            internal virtual string? Password { get; set; }

            /// <inheritdoc/>
            /// <param name="public">Whether this room is public.</param>
            public Group(Guid roomId, Guid ownerId, string name, bool @public = true, IEnumerable<Guid>? userIds = null)
                : base(roomId, ChatRoomType.GROUP, name, userIds)
            {
                OwnerId = ownerId;
                Public = @public;
            }

            /// <inheritdoc/>
            public Group(Guid ownerId, string name, bool @public = true, IEnumerable<Guid>? userIds = null)
                : this(default, ownerId, name, @public, userIds)
            {
            }

            /// <inheritdoc/>
            public Group(Guid roomId, Guid ownerId, string name, bool @public, HashSet<Guid> userIds)
                : base(roomId, ChatRoomType.GROUP, name, userIds)
            {
                OwnerId = ownerId;
                Public = @public;
            }

            /// <inheritdoc/>
            public Group(Guid ownerId, string name, bool @public, HashSet<Guid> userIds)
                : this(default, ownerId, name, @public, userIds)
            {
            }

            /// <inheritdoc/>
            public Group(Guid roomId, Guid ownerId, string name, bool @public = true, params Guid[] userIds)
                : this(roomId, ownerId, name, @public, (IEnumerable<Guid>)userIds)
            {
            }

            /// <inheritdoc/>
            public Group(Guid ownerId, string name, bool @public = true, params Guid[] userIds)
                : this(default, ownerId, name, @public, (IEnumerable<Guid>)userIds)
            {
            }

            /// <inheritdoc/>
            public Group(Guid roomId, Guid ownerId, string name, byte[] pwd, bool @public = true, IEnumerable<Guid>? userIds = null)
                : this(roomId, ownerId, name, Convert.ToBase64String(pwd), @public, userIds)
            {
            }

            /// <inheritdoc/>
            public Group(Guid roomId, Guid ownerId, string name, byte[] pwd, bool @public = true, params Guid[] userIds)
                : this(roomId, ownerId, name, Convert.ToBase64String(pwd), @public, userIds)
            {
            }

            /// <inheritdoc/>
            public Group(Guid ownerId, string name, byte[] pwd, bool @public = true, IEnumerable<Guid>? userIds = null)
                : this(default, ownerId, name, Convert.ToBase64String(pwd), @public, userIds)
            {
            }

            /// <inheritdoc/>
            public Group(Guid roomId, Guid ownerId, string name, string pwd, bool @public = true, IEnumerable<Guid>? userIds = null)
                : this(roomId, ownerId, name, @public, userIds)
            {
                Password = pwd;
            }

            /// <inheritdoc/>
            public Group(Guid roomId, Guid ownerId, string name, string pwd, bool @public = true, params Guid[] userIds)
                : this(roomId, ownerId, name, pwd, @public, (IEnumerable<Guid>)userIds)
            {
            }

            /// <inheritdoc/>
            public Group(Guid ownerId, string name, string pwd, bool @public = true, IEnumerable<Guid>? userIds = null)
                : this(default, ownerId, name, pwd, @public, userIds)
            {
            }
        }
    }
}
