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
        internal const string PASSWORD_FIELD = "pwd";
        internal const string PASSWORD_SALT_FIELD = "pwd_salt";

        /// <inheritdoc/>
        [BsonIgnore]
        public override Guid UserId => Id;

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

        /// <summary>
        /// User IDs of friends for this user.
        /// </summary>
        [BsonElement(FRIENDS_FIELD)]
        public override HashSet<Guid> Friends { get; set; }

        /// <summary>
        /// The password for this Sphynx user.
        /// </summary>
        [BsonElement(PASSWORD_FIELD)]
        internal override string? Password { get; set; }

        /// <summary>
        /// The salt for the password of this Sphynx user.
        /// </summary>
        [BsonElement(PASSWORD_SALT_FIELD)]
        internal override string? PasswordSalt { get; set; }
        
        /// <inheritdoc/>
        public SphynxUserDbInfo(Guid userId, string userName, SphynxUserStatus status, IEnumerable<Guid>? friends = null)
            : base(userId, userName, status, friends!)
        {
        }

        /// <inheritdoc/>
        public SphynxUserDbInfo(string userName, SphynxUserStatus status, IEnumerable<Guid>? friends = null)
            : base(default, userName, status, friends)
        {
        }

        /// <inheritdoc/>
        internal SphynxUserDbInfo(Guid userId, string userName, byte[] pwd, byte[] pwdSalt, SphynxUserStatus status,
            IEnumerable<Guid>? friends = null)
            : base(userId, userName, pwd, pwdSalt, status, friends)
        {
        }
        

        /// <inheritdoc/>
        internal SphynxUserDbInfo(Guid userId, string userName, string pwd, string pwdSalt, SphynxUserStatus status,
            IEnumerable<Guid>? friends = null)
            : base(userId, userName, pwd, pwdSalt, status, friends)
        {
        }
        
        /// <summary>
        /// Creates a new <see cref="SphynxUserDbInfo"/>.
        /// </summary>
        /// <param name="userName">The username of the Sphynx user.</param>
        /// <param name="pwd">The password for this Sphynx user.</param>
        /// <param name="pwdSalt">The salt for the password of this Sphynx user.</param>
        /// <param name="status">The activity status of the Sphynx user.</param>
        /// <param name="friends">User IDs of friends for this user.</param>
        internal SphynxUserDbInfo(string userName, byte[] pwd, byte[] pwdSalt, SphynxUserStatus status, IEnumerable<Guid>? friends = null)
            : this(default, userName, pwd, pwdSalt, status, friends)
        {
        }

        /// <summary>
        /// Creates a new <see cref="SphynxUserDbInfo"/>.
        /// </summary>
        /// <param name="userName">The username of the Sphynx user.</param>
        /// <param name="pwd">The password for this Sphynx user.</param>
        /// <param name="pwdSalt">The salt for the password of this Sphynx user.</param>
        /// <param name="status">The activity status of the Sphynx user.</param>
        /// <param name="friends">User IDs of friends for this user.</param>
        internal SphynxUserDbInfo(string userName, string pwd, string pwdSalt, SphynxUserStatus status, IEnumerable<Guid>? friends = null)
            : this(default, userName, pwd, pwdSalt, status, friends)
        {
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => Equals(obj as SphynxUserInfo);

        /// <inheritdoc/>
        public bool Equals(SphynxUserDbInfo? other) => Equals(other as SphynxUserInfo);

        /// <inheritdoc/>
        public override int GetHashCode() => UserId.GetHashCode();
    }
}