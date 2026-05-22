// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using Sphynx.Model.Room;

namespace Sphynx.Server.Chat.Persistence.Room
{
    [BsonIgnoreExtraElements]
    public class SphynxDbRoom
    {
        [BsonId(IdGenerator = typeof(CombGuidGenerator))]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid RoomId { get; set; }

        [BsonElement("type")]
        [BsonRepresentation(BsonType.String)]
        public SphynxRoomType RoomType { get; set; }

        [BsonElement("created_at")]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTimeOffset CreatedAt { get; set; }
    }
}
