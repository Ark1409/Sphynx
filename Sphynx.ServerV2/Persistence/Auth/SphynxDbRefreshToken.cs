// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using Sphynx.Core;

namespace Sphynx.ServerV2.Persistence.Auth
{
    [BsonIgnoreExtraElements]
    public class SphynxDbRefreshToken
    {
        [BsonId(IdGenerator = typeof(CombGuidGenerator))]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid RefreshToken { get; set; }

        [BsonElement("access_token")]
        public string AccessToken { get; set; } = null!;

        [BsonElement("user_id")]
        public SnowflakeId User { get; set; }

        [BsonElement("exp")]
        [BsonRepresentation(BsonType.String)]
        public DateTimeOffset ExpiryTime { get; set; }

        [BsonElement("created_at")]
        [BsonRepresentation(BsonType.String)]
        public DateTimeOffset CreatedAt { get; set; }
    }
}
