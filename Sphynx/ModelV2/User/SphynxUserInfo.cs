// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Sphynx.ModelV2.User
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

        public SphynxUserInfo()
        {
        }

        public SphynxUserInfo(Guid userId, string userName, SphynxUserStatus userStatus)
        {
            UserId = userId;
            UserName = userName;
            UserStatus = userStatus;
        }

        /// <inheritdoc/>
        public virtual bool Equals(SphynxUserInfo? other) => UserId == other?.UserId;
    }
}
