using Sphynx.Core;
using Sphynx.Model.User;

namespace Sphynx.Network.PacketV2.Broadcast
{
    /// <inheritdoc cref="SphynxPacketType.LOGIN_BCAST"/>
    public sealed class LoginBroadcast : SphynxPacket, IEquatable<LoginBroadcast>
    {
        /// <summary>
        /// User ID of the user who went online.
        /// </summary>
        public SnowflakeId UserId { get; init; }

        /// <summary>
        /// The status of the user who went online.
        /// </summary>
        public SphynxUserStatus UserStatus { get; init; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.LOGIN_BCAST;

        /// <summary>
        /// Creates a new <see cref="LoginBroadcast"/>.
        /// </summary>
        /// <param name="userId">User ID of the user who went online.</param>
        /// <param name="userStatus">The status of the user who went online.</param>
        public LoginBroadcast(SnowflakeId userId, SphynxUserStatus userStatus)
        {
            UserId = userId;
            UserStatus = userStatus;
        }

        /// <inheritdoc/>
        public bool Equals(LoginBroadcast? other) =>
            base.Equals(other) && UserId == other?.UserId && UserStatus == other?.UserStatus;
    }
}
