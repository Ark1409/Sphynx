// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using Sphynx.Core;
using Sphynx.ModelV2.User;

namespace Sphynx.ServerV2.Persistence.User
{
    /// <summary>
    /// Represents a public view of a <c>Sphynx</c> user.
    /// This representation only holds publicly available information about the user from the database.
    /// </summary>
    public class SphynxUserInfo : ModelV2.User.SphynxUserInfo
    {
        public const string ID_FIELD = "_id";
        public const string NAME_FIELD = "name";
        public const string STATUS_FIELD = "status";

        [BsonId]
        public SnowflakeId UserId { get; set; }

        [BsonElement(NAME_FIELD)]
        public string UserName { get; set; }

        [BsonElement(STATUS_FIELD)]
        public SphynxUserStatus UserStatus { get; set; }

        public SphynxUserInfo(SnowflakeId userId, string userName, SphynxUserStatus userStatus)
        {
            UserId = userId;
            UserName = userName;
            UserStatus = userStatus;
        }

        public bool Equals(ModelV2.User.SphynxUserInfo? other) => UserId == other?.UserId;

        public override int GetHashCode() => UserId.GetHashCode();
    }
}
