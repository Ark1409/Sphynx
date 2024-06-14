using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using Sphynx.ChatRoom;
using Sphynx.Server.Storage;

namespace Sphynx.Server.ChatRoom
{
    /// <summary>
    /// Holds information about a group chat room with visibility options.
    /// </summary>
    [BsonIgnoreExtraElements]
    public class GroupChatRoomDbInfo : GroupChatRoomInfo, IIdentifiable<Guid>
    {
        public const string NAME_FIELD = "name";
        public const string USERS_FIELD = "users";
        public const string ROOM_TYPE_FIELD = "room_type";
        public const string VISIBILITY_FIELD = "is_public";
        internal const string PASSWORD_FIELD = "pwd";
        internal const string PASSWORD_SALT_FIELD = "pwd_salt";

        /// <inheritdoc/>
        [BsonElement(NAME_FIELD)]
        public override string Name { get; set; }

        /// <inheritdoc/>
        [BsonIgnore]
        public override Guid RoomId => Id;

        /// <inheritdoc/>
        [BsonId(IdGenerator = typeof(CombGuidGenerator))]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; internal set; }

        /// <inheritdoc/>
        [BsonElement(USERS_FIELD)]
        public override HashSet<Guid> Users { get; set; }

        /// <inheritdoc/>
        [BsonElement(ROOM_TYPE_FIELD)]
        public override ChatRoomType RoomType { get; set; }

        /// <inheritdoc/>
        [BsonElement(VISIBILITY_FIELD)]
        public override bool Public { get; set; }

        /// <summary>
        /// The password for this group chat.
        /// </summary>
        [BsonElement(PASSWORD_FIELD)]
        internal override string? Password { get; set; }

        /// <summary>
        /// The salt for the password of this group chat.
        /// </summary>
        [BsonElement(PASSWORD_SALT_FIELD)]
        internal override string? PasswordSalt { get; set; }

        /// <inheritdoc/>
        public GroupChatRoomDbInfo(Guid roomId, string name, bool @public, IEnumerable<Guid>? userIds = null)
            : base(roomId, name, @public, userIds)
        {
        }

        /// <inheritdoc/>
        public GroupChatRoomDbInfo(string name, bool @public, IEnumerable<Guid>? userIds = null)
            : base(name, @public, userIds)
        {
        }

        /// <inheritdoc/>
        public GroupChatRoomDbInfo(string name, bool @public, params Guid[] userIds)
            : base(name, @public, userIds)
        {
        }

        /// <inheritdoc/>
        internal GroupChatRoomDbInfo(Guid roomId, string name, byte[] pwd, byte[] pwdSalt, bool @public = true, IEnumerable<Guid>? userIds = null)
            : base(roomId, name, pwd, pwdSalt, @public, userIds)
        {
        }

        /// <inheritdoc/>
        internal GroupChatRoomDbInfo(string name, byte[] pwd, byte[] pwdSalt, bool @public = true, IEnumerable<Guid>? userIds = null)
            : base(name, pwd, pwdSalt, @public, userIds)
        {
        }

        /// <inheritdoc/>
        internal GroupChatRoomDbInfo(string name, byte[] pwd, byte[] pwdSalt, bool @public = true, params Guid[] userIds)
            : base(name, pwd, pwdSalt, @public, userIds)
        {
        }

        /// <inheritdoc/>
        internal GroupChatRoomDbInfo(Guid roomId, string name, string pwd, string pwdSalt, bool @public = true, IEnumerable<Guid>? userIds = null)
            : base(roomId, name, pwd, pwdSalt, @public, userIds)
        {
        }
        
        /// <inheritdoc/>
        internal GroupChatRoomDbInfo(string name, string pwd, string pwdSalt, bool @public = true, IEnumerable<Guid>? userIds = null)
            : base(name, pwd, pwdSalt, @public, userIds)
        {
        }
        
        /// <inheritdoc/>
        internal GroupChatRoomDbInfo(string name, string pwd, string pwdSalt, bool @public = true, params Guid[] userIds)
            : base(name, pwd, pwdSalt, @public, userIds)
        {
        }
    }
}