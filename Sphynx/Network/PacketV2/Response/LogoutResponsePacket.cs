using Sphynx.Core;

namespace Sphynx.Network.PacketV2.Response
{
    /// <inheritdoc cref="SphynxPacketType.LOGOUT_RES"/>
    public sealed class LogoutResponsePacket : SphynxResponsePacket, IEquatable<LogoutResponsePacket>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.LOGOUT_RES;

        /// <summary>
        /// Creates a new <see cref="LogoutResponsePacket"/>.
        /// </summary>
        /// <param name="errorCode">Error code for logout attempt.</param>
        public LogoutResponsePacket(SphynxErrorCode errorCode = SphynxErrorCode.SUCCESS) : base(errorCode)
        {
        }

        /// <inheritdoc/>
        public bool Equals(LogoutResponsePacket? other) => base.Equals(other);
    }
}
