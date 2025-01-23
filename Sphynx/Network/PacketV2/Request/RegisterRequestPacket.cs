using Sphynx.Core;

namespace Sphynx.Network.PacketV2.Request
{
    /// <inheritdoc cref="SphynxPacketType.REGISTER_REQ"/>
    /// <remarks>The <see cref="SphynxRequestPacket.UserId"/> and <see cref="SphynxRequestPacket.SessionId"/> properties
    /// are not serialized for this packet.</remarks>
    public sealed class RegisterRequestPacket : SphynxRequestPacket, IEquatable<RegisterRequestPacket>
    {
        /// <summary>
        /// User name entered by user for login.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Password entered by user for login.
        /// </summary>
        public string Password { get; set; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.LOGIN_REQ;

        /// <summary>
        /// Creates a <see cref="RegisterRequestPacket"/>.
        /// </summary>
        /// <param name="userName">User name entered by user for register.</param>
        /// <param name="password">Password entered by user for register.</param>
        /// <remarks>The <see cref="SphynxRequestPacket.UserId"/> and <see cref="SphynxRequestPacket.SessionId"/> properties
        /// are not serialized for this packet.</remarks>
        public RegisterRequestPacket(string userName, string password) : base(SnowflakeId.Empty, Guid.Empty)
        {
            UserName = userName;
            Password = password;
        }

        /// <inheritdoc/>
        public bool Equals(RegisterRequestPacket? other) => PacketType == other?.PacketType &&
                                                            UserName == other?.UserName && Password == other?.Password;
    }
}
