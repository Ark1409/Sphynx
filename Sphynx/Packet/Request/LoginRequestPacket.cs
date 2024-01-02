using System.Diagnostics.CodeAnalysis;

using Microsoft.VisualBasic;

using Sphynx.Utils;

namespace Sphynx.Packet.Request
{
    /// <inheritdoc cref="SphynxPacketType.LOGIN_REQ"/>
    public sealed class LoginRequestPacket : SphynxRequestPacket, IEquatable<LoginRequestPacket>
    {
        /// <summary>
        /// Email entered by user for login.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Password entered by user for login.
        /// </summary>
        // TODO: !!! Temporary !!!
        public string Password { get; set; }
        // TODO: !!! Temporary !!!

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.LOGIN_REQ;

        private const int EMAIL_SIZE_OFFSET = 0;
        private const int EMAIL_OFFSET = EMAIL_SIZE_OFFSET + sizeof(int);

        /// <summary>
        /// Creates a <see cref="LoginRequestPacket"/>.
        /// </summary>
        /// <param name="email">Email entered by user for login.</param>
        /// <param name="password">Password entered by user for login.</param>
        public LoginRequestPacket(string email, string password) : this(Guid.Empty, Guid.Empty, email, password)
        {

        }

        /// <summary>
        /// Creates a <see cref="LoginRequestPacket"/>.
        /// </summary>
        /// <param name="userId">The user ID of the requesting user.</param>
        /// <param name="sessionId">The session ID for the requesting user.</param>
        /// <param name="email">Email entered by user for login.</param>
        /// <param name="password">Password entered by user for login.</param>
        public LoginRequestPacket(Guid userId, Guid sessionId, string email, string password) : base(userId, sessionId)
        {
            Email = email;
            Password = password;
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="LoginRequestPacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        /// <param name="packet">The deserialized packet.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out LoginRequestPacket? packet)
        {
            if (TryDeserialize(contents[..DEFAULT_CONTENT_SIZE], out var userId, out var sessionId) &&
                TryDeserializeContents(contents[DEFAULT_CONTENT_SIZE..], out packet))
            {
                packet.UserId = userId.Value;
                packet.SessionId = sessionId.Value;
                return true;
            }

            packet = null;
            return false;
        }

        private static bool TryDeserializeContents(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out LoginRequestPacket? packet)
        {
            // Avoid exceptions on server
            try
            {
                int emailSize = contents.ReadInt32(EMAIL_SIZE_OFFSET);
                string email = TEXT_ENCODING.GetString(contents.Slice(EMAIL_OFFSET, emailSize));

                // TODO: Read hashed password bytes
                int PASSWORD_SIZE_OFFSET = EMAIL_OFFSET + emailSize;
                int passwordSize = contents.ReadInt32(PASSWORD_SIZE_OFFSET);

                int PASSWORD_OFFSET = PASSWORD_SIZE_OFFSET + sizeof(int);
                string password = TEXT_ENCODING.GetString(contents.Slice(PASSWORD_OFFSET, passwordSize));

                packet = new LoginRequestPacket(email, password);
                return true;
            }
            catch
            {
                packet = null;
                return false;
            }
        }

        /// <inheritdoc/>
        public override bool TrySerialize(out byte[]? packetBytes)
        {
            int emailSize = TEXT_ENCODING.GetByteCount(Email);
            int passwordSize = TEXT_ENCODING.GetByteCount(Password);
            int contentSize = DEFAULT_CONTENT_SIZE + sizeof(int) + emailSize + sizeof(int) + passwordSize;

            packetBytes = new byte[SphynxPacketHeader.HEADER_SIZE + contentSize];
            var packetSpan = new Span<byte>(packetBytes);

            if (TrySerializeHeader(packetSpan[..SphynxPacketHeader.HEADER_SIZE], contentSize) &&
                TrySerialize(packetSpan.Slice(SphynxPacketHeader.HEADER_SIZE, DEFAULT_CONTENT_SIZE)))
            {
                SerializeContents(packetSpan[DEFAULT_CONTENT_SIZE..], emailSize, passwordSize);
                return true;
            }

            packetBytes = null;
            return false;
        }

        private void SerializeContents(Span<byte> buffer, int emailSize, int passwordSize)
        {
            emailSize.WriteBytes(buffer, EMAIL_SIZE_OFFSET);
            TEXT_ENCODING.GetBytes(Email, buffer.Slice(EMAIL_OFFSET, emailSize));

            // TODO: Serialize hashed password
            int PASSWORD_SIZE_OFFSET = EMAIL_OFFSET + emailSize;
            passwordSize.WriteBytes(buffer, PASSWORD_SIZE_OFFSET);

            int PASSWORD_OFFSET = PASSWORD_SIZE_OFFSET + sizeof(int);
            TEXT_ENCODING.GetBytes(Password, buffer.Slice(PASSWORD_OFFSET, passwordSize));
        }

        /// <inheritdoc/>
        public bool Equals(LoginRequestPacket? other) => base.Equals(other) && Email == other?.Email && Password == other?.Password;
    }
}
