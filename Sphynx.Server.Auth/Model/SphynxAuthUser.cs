// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Model.User;
using Sphynx.Server.Persistence.User;

namespace Sphynx.Server.Auth.Model
{
    public class SphynxAuthUser : IEquatable<SphynxAuthUser>
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = null!;
        public SphynxUserStatus UserStatus { get; set; }

        public string? PasswordHash { get; set; }
        public string? PasswordSalt { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset LastLogin { get; set; }

        public SphynxAuthUser()
        {
        }

        public SphynxAuthUser(Guid userId, string userName, SphynxUserStatus userStatus, DateTimeOffset createdAt)
        {
            UserId = userId;
            UserName = userName;
            UserStatus = userStatus;
            CreatedAt = createdAt;
        }

        /// <inheritdoc/>
        public bool Equals(SphynxAuthUser? other) => UserId == other?.UserId;
    }

    public static class SphynxAuthUserExtensions
    {
        public static SphynxAuthUser ToDomain(this SphynxUserInfo userInfo, string? password = null, string? passwordSalt = null)
        {
            return new SphynxAuthUser(userInfo.UserId, userInfo.UserName, userInfo.UserStatus, userInfo.CreatedAt)
            {
                PasswordHash = password,
                PasswordSalt = passwordSalt
            };
        }

        public static SphynxAuthUser ToDomain(this SphynxSelfInfo selfInfo, string? password = null, string? passwordSalt = null)
        {
            return new SphynxAuthUser(selfInfo.UserId, selfInfo.UserName, selfInfo.UserStatus, selfInfo.CreatedAt)
            {
                LastLogin = selfInfo.LastLogin,
                PasswordHash = password,
                PasswordSalt = passwordSalt,
            };
        }

        public static SphynxAuthUser ToDomain(this SphynxDbUser dbUser)
        {
            return new SphynxAuthUser(dbUser.UserId, dbUser.UserName, dbUser.UserStatus, dbUser.CreatedAt)
            {
                LastLogin = dbUser.LastLogin,
                PasswordHash = dbUser.Password,
                PasswordSalt = dbUser.PasswordSalt,
            };
        }

        public static SphynxDbUser ToRecord(this SphynxAuthUser user)
        {
            return new SphynxDbUser(user.UserId, user.UserName, user.UserStatus)
            {
                CreatedAt = user.CreatedAt,
                LastLogin = user.LastLogin,
                Password = user.PasswordHash ?? throw new NullReferenceException("Password cannot be null"),
                PasswordSalt = user.PasswordSalt ?? throw new NullReferenceException("Password salt cannot be null"),
            };
        }

        public static SphynxSelfInfo ToDto(this SphynxAuthUser user)
        {
            return new SphynxSelfInfo(user.UserId, user.UserName, user.UserStatus, user.CreatedAt, user.LastLogin)
            {
                LastLogin = user.LastLogin,
            };
        }

        public static SphynxUserInfo ToSimpleDto(this SphynxAuthUser user)
        {
            return new SphynxUserInfo(user.UserId, user.UserName, user.UserStatus, user.CreatedAt, user.LastLogin);
        }
    }
}
