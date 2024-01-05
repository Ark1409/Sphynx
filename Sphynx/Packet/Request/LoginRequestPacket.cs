using System.Diagnostics.CodeAnalysis;

using Sphynx.Utils;

namespace Sphynx.Packet.Request
{
    /// <inheritdoc cref="SphynxPacketType.LOGIN_REQ"/>
    public sealed class LoginRequestPacket : SphynxRequestPacket, IEquatable<LoginRequestPacket>
    {
        /// <summary>
        /// User name entered by user for login.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Password entered by user for login.
        /// </summary>
        // TODO: !!! Temporary !!!
        public string Password { get; set; }
        // TODO: !!! Temporary !!!

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.LOGIN_REQ;

        private const int USERNAME_SIZE_OFFSET = DEFAULT_CONTENT_SIZE;
        private const int USERNAME_OFFSET = USERNAME_SIZE_OFFSET + sizeof(int);

        /// <summary>
        /// Creates a <see cref="LoginRequestPacket"/>.
        /// </summary>
        /// <param name="userName">User name entered by user for login.</param>
        /// <param name="password">Password entered by user for login.</param>
        public LoginRequestPacket(string userName, string password) : this(Guid.Empty, Guid.Empty, userName, password)
        {

        }

        /// <summary>
        /// Creates a <see cref="LoginRequestPacket"/>.
        /// </summary>
        /// <param name="userId">The user ID of the requesting user.</param>
        /// <param name="sessionId">The session ID for the requesting user.</param>
        /// <param name="userName">User name entered by user for login.</param>
        /// <param name="password">Password entered by user for login.</param>
        public LoginRequestPacket(Guid userId, Guid sessionId, string userName, string password) : base(userId, sessionId)
        {
            UserName = userName;
            Password = password;
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="LoginRequestPacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        /// <param name="packet">The deserialized packet.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out LoginRequestPacket? packet)
        {
            if (!TryDeserialize(contents[..DEFAULT_CONTENT_SIZE], out var userId, out var sessionId))
            {
                packet = null;
                return false;
            }

            try
            {
                int emailSize = contents.ReadInt32(USERNAME_SIZE_OFFSET);
                string email = TEXT_ENCODING.GetString(contents.Slice(USERNAME_OFFSET, emailSize));

                // TODO: Read hashed password bytes
                int PASSWORD_SIZE_OFFSET = USERNAME_OFFSET + emailSize;
                int passwordSize = contents.ReadInt32(PASSWORD_SIZE_OFFSET);

                int PASSWORD_OFFSET = PASSWORD_SIZE_OFFSET + sizeof(int);
                string password = TEXT_ENCODING.GetString(contents.Slice(PASSWORD_OFFSET, passwordSize));

                packet = new LoginRequestPacket(userId.Value, sessionId.Value, email, password);
                return true;
            }
            catch
            {
                packet = null;
                return false;
            }
        }

        /// <inheritdoc/>
        public override bool TrySerialize([NotNullWhen(true)] out byte[]? packetBytes)
        {
            int emailSize = TEXT_ENCODING.GetByteCount(UserName);
            int passwordSize = TEXT_ENCODING.GetByteCount(Password);
            int contentSize = DEFAULT_CONTENT_SIZE + sizeof(int) + emailSize + sizeof(int) + passwordSize;

            packetBytes = new byte[SphynxPacketHeader.HEADER_SIZE + contentSize];
            var packetSpan = new Span<byte>(packetBytes);

            if (TrySerializeHeader(packetSpan[..SphynxPacketHeader.HEADER_SIZE], contentSize) &&
                TrySerialize(packetSpan = packetSpan[SphynxPacketHeader.HEADER_SIZE..]))
            {
                emailSize.WriteBytes(packetSpan, USERNAME_SIZE_OFFSET);
                TEXT_ENCODING.GetBytes(UserName, packetSpan.Slice(USERNAME_OFFSET, emailSize));

                // TODO: Serialize hashed password
                int PASSWORD_SIZE_OFFSET = USERNAME_OFFSET + emailSize;
                passwordSize.WriteBytes(packetSpan, PASSWORD_SIZE_OFFSET);

                int PASSWORD_OFFSET = PASSWORD_SIZE_OFFSET + sizeof(int);
                TEXT_ENCODING.GetBytes(Password, packetSpan.Slice(PASSWORD_OFFSET, passwordSize));
                return true;
            }

            packetBytes = null;
            return false;
        }

        /// <inheritdoc/>
        public bool Equals(LoginRequestPacket? other) => base.Equals(other) && UserName == other?.UserName && Password == other?.Password;
    }
}
