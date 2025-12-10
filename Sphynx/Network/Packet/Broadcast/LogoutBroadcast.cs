namespace Sphynx.Network.Packet.Broadcast
{
    /// <inheritdoc cref="SphynxPacketType.LOGOUT_BCAST"/>
    public sealed class LogoutBroadcast : SphynxPacket, IEquatable<LogoutBroadcast>
    {
        /// <summary>
        /// User ID of the user who went offline.
        /// </summary>
        public Guid UserId { get; set; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.LOGOUT_BCAST;

        public LogoutBroadcast()
        {
        }

        /// <summary>
        /// Creates a new <see cref="LogoutBroadcast"/>.
        /// </summary>
        /// <param name="userId">User ID of the user who went offline.</param>
        public LogoutBroadcast(Guid userId)
        {
            UserId = userId;
        }

        /// <inheritdoc/>
        public bool Equals(LogoutBroadcast? other) => base.Equals(other) && UserId == other?.UserId;
    }
}
