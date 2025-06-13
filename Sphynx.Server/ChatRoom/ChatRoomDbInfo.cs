using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using Sphynx.Model.ChatRoom;
using Sphynx.Server.Storage;

namespace Sphynx.Server.ChatRoom
{
    /// <summary>
    /// Holds information about a chat room containing Sphynx users.
    /// </summary>
    public sealed class ChatRoomDbInfo
    {
        public const string NAME_FIELD = "name";
        public const string USERS_FIELD = "users";
        public const string ROOM_TYPE_FIELD = "room_type";

        // Should only be creating instances of inner-classes
        private ChatRoomDbInfo()
        {
        }

        /// <summary>
        /// Holds information about a direct-message chat room.
        /// </summary>
        [BsonIgnoreExtraElements]
        public class Direct : ChatRoomInfo.Direct, IIdentifiable<Guid>
        {
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
            public override ISet<Guid> Users { get; set; }

            /// <inheritdoc/>
            [BsonElement(ROOM_TYPE_FIELD)]
            public override ChatRoomType RoomType { get; set; }

            /// <inheritdoc/>
            public Direct(Guid roomId, string name, Guid userOne, Guid userTwo)
                : base(roomId, name, userOne, userTwo)
            {
            }

            /// <inheritdoc/>
            public Direct(string name, Guid userOne, Guid userTwo)
                : base(name, userOne, userTwo)
            {
            }
        }

        /// <summary>
        /// Holds information about a group chat room with visibility options.
        /// </summary>
        [BsonIgnoreExtraElements]
        public class Group : ChatRoomInfo.Group, IIdentifiable<Guid>
        {
            public const string VISIBILITY_FIELD = "is_public";
            public const string OWNER_FIELD = "owner_id";
            internal const string PASSWORD_FIELD = "pwd";
            internal const string PASSWORD_SALT_FIELD = "pwd_salt";

            /// <inheritdoc/>
            [BsonElement(NAME_FIELD)]
            public override string Name { get; set; }

            /// <inheritdoc/>
            [BsonIgnore]
            public override Guid RoomId
            {
                get => Id;
                set => Id = value;
            }

            /// <inheritdoc/>
            [BsonId(IdGenerator = typeof(CombGuidGenerator))]
            [BsonGuidRepresentation(GuidRepresentation.Standard)]
            public Guid Id { get; internal set; }

            /// <inheritdoc/>
            [BsonElement(USERS_FIELD)]
            public override ISet<Guid> Users { get; set; }

            /// <inheritdoc/>
            [BsonElement(ROOM_TYPE_FIELD)]
            public override ChatRoomType RoomType { get; set; }

            /// <inheritdoc/>
            [BsonElement(VISIBILITY_FIELD)]
            public override bool Public { get; set; }

            /// <inheritdoc/>
            [BsonElement(OWNER_FIELD)]
            public override Guid OwnerId { get; set; }

            /// <summary>
            /// The password for this group chat.
            /// </summary>
            [BsonElement(PASSWORD_FIELD)]
            internal override string? Password { get; set; }

            /// <summary>
            /// The salt for the password of this group chat.
            /// </summary>
            [BsonElement(PASSWORD_SALT_FIELD)]
            internal string? PasswordSalt { get; set; }

            /// <inheritdoc/>
            public Group(Guid roomId, Guid ownerId, string name, bool @public, IEnumerable<Guid>? userIds = null)
                : base(roomId, ownerId, name, @public, userIds)
            {
            }

            /// <inheritdoc/>
            public Group(Guid ownerId, string name, bool @public, IEnumerable<Guid>? userIds = null)
                : base(ownerId, name, @public, userIds)
            {
            }

            /// <inheritdoc/>
            public Group(Guid ownerId, string name, bool @public, params Guid[] userIds)
                : base(ownerId, name, @public, userIds)
            {
            }

            /// <inheritdoc/>
            internal Group(Guid roomId, Guid ownerId, string name, byte[] pwd, byte[] pwdSalt, bool @public = true, IEnumerable<Guid>? userIds = null)
                : base(roomId, ownerId, name, pwd, @public, userIds)
            {
            }

            /// <inheritdoc/>
            internal Group(Guid ownerId, string name, byte[] pwd, byte[] pwdSalt, bool @public = true, IEnumerable<Guid>? userIds = null)
                : base(ownerId, name, pwd, @public, userIds)
            {
            }

            /// <inheritdoc/>
            internal Group(Guid ownerId, string name, byte[] pwd, byte[] pwdSalt, bool @public = true, params Guid[] userIds)
                : base(ownerId, name, pwd, @public, userIds)
            {
            }

            /// <inheritdoc/>
            internal Group(Guid roomId, Guid ownerId, string name, string pwd, string pwdSalt, bool @public = true, IEnumerable<Guid>? userIds = null)
                : base(ownerId, roomId, name, pwd, @public, userIds)
            {
            }
        }
    }
}