using Sphynx.Core;

namespace Sphynx.Network.Packet.Response
{
    /// <inheritdoc cref="SphynxPacketType.LOGOUT_RES"/>
    public sealed class LogoutResponse : SphynxResponse, IEquatable<LogoutResponse>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.LOGOUT_RES;

        public LogoutResponse()
        {
        }

        /// <summary>
        /// Creates a new <see cref="LogoutResponse"/>.
        /// </summary>
        /// <param name="errorInfo">Error code for logout attempt.</param>
        public LogoutResponse(SphynxErrorCode errorInfo = SphynxErrorCode.SUCCESS) : base(errorInfo)
        {
        }

        /// <summary>
        /// Creates a new <see cref="LogoutResponse"/>.
        /// </summary>
        /// <param name="errorInfo">Error code for logout attempt.</param>
        public LogoutResponse(SphynxErrorInfo errorInfo) : base(errorInfo)
        {
        }

        /// <inheritdoc/>
        public bool Equals(LogoutResponse? other) => base.Equals(other);
    }
}
