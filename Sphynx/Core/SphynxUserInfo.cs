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
        public virtual HashSet<Guid>? Friends { get; set; }

        /// <summary>
        /// Room IDs of chat rooms which this user is in (including DMs).
        /// </summary>
        public virtual HashSet<Guid>? Rooms { get; set; }

        /// <summary>
        /// The password for this Sphynx user.
        /// </summary>
        internal virtual string? Password { get; set; }

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
        /// <param name="rooms">Room IDs of chat rooms which this user is in (including DMs).</param>
        public SphynxUserInfo(Guid userId, string userName, SphynxUserStatus status, IEnumerable<Guid>? friends = null, params Guid[] rooms)
            : this(userId, userName, status, friends, (IEnumerable<Guid>)rooms)
        {
        }

        /// <summary>
        /// Creates a new <see cref="SphynxUserInfo"/>.
        /// </summary>
        /// <param name="userId">The user ID of the Sphynx user.</param>
        /// <param name="userName">The username of the Sphynx user.</param>
        /// <param name="status">The activity status of the Sphynx user.</param>
        /// <param name="friends">User IDs of friends for this user.</param>
        /// <param name="rooms">Room IDs of chat rooms which this user is in (including DMs).</param>
        public SphynxUserInfo(Guid userId, string userName, SphynxUserStatus status, IEnumerable<Guid>? friends = null,
            IEnumerable<Guid>? rooms = null)
        {
            UserId = userId;
            UserName = userName;
            UserStatus = status;

            if (friends is not null)
            {
                Friends = new HashSet<Guid>();
                
                foreach (var friend in friends)
                {
                    Debug.Assert(Friends.Add(friend));
                }
            }

            if (rooms is not null)
            {
                Rooms = new HashSet<Guid>();
                
                foreach (var room in rooms)
                {
                    Debug.Assert(Rooms.Add(room));
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="SphynxUserInfo"/>.
        /// </summary>
        /// <param name="userId">The user ID of the Sphynx user.</param>
        /// <param name="userName">The username of the Sphynx user.</param>
        /// <param name="pwd">The password for this Sphynx user.</param>
        /// <param name="status">The activity status of the Sphynx user.</param>
        /// <param name="friends">User IDs of friends for this user.</param>
        /// <param name="rooms">Room IDs of chat rooms which this user is in (including DMs).</param>
        public SphynxUserInfo(Guid userId, string userName, string pwd, SphynxUserStatus status, HashSet<Guid> friends,
            HashSet<Guid> rooms)
        {
            UserId = userId;
            UserName = userName;
            UserStatus = status;
            Password = pwd;
            Friends = friends;
            Rooms = rooms;
        }

        /// <summary>
        /// Creates a new <see cref="SphynxUserInfo"/>.
        /// </summary>
        /// <param name="userId">The user ID of the Sphynx user.</param>
        /// <param name="userName">The username of the Sphynx user.</param>
        /// <param name="status">The activity status of the Sphynx user.</param>
        /// <param name="friends">User IDs of friends for this user.</param>
        /// <param name="rooms">Room IDs of chat rooms which this user is in (including DMs).</param>
        public SphynxUserInfo(Guid userId, string userName, SphynxUserStatus status, HashSet<Guid> friends,
            HashSet<Guid> rooms)
            : this(userId, userName, null!, status, friends, rooms)
        {
        }

        /// <summary>
        /// Creates a new <see cref="SphynxUserInfo"/>.
        /// </summary>
        /// <param name="userName">The username of the Sphynx user.</param>
        /// <param name="status">The activity status of the Sphynx user.</param>
        /// <param name="friends">User IDs of friends for this user.</param>
        /// <param name="rooms">Room IDs of chat rooms which this user is in (including DMs).</param>
        public SphynxUserInfo(string userName, SphynxUserStatus status, HashSet<Guid> friends,
            HashSet<Guid> rooms)
            : this(userName, null!, status, friends, rooms)
        {
        }

        /// <summary>
        /// Creates a new <see cref="SphynxUserInfo"/>.
        /// </summary>
        /// <param name="userName">The username of the Sphynx user.</param>
        /// <param name="pwd">The password for this Sphynx user.</param>
        /// <param name="status">The activity status of the Sphynx user.</param>
        /// <param name="friends">User IDs of friends for this user.</param>
        /// <param name="rooms">Room IDs of chat rooms which this user is in (including DMs).</param>
        public SphynxUserInfo(string userName, string pwd, SphynxUserStatus status, HashSet<Guid> friends, HashSet<Guid> rooms)
            : this(default, userName, pwd, status, friends, rooms)
        {
        }

        /// <summary>
        /// Creates a new <see cref="SphynxUserInfo"/>.
        /// </summary>
        /// <param name="userId">The user ID of the Sphynx user.</param>
        /// <param name="userName">The username of the Sphynx user.</param>
        /// <param name="pwd">The password for this Sphynx user.</param>
        /// <param name="status">The activity status of the Sphynx user.</param>
        /// <param name="friends">User IDs of friends for this user.</param>
        /// <param name="rooms">Room IDs of chat rooms which this user is in (including DMs).</param>
        public SphynxUserInfo(Guid userId, string userName, string pwd, SphynxUserStatus status, IEnumerable<Guid>? friends = null,
            IEnumerable<Guid>? rooms = null)
            : this(userId, userName, status, friends, rooms)
        {
            Password = pwd;
        }

        /// <summary>
        /// Creates a new <see cref="SphynxUserInfo"/>.
        /// </summary>
        /// <param name="userName">The username of the Sphynx user.</param>
        /// <param name="pwd">The password for this Sphynx user.</param>
        /// <param name="status">The activity status of the Sphynx user.</param>
        /// <param name="friends">User IDs of friends for this user.</param>
        /// <param name="rooms">Room IDs of chat rooms which this user is in (including DMs).</param>
        public SphynxUserInfo(string userName, string pwd, SphynxUserStatus status, IEnumerable<Guid>? friends = null,
            IEnumerable<Guid>? rooms = null)
            : this(default, userName, pwd, status, friends, rooms)
        {
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
            return obj.GetType() == GetType() && Equals((SphynxUserInfo)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode() => UserId.GetHashCode();
    }
}