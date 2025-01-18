using Sphynx.Core;
using Sphynx.Model.User;

namespace Sphynx.Network.PacketV2.Broadcast
{
    /// <inheritdoc cref="SphynxPacketType.LOGIN_BCAST"/>
    public sealed class LoginBroadcastPacket : SphynxPacket, IEquatable<LoginBroadcastPacket>
    {
        /// <summary>
        /// User ID of the user who went online.
        /// </summary>
        public SnowflakeId UserId { get; set; }

        /// <summary>
        /// The status of the user who went online.
        /// </summary>
        public SphynxUserStatus UserStatus { get; set; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.LOGIN_BCAST;

        /// <summary>
        /// Creates a new <see cref="LoginBroadcastPacket"/>.
        /// </summary>
        /// <param name="userId">User ID of the user who went online.</param>
        /// <param name="userStatus">The status of the user who went online.</param>
        public LoginBroadcastPacket(SnowflakeId userId, SphynxUserStatus userStatus)
        {
            UserId = userId;
            UserStatus = userStatus;
        }

        /// <inheritdoc/>
        public bool Equals(LoginBroadcastPacket? other) =>
            base.Equals(other) && UserId == other?.UserId && UserStatus == other?.UserStatus;
    }
}
