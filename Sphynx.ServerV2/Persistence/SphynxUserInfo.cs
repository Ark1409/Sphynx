// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using Sphynx.Core;
using Sphynx.ModelV2.User;

namespace Sphynx.ServerV2.Persistence
{
    /// <summary>
    /// Represents a public view of a <c>Sphynx</c> user.
    /// This representation only holds publicly available information about the user from the database.
    /// </summary>
    public class SphynxUserInfo : ISphynxUserInfo
    {
        [BsonId(IdGenerator = typeof(CombGuidGenerator))]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public SnowflakeId UserId { get; set; }

        [BsonElement("name")]
        public string UserName { get; set; }

        [BsonElement("status")]
        public SphynxUserStatus UserStatus { get; set; }

        public SphynxUserInfo(SnowflakeId userId, string userName, SphynxUserStatus userStatus)
        {
            UserId = userId;
            UserName = userName;
            UserStatus = userStatus;
        }

        public bool Equals(ISphynxUserInfo? other) => UserId == other?.UserId;

        public override int GetHashCode() => UserId.GetHashCode();
    }
}
