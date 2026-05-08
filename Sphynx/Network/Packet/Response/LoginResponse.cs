using Sphynx.Core;
using Sphynx.Model.User;

namespace Sphynx.Network.Packet.Response
{
    public class LoginResponse : SphynxResponse, IEquatable<LoginResponse>
    {
        /// <inheritdoc/>
        public override SphynxRequestType ResponseType => SphynxRequestType.LOGIN_REQ;

        public SphynxSelfInfo? UserInfo { get; set; }
        public Guid? SessionId { get; set; }

        public LoginResponse()
        {
        }

        public LoginResponse(SphynxErrorInfo errorInfo) : base(errorInfo)
        {
        }

        public LoginResponse(SphynxSelfInfo userInfo, Guid sessionId)
            : this(SphynxErrorCode.SUCCESS)
        {
            UserInfo = userInfo ?? throw new ArgumentNullException(nameof(userInfo));
            SessionId = sessionId;
        }

        /// <inheritdoc/>
        public bool Equals(LoginResponse? other)
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
