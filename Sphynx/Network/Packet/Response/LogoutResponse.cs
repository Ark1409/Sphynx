using Sphynx.Core;

namespace Sphynx.Network.Packet.Response
{
    public class LogoutResponse : SphynxResponse, IEquatable<LogoutResponse>
    {
        /// <inheritdoc/>
        public override SphynxRequestType ResponseType => SphynxRequestType.LOGOUT_REQ;

        public LogoutResponse()
        {
        }

        public LogoutResponse(SphynxErrorCode errorInfo = SphynxErrorCode.SUCCESS) : base(errorInfo)
        {
        }

        public LogoutResponse(SphynxErrorInfo errorInfo) : base(errorInfo)
        {
        }

        /// <inheritdoc/>
        public bool Equals(LogoutResponse? other) => base.Equals(other);
    }
}
