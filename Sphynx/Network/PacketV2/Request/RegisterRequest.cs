using Sphynx.Core;
using Sphynx.Network.PacketV2.Response;

namespace Sphynx.Network.PacketV2.Request
{
    /// <inheritdoc cref="SphynxPacketType.REGISTER_REQ"/>
    /// <remarks>The <see cref="SphynxRequest.AccessToken"/> property is not serialized for this packet.</remarks>
    public sealed class RegisterRequest : SphynxRequest<RegisterResponse>, IEquatable<RegisterRequest>
    {
        /// <summary>
        /// User name entered by user for registration.
        /// </summary>
        public string UserName { get; init; }

        /// <summary>
        /// Password entered by user for registration.
        /// </summary>
        public string Password { get; init; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.REGISTER_REQ;

        /// <summary>
        /// Creates a <see cref="RegisterRequest"/>.
        /// </summary>
        /// <param name="userName">User name entered by user for register.</param>
        /// <param name="password">Password entered by user for register.</param>
        /// <remarks>The <see cref="SphynxRequest.AccessToken"/> property is not serialized for this packet.</remarks>
        public RegisterRequest(string userName, string password) : base(null!)
        {
            UserName = userName;
            Password = password;
        }

        /// <inheritdoc/>
        public bool Equals(RegisterRequest? other) => PacketType == other?.PacketType &&
                                                      UserName == other?.UserName && Password == other?.Password;

        public override RegisterResponse CreateResponse(SphynxErrorInfo errorInfo) => new RegisterResponse(errorInfo);
    }
}
