using MongoDB.Bson.Serialization.Attributes;
using Sphynx.Core;
using Sphynx.ModelV2.User;

namespace Sphynx.ServerV2.Persistence.User
{
    /// <summary>
    /// Represents a complete representation of a <c>Sphynx</c> user within the database.
    /// </summary>
    /// <seealso cref="ModelV2.User.SphynxSelfInfo"/>
    [BsonIgnoreExtraElements]
    public class SphynxDbUser : IEquatable<SphynxDbUser>
    {
        [BsonId]
        public SnowflakeId UserId { get; set; }

        [BsonElement("name")]
        public string UserName { get; set; }

        [BsonElement("status")]
        public SphynxUserStatus UserStatus { get; set; }

        [BsonElement("friends")]
        public HashSet<SnowflakeId> Friends { get; set; } = new();

        [BsonElement("rooms")]
        public HashSet<SnowflakeId> Rooms { get; set; } = new();

        [BsonElement("last_read")]
        public LastReadDbMessages LastReadMessages { get; set; } = new();

        [BsonElement("out_reqs")]
        public HashSet<SnowflakeId> OutgoingFriendRequests { get; set; } = new();

        [BsonElement("inc_recs")]
        public HashSet<SnowflakeId> IncomingFriendRequests { get; set; } = new();

        /// <summary>
        /// The hashed password for this Sphynx user, as a base-64 string.
        /// </summary>
        [BsonElement("pwd")]
        public string? Password { get; set; }

        /// <summary>
        /// The salt for the password of this Sphynx user.
        /// </summary>
        [BsonElement("pwd_salt")]
        public string? PasswordSalt { get; set; }

        public SphynxDbUser(SnowflakeId userId, string userName, SphynxUserStatus userStatus)
        {
            UserId = userId;
            UserName = userName;
            UserStatus = userStatus;
        }

        public SphynxDbUser(SnowflakeId userId,
            string userName,
            SphynxUserStatus userStatus,
            string? password,
            string? passwordSalt,
            ISet<SnowflakeId> friends,
            ISet<SnowflakeId> rooms,
            LastReadDbMessages lastReadMessages,
            ISet<SnowflakeId> outgoingFriendRequests,
            ISet<SnowflakeId> incomingFriendRequests)
            : this(userId, userName, userStatus)
        {
            Friends = friends as HashSet<SnowflakeId> ?? new HashSet<SnowflakeId>(friends);
            Rooms = rooms as HashSet<SnowflakeId> ?? new HashSet<SnowflakeId>(rooms);
            LastReadMessages = lastReadMessages;
            OutgoingFriendRequests = outgoingFriendRequests as HashSet<SnowflakeId> ?? new HashSet<SnowflakeId>(outgoingFriendRequests);
            IncomingFriendRequests = incomingFriendRequests as HashSet<SnowflakeId> ?? new HashSet<SnowflakeId>(incomingFriendRequests);
            Password = password;
            PasswordSalt = passwordSalt;
        }

        /// <inheritdoc/>
        public bool Equals(SphynxDbUser? other) => UserId == other?.UserId;

        /// <inheritdoc/>
        public override int GetHashCode() => UserId.GetHashCode();
    }
}
