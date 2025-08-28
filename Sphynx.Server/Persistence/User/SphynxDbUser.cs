using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using Sphynx.Core;
using Sphynx.Model.User;

namespace Sphynx.Server.Persistence.User
{
    /// <summary>
    /// Represents a complete representation of a <c>Sphynx</c> user within the database.
    /// </summary>
    /// <seealso cref="Model.User.SphynxSelfInfo"/>
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

        [BsonElement("friends")]
        public HashSet<Guid> Friends { get; set; } = new();

        [BsonElement("rooms")]
        public HashSet<Guid> Rooms { get; set; } = new();

        [BsonElement("last_read")]
        public LastReadDbMessages LastReadMessages { get; set; } = new();

        [BsonElement("out_reqs")]
        public HashSet<Guid> OutgoingFriendRequests { get; set; } = new();

        [BsonElement("inc_recs")]
        public HashSet<Guid> IncomingFriendRequests { get; set; } = new();

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

        public SphynxDbUser() : this(default, null!, default)
        {
        }

        public SphynxDbUser(Guid userId, string userName, SphynxUserStatus userStatus)
        {
            UserId = userId;
            UserName = userName;
            UserStatus = userStatus;
        }

        public SphynxDbUser(Guid userId,
            string userName,
            SphynxUserStatus userStatus,
            string? password,
            string? passwordSalt,
            ISet<Guid> friends,
            ISet<Guid> rooms,
            LastReadDbMessages lastReadMessages,
            ISet<Guid> outgoingFriendRequests,
            ISet<Guid> incomingFriendRequests)
            : this(userId, userName, userStatus)
        {
            Friends = friends as HashSet<Guid> ?? new HashSet<Guid>(friends);
            Rooms = rooms as HashSet<Guid> ?? new HashSet<Guid>(rooms);
            LastReadMessages = lastReadMessages;
            OutgoingFriendRequests = outgoingFriendRequests as HashSet<Guid> ?? new HashSet<Guid>(outgoingFriendRequests);
            IncomingFriendRequests = incomingFriendRequests as HashSet<Guid> ?? new HashSet<Guid>(incomingFriendRequests);
            Password = password;
            PasswordSalt = passwordSalt;
        }

        /// <inheritdoc/>
        public bool Equals(SphynxDbUser? other) => UserId == other?.UserId;

        /// <inheritdoc/>
        public override int GetHashCode() => UserId.GetHashCode();
    }
}
