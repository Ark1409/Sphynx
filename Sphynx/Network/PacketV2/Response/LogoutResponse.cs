using Sphynx.Core;

namespace Sphynx.Network.PacketV2.Response
{
    /// <inheritdoc cref="SphynxPacketType.LOGOUT_RES"/>
    public sealed class LogoutResponse : SphynxResponse, IEquatable<LogoutResponse>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.LOGOUT_RES;

        /// <summary>
        /// Creates a new <see cref="LogoutResponse"/>.
        /// </summary>
        /// <param name="errorCode">Error code for logout attempt.</param>
        public LogoutResponse(SphynxErrorCode errorCode = SphynxErrorCode.SUCCESS) : base(errorCode)
        {
        }

        /// <inheritdoc/>
        public bool Equals(LogoutResponse? other) => base.Equals(other);
    }
}
