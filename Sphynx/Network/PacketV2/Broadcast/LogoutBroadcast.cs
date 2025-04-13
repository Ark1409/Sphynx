using Sphynx.Core;

namespace Sphynx.Network.PacketV2.Broadcast
{
    /// <inheritdoc cref="SphynxPacketType.LOGOUT_BCAST"/>
    public sealed class LogoutBroadcast : SphynxPacket, IEquatable<LogoutBroadcast>
    {
        /// <summary>
        /// User ID of the user who went offline.
        /// </summary>
        public SnowflakeId UserId { get; init; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.LOGOUT_BCAST;

        /// <summary>
        /// Creates a new <see cref="LogoutBroadcast"/>.
        /// </summary>
        /// <param name="userId">User ID of the user who went offline.</param>
        public LogoutBroadcast(SnowflakeId userId)
        {
            UserId = userId;
        }

        /// <inheritdoc/>
        public bool Equals(LogoutBroadcast? other) => base.Equals(other) && UserId == other?.UserId;
    }
}
