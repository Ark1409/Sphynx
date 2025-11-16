// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Sphynx.Model.User
{
    /// <summary>
    /// A type which holds information about a specific <c>Sphynx</c> user.
    /// </summary>
    public class SphynxUserInfo : IEquatable<SphynxUserInfo>
    {
        /// <summary>
        /// The user ID for this <c>Sphynx</c> user.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// The username for this <c>Sphynx</c> user.
        /// </summary>
        public string UserName { get; set; } = null!;

        /// <summary>
        /// The activity status of this <c>Sphynx</c> user.
        /// </summary>
        public SphynxUserStatus UserStatus { get; set; }

        /// <summary>
        /// The time at which this account was created.
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; }

        /// <summary>
        /// Last login time of the user.
        /// </summary>
        public DateTimeOffset LastLogin { get; set; }

        public SphynxUserInfo()
        {
        }

        public SphynxUserInfo(Guid userId, string userName, SphynxUserStatus userStatus)
            : this(userId, userName, userStatus, default, default)
        {
        }

        public SphynxUserInfo(Guid userId, string userName, SphynxUserStatus userStatus, DateTimeOffset createdAt, DateTimeOffset lastLogin)
        {
            UserId = userId;
            UserName = userName;
            UserStatus = userStatus;
            CreatedAt = createdAt;
            LastLogin = lastLogin;
        }

        /// <inheritdoc/>
        public virtual bool Equals(SphynxUserInfo? other) => UserId == other?.UserId;
    }
}
