using Sphynx.Core;
using Sphynx.Network.Packet.Response;

namespace Sphynx.Network.Packet.Request
{
    /// <inheritdoc cref="SphynxPacketType.LOGIN_REQ"/>
    /// <remarks>The <see cref="SphynxRequest.SessionId"/> property is not serialized for this packet.</remarks>
    public sealed class LoginRequest : SphynxRequest<LoginResponse>, IEquatable<LoginRequest>
    {
        /// <summary>
        /// User name entered by user for login.
        /// </summary>
        public string UserName { get; set; } = null!;

        /// <summary>
        /// Password entered by user for login.
        /// </summary>
        public string Password { get; set; } = null!;

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.LOGIN_REQ;

        public LoginRequest()
        {
        }

        /// <summary>
        /// Creates a <see cref="LoginRequest"/>.
        /// </summary>
        /// <param name="userName">User name entered by user for login.</param>
        /// <param name="password">Password entered by user for login.</param>
        /// <remarks>The <see cref="SphynxRequest.SessionId"/> property is not serialized for this packet.</remarks>
        public LoginRequest(string userName, string password)
        {
            UserName = userName;
            Password = password;
        }

        /// <inheritdoc/>
        public bool Equals(LoginRequest? other) =>
            PacketType == other?.PacketType && UserName == other?.UserName && Password == other?.Password;

        public override LoginResponse CreateResponse(SphynxErrorInfo errorInfo) => new LoginResponse(errorInfo)
        {
            RequestTag = RequestTag
        };
    }
}
