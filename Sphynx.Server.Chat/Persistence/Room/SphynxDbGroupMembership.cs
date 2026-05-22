using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace Sphynx.Server.Chat.Persistence.Room
{
    [BsonIgnoreExtraElements]
    public class SphynxDbGroupMembership
    {
        [BsonId(IdGenerator = typeof(CombGuidGenerator))]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid MembershipId { get; set; }

        [BsonElement("room_id")]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid RoomId { get; set; }

        [BsonElement("member_id")]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid MemberId { get; set; }

        [BsonElement("is_owner")]
        public bool IsOwner { get; set; }

        [BsonElement("joined_at")]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTimeOffset JoinedAt { get; set; }
    }
}
