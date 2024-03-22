namespace Sphynx.ChatRoom
{
    /// <summary>
    /// Holds information about a group chat room with visibility options.
    /// </summary>
    public interface IGroupChatRoomInfo : IChatRoomInfo
    {
        /// <summary>
        /// Whether this room is public.
        /// </summary>
        public bool Public { get; }
    }
}
