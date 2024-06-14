namespace Sphynx.ChatRoom
{
    /// <summary>
    /// Holds information about a group chat room with visibility options.
    /// </summary>
    public class GroupChatRoomInfo : ChatRoomInfo
    {
        /// <summary>
        /// Whether this room is public.
        /// </summary>
        public virtual bool Public { get; set; }

        /// <summary>
        /// The password for this group chat.
        /// </summary>
        internal virtual string? Password { get; set; }

        /// <summary>
        /// The salt for the password of this group chat.
        /// </summary>
        internal virtual string? PasswordSalt { get; set; }

        /// <inheritdoc/>
        /// <param name="public">Whether this room is public.</param>
        public GroupChatRoomInfo(Guid roomId, string name, bool @public = true, IEnumerable<Guid>? userIds = null)
            : base(roomId, ChatRoomType.GROUP, name, userIds)
        {
            Public = @public;
        }

        /// <inheritdoc/>
        public GroupChatRoomInfo(string name, bool @public = true, IEnumerable<Guid>? userIds = null)
            : this(default, name, @public, userIds)
        {
        }

        /// <inheritdoc/>
        public GroupChatRoomInfo(Guid roomId, string name, bool @public = true, params Guid[] userIds)
            : this(roomId, name, @public, (IEnumerable<Guid>)userIds)
        {
        }

        /// <inheritdoc/>
        public GroupChatRoomInfo(string name, bool @public = true, params Guid[] userIds)
            : this(default, name, @public, (IEnumerable<Guid>)userIds)
        {
        }

        /// <inheritdoc/>
        internal GroupChatRoomInfo(Guid roomId, string name, byte[] pwd, byte[] pwdSalt, bool @public = true, IEnumerable<Guid>? userIds = null)
            : this(roomId, name, Convert.ToBase64String(pwd), Convert.ToBase64String(pwdSalt), @public, userIds)
        {
        }

        /// <inheritdoc/>
        internal GroupChatRoomInfo(Guid roomId, string name, byte[] pwd, byte[] pwdSalt, bool @public = true, params Guid[] userIds)
            : this(roomId, name, Convert.ToBase64String(pwd), Convert.ToBase64String(pwdSalt), @public, userIds)
        {
        }

        /// <inheritdoc/>
        internal GroupChatRoomInfo(string name, byte[] pwd, byte[] pwdSalt, bool @public = true, IEnumerable<Guid>? userIds = null)
            : this(default, name, Convert.ToBase64String(pwd), Convert.ToBase64String(pwdSalt), @public, userIds)
        {
        }

        /// <inheritdoc/>
        internal GroupChatRoomInfo(Guid roomId, string name, string pwd, string pwdSalt, bool @public = true, IEnumerable<Guid>? userIds = null)
            : this(roomId, name, @public, userIds)
        {
            Password = pwd;
            PasswordSalt = pwdSalt;
        }

        /// <inheritdoc/>
        internal GroupChatRoomInfo(Guid roomId, string name, string pwd, string pwdSalt, bool @public = true, params Guid[] userIds)
            : this(roomId, name, @public, userIds)
        {
            Password = pwd;
            PasswordSalt = pwdSalt;
        }

        /// <inheritdoc/>
        internal GroupChatRoomInfo(string name, string pwd, string pwdSalt, bool @public = true, IEnumerable<Guid>? userIds = null)
            : this(default, name, @public, userIds)
        {
            Password = pwd;
            PasswordSalt = pwdSalt;
        }
    }
}