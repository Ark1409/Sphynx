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
        /// Whether to logout of all sessions for this user.
        /// </summary>
        public bool AllSessions { get; set; }

        public LogoutRequest()
        {
        }

        /// <summary>
        /// Creates a new <see cref="LogoutRequest"/>.
        /// </summary>
        public LogoutRequest(Guid sessionId, bool allSessions = false) : base(sessionId)
        {
            AllSessions = allSessions;
        }

        /// <inheritdoc/>
        public bool Equals(LogoutRequest? other) => base.Equals(other);

        public override LogoutResponse CreateResponse(SphynxErrorInfo errorInfo) => new LogoutResponse(errorInfo);
    }
}
