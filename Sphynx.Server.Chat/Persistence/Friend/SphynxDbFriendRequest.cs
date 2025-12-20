// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace Sphynx.Server.Chat.Persistence.Friend
{
    [BsonIgnoreExtraElements]
    public class SphynxDbFriendRequest
    {
        [BsonId(IdGenerator = typeof(CombGuidGenerator))]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid RequestId { get; set; }

        [BsonElement("initiator_id")]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid InitiatorId { get; set; }

        [BsonElement("other_id")]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid OtherId { get; set; }

        [BsonElement("created_at")]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTimeOffset SentAt { get; set; }
    }
}
