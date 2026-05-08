using Sphynx.Core;
using Sphynx.Network.Packet.Response;

namespace Sphynx.Network.Packet.Request
{
    public class RegisterRequest : SphynxRequest<RegisterResponse>, IEquatable<RegisterRequest>
    {
        public string UserName { get; set; } = null!;
        public string Password { get; set; } = null!;

        /// <inheritdoc/>
        public override SphynxRequestType RequestType => SphynxRequestType.REGISTER_REQ;

        public RegisterRequest()
        {
        }

        public RegisterRequest(string userName, string password)
        {
            UserName = userName;
            Password = password;
        }

        /// <inheritdoc/>
        public bool Equals(RegisterRequest? other) => MessageType == other?.MessageType &&
                                                      UserName == other?.UserName && Password == other?.Password;

        public override RegisterResponse CreateResponse(SphynxErrorInfo errorInfo) => new RegisterResponse(errorInfo);
    }
}
