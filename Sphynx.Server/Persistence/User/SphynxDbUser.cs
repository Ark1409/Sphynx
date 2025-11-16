using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using Sphynx.Model.User;

namespace Sphynx.Server.Persistence.User
{
    /// <summary>
    /// Represents a complete representation of a <c>Sphynx</c> user within the database.
    /// </summary>
    /// <seealso cref="SphynxSelfInfo"/>
    [BsonIgnoreExtraElements]
    public class SphynxDbUser : IEquatable<SphynxDbUser>
    {
        [BsonId(IdGenerator = typeof(CombGuidGenerator))]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid UserId { get; set; }

        [BsonElement("name")]
        public string UserName { get; set; }

        [BsonElement("status")]
        public SphynxUserStatus UserStatus { get; set; }

        /// <summary>
        /// The hashed password for this Sphynx user, as a base-64 string.
        /// </summary>
        [BsonElement("pwd")]
        public string Password { get; set; } = null!;

        /// <summary>
        /// The salt for the password of this Sphynx user.
        /// </summary>
        [BsonElement("pwd_salt")]
        public string PasswordSalt { get; set; } = null!;

        [BsonElement("last_login")]
        [BsonRepresentation(BsonType.String)]
        public DateTimeOffset LastLogin { get; set; }

        [BsonElement("created_at")]
        [BsonRepresentation(BsonType.String)]
        public DateTimeOffset CreatedAt { get; set; }

        [BsonElement("last_read")]
        public LastReadDbMessages LastReadMessages { get; set; } = new();

        public SphynxDbUser() : this(default, null!, default)
        {
        }

        public SphynxDbUser(Guid userId, string userName, SphynxUserStatus userStatus)
        {
            UserId = userId;
            UserName = userName;
            UserStatus = userStatus;
        }

        public SphynxDbUser(Guid userId, string userName, SphynxUserStatus userStatus, string password, string passwordSalt, DateTimeOffset createdAt)
        {
            UserId = userId;
            UserName = userName;
            UserStatus = userStatus;
            Password = password;
            PasswordSalt = passwordSalt;
            CreatedAt = createdAt;
        }

        /// <inheritdoc/>
        public bool Equals(SphynxDbUser? other) => UserId == other?.UserId;

        /// <inheritdoc/>
        public override int GetHashCode() => UserId.GetHashCode();
    }
}
