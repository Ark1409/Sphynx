using System.Runtime.InteropServices;

namespace Sphynx.Packet.Request
{
    /// <inheritdoc cref="SphynxPacketType.LOGIN_REQ"/>
    public sealed class LoginRequestPacket : SphynxPacket
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

        /// <summary>
        /// Creates a <see cref="LoginRequestPacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        public LoginRequestPacket(ReadOnlySpan<byte> contents)
        {
            int emailSize = MemoryMarshal.Cast<byte, int>(contents.Slice(0, sizeof(int)))[0];

            const int EMAIL_OFFSET = sizeof(int) + 1;
            Email = TEXT_ENCODING.GetString(contents.Slice(EMAIL_OFFSET, emailSize));

            //                              //
            // TODO: Read password bytes    //
            //                              //
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
            int contentSize = emailSize + PASSWORD_SIZE;

            byte[] serializedBytes = new byte[SphynxRequestHeader.HEADER_SIZE + contentSize];

            SerializeData(new Span<byte>(serializedBytes), contentSize, emailSize);

            return serializedBytes;
        }

        private void SerializeData(Span<byte> stream, int contentSize, int emailSize)
        {
            var header = new SphynxRequestHeader(PacketType, contentSize);
            header.Serialize(stream.Slice(0, SphynxRequestHeader.HEADER_SIZE));

            Span<byte> serializedEmailSize = MemoryMarshal.Cast<int, byte>(stackalloc int[] { emailSize });
            serializedEmailSize.CopyTo(stream.Slice(SphynxRequestHeader.HEADER_SIZE));

            int EMAIL_OFFSET = SphynxRequestHeader.HEADER_SIZE + serializedEmailSize.Length;
            TEXT_ENCODING.GetBytes(Email, stream.Slice(EMAIL_OFFSET, emailSize));

            //                                  //
            // TODO: Serialize hashed password  //
            //                                  //
        }
    }
}
