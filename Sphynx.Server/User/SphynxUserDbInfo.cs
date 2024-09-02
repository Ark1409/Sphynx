using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using Sphynx.Core;
using Sphynx.Server.Storage;

namespace Sphynx.Server.User
{
    /// <summary>
    /// A shallow representation a <see cref="SphynxUserInfo"/> document within the MongoDB database,
    /// excluding private information and/or credentials.
    /// </summary>
    /// <summary>
    /// Represents a complete <see cref="SphynxUserInfo"/> document within the MongoDB database.
    /// </summary>
    [BsonIgnoreExtraElements]
    public class SphynxUserDbInfo : SphynxUserInfo, IEquatable<SphynxUserDbInfo>, IIdentifiable<Guid>
    {
        public const string NAME_FIELD = "name";
        public const string STATUS_FIELD = "status";
        public const string FRIENDS_FIELD = "friends";
        public const string ROOMS_FIELD = "rooms";
        internal const string PASSWORD_FIELD = "pwd";
        internal const string PASSWORD_SALT_FIELD = "pwd_salt";

        /// <inheritdoc/>
        [BsonIgnore]
        public override Guid UserId
        {
            get => Id;
            set => Id = value;
        }

        /// <inheritdoc/>
        [BsonId(IdGenerator = typeof(CombGuidGenerator))]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; internal set; }

        /// <inheritdoc/>
        [BsonElement(NAME_FIELD)]
        public override string UserName { get; set; }

        /// <inheritdoc/>
        [BsonElement(STATUS_FIELD)]
        public override SphynxUserStatus UserStatus { get; set; }

        /// <inheritdoc/>
        [BsonElement(FRIENDS_FIELD)]
        public override ISet<Guid>? Friends { get; set; }

        /// <inheritdoc/>
        [BsonElement(ROOMS_FIELD)]
        public override ISet<Guid>? Rooms { get; set; }

        /// <summary>
        /// The hashed password for this Sphynx user, as a base-64 string.
        /// </summary>
        [BsonElement(PASSWORD_FIELD)]
        internal string? Password { get; set; }

        /// <summary>
        /// The salt for the password of this Sphynx user.
        /// </summary>
        [BsonElement(PASSWORD_SALT_FIELD)]
        internal string? PasswordSalt { get; set; }

        /// <inheritdoc/>
        public SphynxUserDbInfo(Guid userId, string userName, SphynxUserStatus status,
            IEnumerable<Guid>? friends = null,
            IEnumerable<Guid>? rooms = null)
            : base(userId, userName, status, friends, rooms)
        {
        }

        /// <summary>
        /// Creates a new <see cref="SphynxUserDbInfo"/>.
        /// </summary>
        /// <param name="userName">The username of the Sphynx user.</param>
        /// <param name="encodedPwd">The password for this Sphynx user.</param>
        /// <param name="encodedSalt">The salt for the password of this Sphynx user.</param>
        /// <param name="status">The activity status of the Sphynx user.</param>
        /// <param name="friends">User IDs of friends for this user.</param>
        internal SphynxUserDbInfo(string userName, byte[] encodedPwd, byte[] encodedSalt, SphynxUserStatus status,
            IEnumerable<Guid>? friends = null,
            IEnumerable<Guid>? rooms = null)
            : this(default, userName, encodedPwd, encodedSalt, status, friends, rooms)
        {
        }

        /// <summary>
        /// Creates a new <see cref="SphynxUserDbInfo"/>.
        /// </summary>
        /// <param name="userName">The username of the Sphynx user.</param>
        /// <param name="encodedPwd">The password for this Sphynx user.</param>
        /// <param name="encodedSalt">The salt for the password of this Sphynx user.</param>
        /// <param name="status">The activity status of the Sphynx user.</param>
        /// <param name="friends">User IDs of friends for this user.</param>
        internal SphynxUserDbInfo(string userName, string encodedPwd, string encodedSalt, SphynxUserStatus status,
            IEnumerable<Guid>? friends = null,
            IEnumerable<Guid>? rooms = null)
            : this(default, userName, encodedPwd, encodedSalt, status, friends, rooms)
        {
        }

        /// <summary>
        /// Creates a new <see cref="SphynxUserDbInfo"/>.
        /// </summary>
        /// <param name="userId">The user ID of the Sphynx user.</param>
        /// <param name="userName">The username of the Sphynx user.</param>
        /// <param name="encodedPwd">The password for this Sphynx user, as a base-64 string</param>
        /// <param name="encodedSalt">The salt for the password of this Sphynx user, as a base-64 string</param>
        /// <param name="status">The activity status of the Sphynx user.</param>
        /// <param name="friends">User IDs of friends for this user.</param>
        /// <param name="rooms">Room IDs of chat rooms which this user is in (including DMs).</param>
        public SphynxUserDbInfo(Guid userId, string userName, byte[] encodedPwd, byte[] encodedSalt, SphynxUserStatus status,
            IEnumerable<Guid>? friends = null,
            IEnumerable<Guid>? rooms = null)
            : this(userId, userName, Convert.ToBase64String(encodedPwd), Convert.ToBase64String(encodedSalt), status,
                friends, rooms)
        {
        }

        /// <summary>
        /// Creates a new <see cref="SphynxUserDbInfo"/>.
        /// </summary>
        /// <param name="userId">The user ID of the Sphynx user.</param>
        /// <param name="userName">The username of the Sphynx user.</param>
        /// <param name="encodedPwd">The password for this Sphynx user, as a base-64 string.</param>
        /// <param name="encodedSalt">The salt for the password of this Sphynx user, as a base-64 string</param>
        /// <param name="status">The activity status of the Sphynx user.</param>
        /// <param name="friends">User IDs of friends for this user.</param>
        /// <param name="rooms">Room IDs of chat rooms which this user is in (including DMs).</param>
        public SphynxUserDbInfo(Guid userId, string userName, string encodedPwd, string encodedSalt, SphynxUserStatus status,
            IEnumerable<Guid>? friends = null,
            IEnumerable<Guid>? rooms = null)
            : this(userId, userName, status, friends, rooms)
        {
            Password = encodedPwd;
            PasswordSalt = encodedSalt;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => Equals(obj as SphynxUserInfo);

        /// <inheritdoc/>
        public bool Equals(SphynxUserDbInfo? other) => Equals(other as SphynxUserInfo);

        /// <inheritdoc/>
        public override int GetHashCode() => UserId.GetHashCode();
    }
}