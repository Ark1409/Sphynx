using System.Diagnostics;

namespace Sphynx.Core
{
    /// <summary>
    /// A type which holds information about a specific Sphynx user.
    /// </summary>
    public class SphynxUserInfo : IEquatable<SphynxUserInfo>
    {
        /// <summary>
        /// The user ID for this Sphynx user.
        /// </summary>
        public virtual Guid UserId { get; set; }

        /// <summary>
        /// The username for this Sphynx user.
        /// </summary>
        public virtual string UserName { get; set; }

        /// <summary>
        /// The activity status of this Sphynx user.
        /// </summary>
        public virtual SphynxUserStatus UserStatus { get; set; }

        /// <summary>
        /// User IDs of friends for this user.
        /// </summary>
        public virtual HashSet<Guid> Friends { get; set; }

        /// <summary>
        /// The password for this Sphynx user.
        /// </summary>
        internal virtual string? Password { get; set; }

        /// <summary>
        /// The salt for the password of this Sphynx user.
        /// </summary>
        internal virtual string? PasswordSalt { get; set; }

        /// <summary>
        /// Creates a new <see cref="SphynxUserInfo"/>.
        /// </summary>
        /// <param name="userName">The username of the Sphynx user.</param>
        /// <param name="status">The activity status of the Sphynx user.</param>
        /// <param name="friends">User IDs of friends for this user.</param>
        public SphynxUserInfo(string userName, SphynxUserStatus status, IEnumerable<Guid>? friends = null)
            : this(default, userName, status, friends)
        {
        }

        /// <summary>
        /// Creates a new <see cref="SphynxUserInfo"/>.
        /// </summary>
        /// <param name="userName">The username of the Sphynx user.</param>
        /// <param name="status">The activity status of the Sphynx user.</param>
        /// <param name="friends">User IDs of friends for this user.</param>
        public SphynxUserInfo(string userName, SphynxUserStatus status, params Guid[] friends)
            : this(userName, status, (IEnumerable<Guid>)friends)
        {
        }

        /// <inheritdoc/>
        public SphynxUserInfo(Guid userId, string userName, SphynxUserStatus status, params Guid[] friends)
            : this(userId, userName, status, (IEnumerable<Guid>)friends)
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
            UserId = userId;
            UserName = userName;
            UserStatus = status;
            Friends = new HashSet<Guid>();

            if (friends is not null)
            {
                foreach (var friend in friends)
                {
                    Debug.Assert(Friends.Add(friend));
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="SphynxUserInfo"/>.
        /// </summary>
        /// <param name="userId">The user ID of the Sphynx user.</param>
        /// <param name="userName">The username of the Sphynx user.</param>
        /// <param name="pwd">The password for this Sphynx user.</param>
        /// <param name="pwdSalt">The salt for the password of this Sphynx user.</param>
        /// <param name="status">The activity status of the Sphynx user.</param>
        /// <param name="friends">User IDs of friends for this user.</param>
        public SphynxUserInfo(Guid userId, string userName, byte[] pwd, byte[] pwdSalt, SphynxUserStatus status, IEnumerable<Guid>? friends = null)
            : this(userId, userName, Convert.ToBase64String(pwd), Convert.ToBase64String(pwdSalt), status, friends)
        {
        }

        /// <summary>
        /// Creates a new <see cref="SphynxUserInfo"/>.
        /// </summary>
        /// <param name="userId">The user ID of the Sphynx user.</param>
        /// <param name="userName">The username of the Sphynx user.</param>
        /// <param name="pwd">The password for this Sphynx user.</param>
        /// <param name="pwdSalt">The salt for the password of this Sphynx user.</param>
        /// <param name="status">The activity status of the Sphynx user.</param>
        /// <param name="friends">User IDs of friends for this user.</param>
        public SphynxUserInfo(Guid userId, string userName, string pwd, string pwdSalt, SphynxUserStatus status, IEnumerable<Guid>? friends = null)
            : this(userId, userName, status, friends)
        {
            Password = pwd;
            PasswordSalt = pwdSalt;
        }

        /// <inheritdoc />
        public bool Equals(SphynxUserInfo? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return UserId.Equals(other.UserId);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == this.GetType() && Equals((SphynxUserInfo)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode() => UserId.GetHashCode();
    }
}