using System.Runtime.InteropServices;

namespace Sphynx.Packet.Request
{
    /// <inheritdoc cref="SphynxPacketType.LOGIN_REQ"/>
    public sealed class LoginRequestPacket : SphynxRequestPacket
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
            int emailSize = MemoryMarshal.Cast<byte, int>(contents.Slice(EMAIL_SIZE_OFFSET, sizeof(int)))[0];
            Email = TEXT_ENCODING.GetString(contents.Slice(EMAIL_OFFSET, emailSize));

            // ---------------------------- //
            // TODO: Read password bytes    //
            // ---------------------------- //
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
            const int PASSWORD_SIZE = 256;
            int contentSize = sizeof(int) + emailSize + PASSWORD_SIZE;

            byte[] serializedBytes = new byte[SphynxRequestHeader.HEADER_SIZE + contentSize];
            var serializationSpan = new Span<byte>(serializedBytes);

            SerializeHeader(serializationSpan.Slice(0, SphynxRequestHeader.HEADER_SIZE), contentSize);
            SerializeContents(serializationSpan.Slice(SphynxRequestHeader.HEADER_SIZE), emailSize);

            return serializedBytes;
        }
        
        private void SerializeContents(Span<byte> buffer, int emailSize)
        {
            Span<byte> emailSizeBytes = MemoryMarshal.Cast<int, byte>(stackalloc int[] { emailSize });
            emailSizeBytes.CopyTo(buffer.Slice(EMAIL_SIZE_OFFSET, sizeof(int)));

            TEXT_ENCODING.GetBytes(Email, buffer.Slice(EMAIL_OFFSET, emailSize));

            // -------------------------------- //
            // TODO: Serialize hashed password  //
            // -------------------------------- //
        }
    }
}
