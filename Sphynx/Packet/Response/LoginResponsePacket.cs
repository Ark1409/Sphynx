namespace Sphynx.Packet.Response
{
    /// <inheritdoc cref="SphynxPacketType.LOGIN_RES"/>
    public sealed class LoginResponsePacket : SphynxResponsePacket, IEquatable<LoginResponsePacket>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.LOGIN_RES;

        /// <summary>
        /// Creates a new <see cref="LoginResponsePacket"/> with <see cref="SphynxErrorCode.SUCCESS"/>.
        /// </summary>
        public LoginResponsePacket() : this(SphynxErrorCode.SUCCESS)
        {

        }

        /// <summary>
        /// Creates a new <see cref="LoginResponsePacket"/>.
        /// </summary>
        /// <param name="errorCode">Error code for login attempt.</param>
        public LoginResponsePacket(SphynxErrorCode errorCode) : base(errorCode)
        {

        }

        /// <inheritdoc/>
        public override byte[] Serialize()
        {
            byte[] packetBytes = new byte[SphynxResponseHeader.HEADER_SIZE];
            var packetSpan = new Span<byte>(packetBytes);

            SerializeHeader(packetSpan, 0);

            return packetBytes;
        }

        /// <inheritdoc/>
        public bool Equals(LoginResponsePacket? other) => base.Equals(other);
    }
}
