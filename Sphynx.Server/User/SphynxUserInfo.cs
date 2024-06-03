using System.Diagnostics;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using Sphynx.Core;
using Sphynx.Server.Storage;

namespace Sphynx.Server.User
{
    /// <summary>
    /// A shallow representation a <see cref="ISphynxUserInfo"/> document within the MongoDB database,
    /// excluding private information and/or credentials.
    /// </summary>
    [BsonIgnoreExtraElements]
    public class SphynxUserInfo : ISphynxUserInfo, IEquatable<SphynxUserInfo>, IIdentifiable<Guid>
    {
        public const string NAME_FIELD = "name";
        public const string STATUS_FIELD = "status";
        public const string FRIENDS_FIELD = "friends";

        /// <inheritdoc/>
        [BsonIgnore]
        public Guid UserId => Id;

        /// <inheritdoc/>
        [BsonId(IdGenerator = typeof(CombGuidGenerator))]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; internal set; }

        /// <inheritdoc/>
        [BsonElement(NAME_FIELD)]
        public string UserName { get; internal set; }

        /// <inheritdoc/>
        [BsonElement(STATUS_FIELD)]
        public SphynxUserStatus UserStatus { get; internal set; }

        /// <summary>
        /// User IDs of friends for this user.
        /// </summary>
        [BsonElement(FRIENDS_FIELD)]
        public HashSet<Guid> Friends { get; internal set; }

        /// <summary>
        /// Creates a new <see cref="SphynxUserInfo"/>.
        /// </summary>
        /// <param name="userName">The username of the Sphynx user.</param>
        /// <param name="status">The activity status of the Sphynx user.</param>
        /// <param name="friends">User IDs of friends for this user.</param>
        public SphynxUserInfo(string userName, SphynxUserStatus status, IEnumerable<Guid>? friends = null)
            : this(default(Guid), userName, status, friends)
        {
        }

        /// <summary>
        /// Creates a new <see cref="SphynxUserInfo"/>.
        /// </summary>
        /// <param name="userId">The user ID of the Sphynx user.</param>
        /// <param name="userName">The username of the Sphynx user.</param>
        /// <param name="status">The activity status of the Sphynx user.</param>
        /// <param name="friends">User IDs of friends for this user.</param>
        public SphynxUserInfo(Guid userId, string userName, SphynxUserStatus status, IEnumerable<Guid>? friends = null)
        {
            Id = userId;
            UserName = userName;
            UserStatus = status;
            Friends = new HashSet<Guid>();

            if (friends is not null)
            {
                foreach (var friend in friends)
                    Debug.Assert(Friends.Add(friend));
            }
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => Equals(obj as ISphynxUserInfo);

        /// <inheritdoc/>
        public bool Equals(SphynxUserInfo? other) => Equals(other as ISphynxUserInfo);

        /// <inheritdoc/>
        public bool Equals(ISphynxUserInfo? other) => UserId == other?.UserId;

        /// <inheritdoc/>
        public override int GetHashCode() => UserId.GetHashCode();
    }

    /// <summary>
    /// Represents a complete <see cref="ISphynxUserInfo"/> document within the MongoDB database.
    /// </summary>
    [BsonIgnoreExtraElements]
    internal sealed class SphynxDbUserInfo : SphynxUserInfo
    {
        internal const string PASSWORD_FIELD = "pwd";
        internal const string PASSWORD_SALT_FIELD = "pwd_salt";

        /// <summary>
        /// The password for this Sphynx user.
        /// </summary>
        [BsonElement(PASSWORD_FIELD)]
        internal string? Password { get; set; }

        /// <summary>
        /// The salt for the password of this Sphynx user.
        /// </summary>
        [BsonElement(PASSWORD_SALT_FIELD)]
        internal string? PasswordSalt { get; set; }

        /// <summary>
        /// Creates a new <see cref="SphynxDbUserInfo"/>.
        /// </summary>
        /// <param name="userName">The username of the Sphynx user.</param>
        /// <param name="pwd">The password for this Sphynx user.</param>
        /// <param name="pwdSalt">The salt for the password of this Sphynx user.</param>
        /// <param name="status">The activity status of the Sphynx user.</param>
        /// <param name="friends">User IDs of friends for this user.</param>
        internal SphynxDbUserInfo(string userName, byte[] pwd, byte[] pwdSalt, SphynxUserStatus status, IEnumerable<Guid>? friends = null)
            : this(userName, Convert.ToBase64String(pwd), Convert.ToBase64String(pwdSalt), status, friends)
        {
        }

        /// <summary>
        /// Creates a new <see cref="SphynxDbUserInfo"/>.
        /// </summary>
        /// <param name="userName">The username of the Sphynx user.</param>
        /// <param name="pwd">The password for this Sphynx user.</param>
        /// <param name="pwdSalt">The salt for the password of this Sphynx user.</param>
        /// <param name="status">The activity status of the Sphynx user.</param>
        /// <param name="friends">User IDs of friends for this user.</param>
        internal SphynxDbUserInfo(string userName, string pwd, string pwdSalt, SphynxUserStatus status, IEnumerable<Guid>? friends = null)
            : base(userName, status, friends)
        {
            Password = pwd;
            PasswordSalt = pwdSalt;
        }
        
        /// <summary>
        /// Creates a new <see cref="SphynxDbUserInfo"/> from a preexisting <see cref="SphynxUserInfo"/>.
        /// </summary>
        /// <param name="shallow">The user to copy information from.</param>
        internal SphynxDbUserInfo(SphynxUserInfo shallow, byte[]? pwd = null, byte[]? pwdSalt = null)
            : this(shallow.UserId, shallow.UserName, pwd, pwdSalt, shallow.UserStatus, shallow.Friends)
        {
        }

        /// <summary>
        /// Creates a new <see cref="SphynxDbUserInfo"/>.
        /// </summary>
        /// <param name="userId">The user ID of the Sphynx user.</param>
        /// <param name="userName">The username of the Sphynx user.</param>
        /// <param name="pwd">The password for this Sphynx user.</param>
        /// <param name="pwdSalt">The salt for the password of this Sphynx user.</param>
        /// <param name="status">The activity status of the Sphynx user.</param>
        /// <param name="friends">User IDs of friends for this user.</param>
        internal SphynxDbUserInfo(Guid userId, string userName, byte[] pwd, byte[] pwdSalt, SphynxUserStatus status,
            IEnumerable<Guid>? friends = null)
            : this(userId, userName, Convert.ToBase64String(pwd), Convert.ToBase64String(pwdSalt), status, friends)
        {
        }
        
        /// <summary>
        /// Creates a new <see cref="SphynxDbUserInfo"/>.
        /// </summary>
        /// <param name="userId">The user ID of the Sphynx user.</param>
        /// <param name="userName">The username of the Sphynx user.</param>
        /// <param name="pwd">The password for this Sphynx user.</param>
        /// <param name="pwdSalt">The salt for the password of this Sphynx user.</param>
        /// <param name="status">The activity status of the Sphynx user.</param>
        /// <param name="friends">User IDs of friends for this user.</param>
        internal SphynxDbUserInfo(Guid userId, string userName, string pwd, string pwdSalt, SphynxUserStatus status,
            IEnumerable<Guid>? friends = null)
            : base(userId, userName, status, friends)
        {
            Password = pwd;
            PasswordSalt = pwdSalt;
        }
    }
}