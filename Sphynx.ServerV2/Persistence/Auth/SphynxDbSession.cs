// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace Sphynx.ServerV2.Persistence.Auth
{
    [BsonIgnoreExtraElements]
    public class SphynxDbSession
    {
        [BsonId(IdGenerator = typeof(CombGuidGenerator))]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid SessionId { get; set; }

        [BsonElement("user_id")]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid UserId { get; set; }

        [BsonElement("ip_address")]
        public string IpAddress { get; set; } = null!;

        [BsonElement("exp")]
        [BsonRepresentation(BsonType.String)]
        public DateTimeOffset ExpiresAt { get; set; }

        [BsonElement("created_at")]
        [BsonRepresentation(BsonType.String)]
        public DateTimeOffset CreatedAt { get; set; }
    }
}
