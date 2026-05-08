using Sphynx.Model.User;

namespace Sphynx.Network.Packet.Broadcast
{
    public class LoginBroadcast : SphynxBroadcast, IEquatable<LoginBroadcast>
    {
        /// <summary>
        /// User ID of the user who went online.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// The status of the user who went online.
        /// </summary>
        public SphynxUserStatus UserStatus { get; set; }

        /// <inheritdoc/>
        public override SphynxBroadcastType BroadcastType => SphynxBroadcastType.LOGIN_BCAST;

        public LoginBroadcast()
        {
        }

        /// <summary>
        /// Creates a new <see cref="LoginBroadcast"/>.
        /// </summary>
        /// <param name="userId">User ID of the user who went online.</param>
        /// <param name="userStatus">The status of the user who went online.</param>
        public LoginBroadcast(Guid userId, SphynxUserStatus userStatus)
        {
            UserId = userId;
            UserStatus = userStatus;
        }

        /// <inheritdoc/>
        public bool Equals(LoginBroadcast? other) => base.Equals(other) && UserId == other?.UserId && UserStatus == other?.UserStatus;
    }
}
