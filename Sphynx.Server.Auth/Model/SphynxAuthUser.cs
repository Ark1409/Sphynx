// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Immutable;
using Sphynx.Core;
using Sphynx.ModelV2.User;

namespace Sphynx.Server.Auth.Model
{
    public class SphynxAuthUser : IEquatable<SphynxAuthUser>
    {
        public SnowflakeId UserId { get; set; }
        public string UserName { get; set; } = null!;
        public SphynxUserStatus UserStatus { get; set; }

        public string? PasswordHash { get; set; }
        public string? PasswordSalt { get; set; }

        public ISet<SnowflakeId>? Friends { get; set; }
        public ISet<SnowflakeId>? Rooms { get; set; }
        public IDictionary<SnowflakeId, SnowflakeId>? LastReadMessages { get; set; }
        public ISet<SnowflakeId>? OutgoingFriendRequests { get; set; }
        public ISet<SnowflakeId>? IncomingFriendRequests { get; set; }

        public SphynxAuthUser()
        {
        }

        public SphynxAuthUser(SnowflakeId userId, string userName, SphynxUserStatus userStatus)
        {
            UserId = userId;
            UserName = userName;
            UserStatus = userStatus;
        }

        /// <inheritdoc/>
        public bool Equals(SphynxAuthUser? other) => UserId == other?.UserId;
    }

    public static class SphynxAuthUserExtensions
    {
        public static SphynxAuthUser ToDomain(this SphynxUserInfo userInfo, string? password = null, string? passwordSalt = null)
        {
            return new SphynxAuthUser(userInfo.UserId, userInfo.UserName, userInfo.UserStatus)
            {
                PasswordHash = password,
                PasswordSalt = passwordSalt
            };
        }

        public static SphynxAuthUser ToDomain(this SphynxSelfInfo selfInfo, string? password = null, string? passwordSalt = null)
        {
            return new SphynxAuthUser(selfInfo.UserId, selfInfo.UserName, selfInfo.UserStatus)
            {
                Friends = selfInfo.Friends,
                Rooms = selfInfo.Rooms,
                PasswordHash = password,
                PasswordSalt = passwordSalt,
                LastReadMessages = selfInfo.LastReadMessages,
                IncomingFriendRequests = selfInfo.IncomingFriendRequests,
                OutgoingFriendRequests = selfInfo.OutgoingFriendRequests,
            };
        }

        public static SphynxAuthUser ToDomain(this ServerV2.Persistence.User.SphynxSelfInfo dbUser)
        {
            return new SphynxAuthUser(dbUser.UserId, dbUser.UserName, dbUser.UserStatus)
            {
                Friends = dbUser.Friends,
                Rooms = dbUser.Rooms,
                PasswordHash = dbUser.Password,
                PasswordSalt = dbUser.PasswordSalt,
                LastReadMessages = dbUser.LastReadMessages,
                IncomingFriendRequests = dbUser.IncomingFriendRequests,
                OutgoingFriendRequests = dbUser.OutgoingFriendRequests,
            };
        }

        public static ServerV2.Persistence.User.SphynxSelfInfo ToRecord(this SphynxAuthUser user)
        {
            return new ServerV2.Persistence.User.SphynxSelfInfo(user.UserId, user.UserName, user.UserStatus)
            {
                Friends = user.Friends ?? ImmutableHashSet<SnowflakeId>.Empty,
                Rooms = user.Rooms ?? ImmutableHashSet<SnowflakeId>.Empty,
                Password = user.PasswordHash,
                PasswordSalt = user.PasswordSalt,
                LastReadMessages = user.LastReadMessages is null ? new LastReadMessageInfo() : new LastReadMessageInfo(user.LastReadMessages),
                IncomingFriendRequests = user.IncomingFriendRequests ?? ImmutableHashSet<SnowflakeId>.Empty,
                OutgoingFriendRequests = user.OutgoingFriendRequests ?? ImmutableHashSet<SnowflakeId>.Empty,
            };
        }

        public static SphynxSelfInfo ToDto(this SphynxAuthUser user)
        {
            return new SphynxSelfInfo(user.UserId, user.UserName, user.UserStatus)
            {
                Friends = user.Friends!,
                Rooms = user.Rooms!,
                LastReadMessages = user.LastReadMessages is null ? null! : new LastReadMessageInfo(user.LastReadMessages),
                IncomingFriendRequests = user.IncomingFriendRequests!,
                OutgoingFriendRequests = user.OutgoingFriendRequests!,
            };
        }

        public static SphynxUserInfo ToSimpleDto(this SphynxAuthUser user)
        {
            return new SphynxUserInfo(user.UserId, user.UserName, user.UserStatus);
        }
    }
}
