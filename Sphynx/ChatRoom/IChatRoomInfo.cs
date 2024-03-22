namespace Sphynx.ChatRoom
{
    /// <summary>
    /// Holds information about a chat room containing Sphynx users.
    /// </summary>
    public interface IChatRoomInfo : IEquatable<IChatRoomInfo?>
    {
        /// <summary>
        /// The name of this chat room.
        /// </summary>
        public string Name { get; }
        
        /// <summary>
        /// The unique ID of this room.
        /// </summary>
        public Guid RoomId { get; }
        
        /// <summary>
        /// A collection of the user IDs of the users within this chat room.
        /// </summary>
        public ICollection<Guid> Users { get; }
        
        /// <summary>
        /// Returns the type of this <see cref="IChatRoomInfo"/>.
        /// </summary>
        public ChatRoomType RoomType { get; }
    }
}