using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using Sphynx.ChatRoom;
using Sphynx.Server.Storage;

namespace Sphynx.Server.ChatRoom
{
    /// <summary>
    /// Holds information about a direct-message chat room.
    /// </summary>
    [BsonIgnoreExtraElements]
    public class DirectChatRoomDbInfo : DirectChatRoomInfo, IIdentifiable<Guid>
    {
        public const string NAME_FIELD = "name";
        public const string USERS_FIELD = "users";
        public const string ROOM_TYPE_FIELD = "room_type";

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
        public DirectChatRoomDbInfo(Guid roomId, string name, Guid userOne, Guid userTwo)
            : base(roomId, name, userOne, userTwo)
        {
        }
        
        /// <inheritdoc/>
        public DirectChatRoomDbInfo(string name, Guid userOne, Guid userTwo)
            : base(name, userOne, userTwo)
        {
        }
    }
}