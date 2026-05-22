using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using Sphynx.Model.Room;

namespace Sphynx.Server.Chat.Persistence.Room
{
    [BsonIgnoreExtraElements]
    public class SphynxDbGroupRoom
    {
        [BsonId(IdGenerator = typeof(CombGuidGenerator))]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid RoomId { get; set; }

        public SphynxRoomType RoomType => SphynxRoomType.GROUP;

        [BsonElement("name")]
        public string Name { get; set; } = null!;

        [BsonElement("is_public")]
        public bool IsPublic { get; set; }

        [BsonElement("owner_id")]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid OwnerId { get; set; }

        [BsonElement("pwd")]
        public string? PasswordHash { get; set; }

        [BsonElement("pwd_salt")]
        public string? PasswordSalt { get; set; }

        // Denormalize
        [BsonElement("created_at")]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTimeOffset CreatedAt { get; set; }
    }
}
