using Sphynx.Core;
using Sphynx.Network.PacketV2.Response;

namespace Sphynx.Network.PacketV2.Request
{
    /// <inheritdoc cref="SphynxPacketType.LOGOUT_REQ"/>
    public sealed class LogoutRequest : SphynxRequest, IEquatable<LogoutRequest>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.LOGOUT_REQ;

        public Guid RefreshToken { get; }

        public LogoutRequest()
        {
        }

        /// <summary>
        /// Creates a new <see cref="LogoutRequest"/>.
        /// </summary>
        public LogoutRequest(Guid sessionId, Guid refreshToken) : base(sessionId)
        {
            RefreshToken = refreshToken;
        }

        /// <inheritdoc/>
        public bool Equals(LogoutRequest? other) => base.Equals(other);

        public override LogoutResponse CreateResponse(SphynxErrorInfo errorInfo) => new LogoutResponse(errorInfo);
    }
}
