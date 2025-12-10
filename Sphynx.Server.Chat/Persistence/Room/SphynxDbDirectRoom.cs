using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using Sphynx.Model.Room;

namespace Sphynx.Server.Chat.Persistence.Room
{
    [BsonIgnoreExtraElements]
    public class SphynxDbDirectRoom
    {
        [BsonId(IdGenerator = typeof(CombGuidGenerator))]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid RoomId { get; set; }

        public SphynxRoomType RoomType => SphynxRoomType.DIRECT_MSG;

        [BsonElement("user_a")]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid UserA { get; set; }

        [BsonElement("user_b")]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid UserB { get; set; }

        // Denormalize
        [BsonElement("created_at")]
        [BsonRepresentation(BsonType.String)]
        public DateTimeOffset CreatedAt { get; set; }
    }
}
