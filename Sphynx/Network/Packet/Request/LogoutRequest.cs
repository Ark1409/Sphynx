using Sphynx.Core;
using Sphynx.Network.Packet.Response;

namespace Sphynx.Network.Packet.Request
{
    public class LogoutRequest : SphynxRequest<LogoutResponse>, IEquatable<LogoutRequest>
    {
        /// <inheritdoc/>
        public override SphynxRequestType RequestType => SphynxRequestType.LOGOUT_REQ;

        /// <summary>
        /// Whether to logout of all sessions for this user.
        /// </summary>
        public bool AllSessions { get; set; }

        public LogoutRequest()
        {
        }

        public LogoutRequest(bool allSessions)
        {
            AllSessions = allSessions;
        }

        /// <inheritdoc/>
        public bool Equals(LogoutRequest? other) => base.Equals(other);

        public override LogoutResponse CreateResponse(SphynxErrorInfo errorInfo) => new LogoutResponse(errorInfo);
    }
}
