using System.Collections.Immutable;
using MongoDB.Bson.Serialization.Attributes;
using Sphynx.Core;
using Sphynx.ModelV2.User;

namespace Sphynx.ServerV2.Persistence.User
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
        public ISet<SnowflakeId> Friends { get; set; } = ImmutableHashSet<SnowflakeId>.Empty;

        [BsonElement(ROOMS_FIELD)]
        public ISet<SnowflakeId> Rooms { get; set; } = ImmutableHashSet<SnowflakeId>.Empty;

        [BsonElement(LAST_READ_MSGS_FIELD)]
        public ILastReadMessageInfo LastReadMessages { get; set; }

        [BsonElement(OUT_FRIEND_REQS_FIELD)]
        public ISet<SnowflakeId> OutgoingFriendRequests { get; set; } = ImmutableHashSet<SnowflakeId>.Empty;

        [BsonElement(INC_FRIEND_REQS_FIELD)]
        public ISet<SnowflakeId> IncomingFriendRequests { get; set; } = ImmutableHashSet<SnowflakeId>.Empty;

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

        public SphynxSelfInfo(SnowflakeId userId, string userName, SphynxUserStatus userStatus) : base(userId, userName, userStatus)
        {
        }

        public SphynxSelfInfo(ISphynxUserInfo userInfo) : base(userInfo.UserId, userInfo.UserName, userInfo.UserStatus)
        {
        }

        public SphynxSelfInfo(ISphynxSelfInfo selfInfo) : base(selfInfo.UserId, selfInfo.UserName, selfInfo.UserStatus)
        {
            Friends = selfInfo.Friends;
            Rooms = selfInfo.Rooms;
            LastReadMessages = selfInfo.LastReadMessages;
            OutgoingFriendRequests = selfInfo.OutgoingFriendRequests;
            IncomingFriendRequests = selfInfo.IncomingFriendRequests;

            if (selfInfo is SphynxSelfInfo dbSelfInfo)
            {
                Password = dbSelfInfo.Password;
                PasswordSalt = dbSelfInfo.PasswordSalt;
            }
        }

        public SphynxSelfInfo(SnowflakeId userId,
            string userName,
            SphynxUserStatus userStatus,
            string? password,
            string? passwordSalt,
            ISet<SnowflakeId> friends,
            ISet<SnowflakeId> rooms,
            ILastReadMessageInfo lastReadMessages,
            ISet<SnowflakeId> outgoingFriendRequests,
            ISet<SnowflakeId> incomingFriendRequests)
            : base(userId, userName, userStatus)
        {
            Friends = friends;
            Rooms = rooms;
            LastReadMessages = lastReadMessages;
            OutgoingFriendRequests = outgoingFriendRequests;
            IncomingFriendRequests = incomingFriendRequests;
            Password = password;
            PasswordSalt = passwordSalt;
        }

        private void Initialize(ISphynxUserInfo userInfo)
        {
        }

        /// <inheritdoc/>
        public override int GetHashCode() => UserId.GetHashCode();
    }
}
