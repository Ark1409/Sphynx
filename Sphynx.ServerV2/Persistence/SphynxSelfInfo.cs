using MongoDB.Bson.Serialization.Attributes;
using Sphynx.Core;
using Sphynx.ModelV2.User;

namespace Sphynx.ServerV2.Persistence
{
    /// <summary>
    /// Represents a complete representation of a <c>Sphynx</c> user within the database.
    /// </summary>
    /// <seealso cref="ISphynxSelfInfo"/>
    [BsonIgnoreExtraElements]
    public class SphynxSelfInfo : SphynxUserInfo, ISphynxSelfInfo
    {
        public const string FRIENDS_FIELD = "friends";
        public const string ROOMS_FIELD = "rooms";
        public const string LAST_READ_MSGS_FIELD = "last_read";
        public const string OUT_FRIEND_REQS_FIELD = "out_reqs";
        public const string INC_FRIEND_REQS_FIELD = "inc_recs";
        public const string PWD_FIELD = "pwd";
        public const string PWD_SALT_FIELD = "pwd_salt";

        [BsonElement(FRIENDS_FIELD)]
        public ISet<SnowflakeId> Friends { get; set; }

        [BsonElement(ROOMS_FIELD)]
        public ISet<SnowflakeId> Rooms { get; set; }

        [BsonElement(LAST_READ_MSGS_FIELD)]
        public ILastReadMessageInfo LastReadMessages { get; set; }

        [BsonElement(OUT_FRIEND_REQS_FIELD)]
        public ISet<SnowflakeId> OutgoingFriendRequests { get; set; }

        [BsonElement(INC_FRIEND_REQS_FIELD)]
        public ISet<SnowflakeId> IncomingFriendRequests { get; set; }

        /// <summary>
        /// The hashed password for this Sphynx user, as a base-64 string.
        /// </summary>
        [BsonElement(PWD_FIELD)]
        internal string? Password { get; set; }

        /// <summary>
        /// The salt for the password of this Sphynx user.
        /// </summary>
        [BsonElement(PWD_SALT_FIELD)]
        internal string? PasswordSalt { get; set; }

        public SphynxSelfInfo(SnowflakeId userId,
            string userName,
            SphynxUserStatus status,
            IEnumerable<Guid>? friends = null,
            IEnumerable<Guid>? rooms = null)
            : base(userId, userName, status)
        {
        }

        /// <summary>
        /// Creates a new <see cref="SphynxSelfInfo"/>.
        /// </summary>
        /// <param name="userName">The username of the Sphynx user.</param>
        /// <param name="encodedPwd">The password for this Sphynx user.</param>
        /// <param name="encodedSalt">The salt for the password of this Sphynx user.</param>
        /// <param name="status">The activity status of the Sphynx user.</param>
        /// <param name="friends">User IDs of friends for this user.</param>
        internal SphynxSelfInfo(string userName,
            byte[] encodedPwd,
            byte[] encodedSalt,
            SphynxUserStatus status,
            IEnumerable<Guid>? friends = null,
            IEnumerable<Guid>? rooms = null)
            : this(default, userName, encodedPwd, encodedSalt, status, friends, rooms)
        {
        }

        /// <summary>
        /// Creates a new <see cref="SphynxSelfInfo"/>.
        /// </summary>
        /// <param name="userName">The username of the Sphynx user.</param>
        /// <param name="encodedPwd">The password for this Sphynx user.</param>
        /// <param name="encodedSalt">The salt for the password of this Sphynx user.</param>
        /// <param name="status">The activity status of the Sphynx user.</param>
        /// <param name="friends">User IDs of friends for this user.</param>
        internal SphynxSelfInfo(string userName,
            string encodedPwd,
            string encodedSalt,
            SphynxUserStatus status,
            IEnumerable<Guid>? friends = null,
            IEnumerable<Guid>? rooms = null)
            : this(default, userName, encodedPwd, encodedSalt, status, friends, rooms)
        {
        }

        /// <summary>
        /// Creates a new <see cref="SphynxSelfInfo"/>.
        /// </summary>
        /// <param name="userId">The user ID of the Sphynx user.</param>
        /// <param name="userName">The username of the Sphynx user.</param>
        /// <param name="encodedPwd">The password for this Sphynx user, as a base-64 string</param>
        /// <param name="encodedSalt">The salt for the password of this Sphynx user, as a base-64 string</param>
        /// <param name="status">The activity status of the Sphynx user.</param>
        /// <param name="friends">User IDs of friends for this user.</param>
        /// <param name="rooms">Room IDs of chat rooms which this user is in (including DMs).</param>
        public SphynxSelfInfo(SnowflakeId userId,
            string userName,
            byte[] encodedPwd,
            byte[] encodedSalt,
            SphynxUserStatus status,
            IEnumerable<Guid>? friends = null,
            IEnumerable<Guid>? rooms = null)
            : this(userId, userName, Convert.ToBase64String(encodedPwd), Convert.ToBase64String(encodedSalt), status,
                friends, rooms)
        {
        }

        /// <summary>
        /// Creates a new <see cref="SphynxSelfInfo"/>.
        /// </summary>
        /// <param name="userId">The user ID of the Sphynx user.</param>
        /// <param name="userName">The username of the Sphynx user.</param>
        /// <param name="encodedPwd">The password for this Sphynx user, as a base-64 string.</param>
        /// <param name="encodedSalt">The salt for the password of this Sphynx user, as a base-64 string</param>
        /// <param name="status">The activity status of the Sphynx user.</param>
        /// <param name="friends">User IDs of friends for this user.</param>
        /// <param name="rooms">Room IDs of chat rooms which this user is in (including DMs).</param>
        public SphynxSelfInfo(SnowflakeId userId,
            string userName,
            string encodedPwd,
            string encodedSalt,
            SphynxUserStatus status,
            IEnumerable<Guid>? friends = null,
            IEnumerable<Guid>? rooms = null)
            : this(userId, userName, status, friends, rooms)
        {
            Password = encodedPwd;
            PasswordSalt = encodedSalt;
        }

        /// <inheritdoc/>
        public override int GetHashCode() => UserId.GetHashCode();
    }
}
