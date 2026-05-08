using Sphynx.Core;
using Sphynx.Model.User;

namespace Sphynx.Network.Packet.Response
{
    public class RegisterResponse : SphynxResponse, IEquatable<RegisterResponse>
    {
        /// <inheritdoc/>
        public override SphynxRequestType ResponseType => SphynxRequestType.REGISTER_REQ;

        public SphynxSelfInfo? UserInfo { get; set; }

        public Guid? SessionId { get; set; }

        public RegisterResponse()
        {
        }

        public RegisterResponse(SphynxErrorInfo errorInfo) : base(errorInfo)
        {
        }

        public RegisterResponse(SphynxSelfInfo userInfo, Guid sessionId) : this(SphynxErrorCode.SUCCESS)
        {
            UserInfo = userInfo;
            SessionId = sessionId;
        }

        /// <inheritdoc/>
        public bool Equals(RegisterResponse? other)
        {
            if (other is null || !base.Equals(other))
                return false;

            if (SessionId != other.SessionId)
                return false;

            if (UserInfo is null && other.UserInfo is null)
                return true;

            if (UserInfo is null || other.UserInfo is null)
                return false;

            return UserInfo.Equals(other.UserInfo);
        }
    }
}
