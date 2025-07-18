using Sphynx.Core;

namespace Sphynx.Network.PacketV2.Request
{
    /// <inheritdoc cref="SphynxPacketType.LOGIN_REQ"/>
    /// <remarks>The <see cref="SphynxRequest.AccessToken"/> property is not serialized for this packet.</remarks>
    public sealed class LoginRequest : SphynxRequest, IEquatable<LoginRequest>
    {
        /// <summary>
        /// User name entered by user for login.
        /// </summary>
        public string UserName { get; init; }

        /// <summary>
        /// Password entered by user for login.
        /// </summary>
        public string Password { get; init; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.LOGIN_REQ;

        /// <summary>
        /// Creates a <see cref="LoginRequest"/>.
        /// </summary>
        /// <param name="userName">User name entered by user for login.</param>
        /// <param name="password">Password entered by user for login.</param>
        /// <remarks>The <see cref="SphynxRequest.AccessToken"/> property is not serialized for this packet.</remarks>
        public LoginRequest(string userName, string password) : base(null!)
        {
            UserName = userName;
            Password = password;
        }

        /// <inheritdoc/>
        public bool Equals(LoginRequest? other) =>
            PacketType == other?.PacketType && UserName == other?.UserName && Password == other?.Password;
    }
}
