// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Model.User;
using Sphynx.Server.Persistence.User;

namespace Sphynx.Server.Chat.Model
{
    public class SphynxChatUser : IEquatable<SphynxChatUser>
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = null!;
        public SphynxUserStatus UserStatus { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset LastLogin { get; set; }

        public SphynxChatUser()
        {
        }

        public SphynxChatUser(Guid userId, string userName, SphynxUserStatus userStatus) : this(userId, userName, userStatus, default, default)
        {
        }

        public SphynxChatUser(Guid userId, string userName, SphynxUserStatus userStatus, DateTimeOffset createdAt, DateTimeOffset lastLogin)
        {
            UserId = userId;
            UserName = userName;
            UserStatus = userStatus;
            CreatedAt = createdAt;
            LastLogin = lastLogin;
        }

        /// <inheritdoc/>
        public virtual bool Equals(SphynxChatUser? other) => UserId == other?.UserId;

        public override int GetHashCode() => UserId.GetHashCode();
    }

    public static class SphynxChatUserExtensions
    {
        public static SphynxChatUser ToSimpleDomain(this SphynxSelfInfo selfInfo)
        {
            return new SphynxChatUser(selfInfo.UserId, selfInfo.UserName, selfInfo.UserStatus, selfInfo.CreatedAt, selfInfo.LastLogin);
        }

        public static SphynxChatUser ToSimpleDomain(this SphynxDbUser dbUser)
        {
            return new SphynxChatUser(dbUser.UserId, dbUser.UserName, dbUser.UserStatus, dbUser.CreatedAt, dbUser.LastLogin);
        }

        public static SphynxUserInfo ToDto(this SphynxChatUser user)
        {
            return new SphynxUserInfo(user.UserId, user.UserName, user.UserStatus, user.CreatedAt, user.LastLogin);
        }
    }
}
