namespace Sphynx.ChatRoom
{
    /// <summary>
    /// Holds information about a direct-message chat room.
    /// </summary>
    public class DirectChatRoomInfo : ChatRoomInfo
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
        public DirectChatRoomInfo(Guid roomId, string name, Guid userOne, Guid userTwo)
            : base(roomId, ChatRoomType.DIRECT_MSG, name, userOne, userTwo)
        {
        }

        /// <summary>
        /// Creates new chat room information.
        /// </summary>
        /// <param name="name">The name of this chat room.</param>
        /// <param name="userOne">The user ID of one of the users within this direct-message chat room.</param>
        /// <param name="userTwo">The user ID of the other user within this direct-message chat room.</param>
        public DirectChatRoomInfo(string name, Guid userOne, Guid userTwo)
            : this(default, name, userOne, userTwo)
        {
        }
    }
}