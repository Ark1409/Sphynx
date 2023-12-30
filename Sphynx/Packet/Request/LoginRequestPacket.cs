using System.Runtime.InteropServices;

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
        /// <param name="contents">Packet contents, excluding the header.</param>
        public LoginRequestPacket(ReadOnlySpan<byte> contents)
        {
            int emailSize = contents.Slice(EMAIL_SIZE_OFFSET, sizeof(int)).ReadInt32();
            Email = TEXT_ENCODING.GetString(contents.Slice(EMAIL_OFFSET, emailSize));

            // TODO: Read hashed password bytes
            int PASSWORD_SIZE_OFFSET = EMAIL_OFFSET + emailSize;
            int passwordSize = contents.Slice(PASSWORD_SIZE_OFFSET, sizeof(int)).ReadInt32();
            Password = TEXT_ENCODING.GetString(contents.Slice(EMAIL_OFFSET + emailSize + sizeof(int), passwordSize));
        }

        /// <summary>
        /// Creates a <see cref="LoginRequestPacket"/>.
        /// </summary>
        /// <param name="email">Email entered by user for login.</param>
        /// <param name="password">Password entered by user for login.</param>
        public LoginRequestPacket(string email, string password)
        {
            Email = email;
            Password = password;
        }

        /// <inheritdoc/>
        public override byte[] Serialize()
        {
            int emailSize = TEXT_ENCODING.GetByteCount(Email);
            int passwordSize = TEXT_ENCODING.GetByteCount(Password);
            int contentSize = sizeof(int) + emailSize + sizeof(int) + passwordSize;

            byte[] serializedBytes = new byte[SphynxRequestHeader.HEADER_SIZE + contentSize];
            var serializationSpan = new Span<byte>(serializedBytes);

            SerializeHeader(serializationSpan.Slice(0, SphynxRequestHeader.HEADER_SIZE), contentSize);
            SerializeContents(serializationSpan.Slice(SphynxRequestHeader.HEADER_SIZE), emailSize, passwordSize);

            return serializedBytes;
        }

        private void SerializeContents(Span<byte> buffer, int emailSize, int passwordSize)
        {
            emailSize.WriteBytes(buffer.Slice(EMAIL_SIZE_OFFSET, sizeof(int)));
            TEXT_ENCODING.GetBytes(Email, buffer.Slice(EMAIL_OFFSET, emailSize));

            // TODO: Serialize hashed password
            int PASSWORD_SIZE_OFFSET = EMAIL_OFFSET + emailSize;
            passwordSize.WriteBytes(buffer.Slice(PASSWORD_SIZE_OFFSET, sizeof(int)));

            int PASSWORD_OFFSET = PASSWORD_SIZE_OFFSET + sizeof(int);
            TEXT_ENCODING.GetBytes(Password, buffer.Slice(PASSWORD_OFFSET, passwordSize));
        }

        /// <inheritdoc/>
        public bool Equals(LoginRequestPacket? other) => Email == other?.Email && Password == other?.Password;
    }
}
