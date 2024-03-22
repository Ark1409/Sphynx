namespace Sphynx.ChatRoom
{
    /// <summary>
    /// Holds information about a direct-message chat room.
    /// </summary>
    public interface IDirectChatRoomInfo : IChatRoomInfo
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
    }
}