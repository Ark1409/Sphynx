using Sphynx.Core;
using Sphynx.Network.PacketV2.Response;

namespace Sphynx.Network.PacketV2.Request
{
    /// <inheritdoc cref="SphynxPacketType.LOGOUT_REQ"/>
    public sealed class LogoutRequest : SphynxRequest, IEquatable<LogoutRequest>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.LOGOUT_REQ;

        /// <summary>
        /// Creates a new <see cref="LogoutRequest"/>.
        /// </summary>
        /// <param name="accessToken">The JWT access token for this request.</param>
        public LogoutRequest(string accessToken) : base(accessToken)
        {
        }

        /// <inheritdoc/>
        public bool Equals(LogoutRequest? other) => base.Equals(other);

        public override LogoutResponse CreateResponse(SphynxErrorInfo errorInfo) => new LogoutResponse(errorInfo);
    }
}
