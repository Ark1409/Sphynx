using Sphynx.Core;
using Sphynx.Network.Packet.Response;

namespace Sphynx.Network.Packet.Request
{
    public class LoginRequest : SphynxRequest<LoginResponse>, IEquatable<LoginRequest>
    {
        /// <inheritdoc/>
        public override SphynxRequestType RequestType => SphynxRequestType.LOGIN_REQ;

        public string UserName { get; set; } = null!;
        public string Password { get; set; } = null!;

        public LoginRequest()
        {
        }

        public LoginRequest(string userName, string password)
        {
            UserName = userName;
            Password = password;
        }

        public bool Equals(LoginRequest? other) => base.Equals(other) && UserName == other?.UserName && Password == other?.Password;

        public override LoginResponse CreateResponse(SphynxErrorInfo errorInfo) => new LoginResponse(errorInfo);
    }
}
