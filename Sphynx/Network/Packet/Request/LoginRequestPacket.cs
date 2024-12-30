using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Sphynx.Utils;

namespace Sphynx.Network.Packet.Request
{
    /// <inheritdoc cref="SphynxPacketType.LOGIN_REQ"/>
    /// <remarks>The <see cref="SphynxRequestPacket.UserId"/> and <see cref="SphynxRequestPacket.SessionId"/> properties
    /// are not serialized for this packet.</remarks>
    public sealed class LoginRequestPacket : SphynxRequestPacket, IEquatable<LoginRequestPacket>
    {
        /// <summary>
        /// User name entered by user for login.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Password entered by user for login.
        /// </summary>
        /// <remarks>We rely on SSL connection.</remarks>
        public string Password { get; set; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.LOGIN_REQ;

        private const int USERNAME_SIZE_OFFSET = 0;
        private const int USERNAME_OFFSET = USERNAME_SIZE_OFFSET + sizeof(int);

        /// <summary>
        /// Creates a <see cref="LoginRequestPacket"/>.
        /// </summary>
        /// <param name="userName">User name entered by user for login.</param>
        /// <param name="password">Password entered by user for login.</param>
        /// <remarks>The <see cref="SphynxRequestPacket.UserId"/> and <see cref="SphynxRequestPacket.SessionId"/> properties
        /// are not serialized for this packet.</remarks>
        public LoginRequestPacket(string userName, string password) : base(Guid.Empty, Guid.Empty)
        {
            UserName = userName;
            Password = password;
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="LoginRequestPacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        /// <param name="packet">The deserialized packet.</param>
        /// <remarks>The <see cref="SphynxRequestPacket.UserId"/> and <see cref="SphynxRequestPacket.SessionId"/> properties
        /// are not expected to be within the contents of the packet.</remarks>
        public static bool TryDeserialize(ReadOnlySpan<byte> contents,
            [NotNullWhen(true)] out LoginRequestPacket? packet)
        {
            int minContentSize = sizeof(int) + sizeof(int); // UsernameSize, PasswordSize

            if (contents.Length < minContentSize)
            {
                packet = null;
                return false;
            }

            try
            {
                int usernameSize = contents[USERNAME_SIZE_OFFSET..].ReadInt32();
                string userName = TEXT_ENCODING.GetString(contents.Slice(USERNAME_OFFSET, usernameSize));

                int PASSWORD_SIZE_OFFSET = USERNAME_OFFSET + usernameSize;
                int passwordSize = contents[PASSWORD_SIZE_OFFSET..].ReadInt32();
                int PASSWORD_OFFSET = PASSWORD_SIZE_OFFSET + sizeof(int);
                string password = TEXT_ENCODING.GetString(contents.Slice(PASSWORD_OFFSET, passwordSize));

                packet = new LoginRequestPacket(userName, password);
                return true;
            }
            catch
            {
                packet = null;
                return false;
            }
        }

        /// <inheritdoc/>
        /// <remarks>The <see cref="SphynxRequestPacket.UserId"/> and <see cref="SphynxRequestPacket.SessionId"/> properties
        /// are not serialized for this packet.</remarks>
        public override bool TrySerialize([NotNullWhen(true)] out byte[]? packetBytes)
        {
            GetPacketInfo(out int usernameSize, out int passwordSize, out int contentSize);

            int bufferSize = SphynxPacketHeader.HEADER_SIZE + contentSize;

            if (!TrySerialize(packetBytes = new byte[bufferSize], usernameSize, passwordSize))
            {
                packetBytes = null;
                return false;
            }

            return true;
        }

        /// <inheritdoc/>
        /// <remarks>The <see cref="SphynxRequestPacket.UserId"/> and <see cref="SphynxRequestPacket.SessionId"/> properties
        /// are not serialized for this packet.</remarks>
        public override async Task<bool> TrySerializeAsync(Stream stream)
        {
            if (!stream.CanWrite) return false;

            GetPacketInfo(out int usernameSize, out int passwordSize, out int contentSize);

            int bufferSize = SphynxPacketHeader.HEADER_SIZE + contentSize;
            byte[] rawBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            var buffer = rawBuffer.AsMemory()[..bufferSize];

            try
            {
                if (TrySerialize(buffer.Span, usernameSize, passwordSize))
                {
                    await stream.WriteAsync(buffer);
                    return true;
                }
            }
            catch
            {
                ArrayPool<byte>.Shared.Return(rawBuffer);
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GetPacketInfo(out int usernameSize, out int passwordSize, out int contentSize)
        {
            usernameSize = !string.IsNullOrEmpty(UserName) ? TEXT_ENCODING.GetByteCount(UserName) : 0;
            passwordSize = !string.IsNullOrEmpty(Password) ? TEXT_ENCODING.GetByteCount(Password) : 0;
            contentSize = sizeof(int) + usernameSize + sizeof(int) + passwordSize;
        }

        private bool TrySerialize(Span<byte> buffer, int usernameSize, int passwordSize)
        {
            if (!TrySerializeHeader(buffer)) // Don't serialize UserId and SessionId for login packet
            {
                return false;
            }

            buffer = buffer[SphynxPacketHeader.HEADER_SIZE..];

            usernameSize.WriteBytes(buffer[USERNAME_SIZE_OFFSET..]);
            TEXT_ENCODING.GetBytes(UserName, buffer.Slice(USERNAME_OFFSET, usernameSize));

            int PASSWORD_SIZE_OFFSET = USERNAME_OFFSET + usernameSize;
            passwordSize.WriteBytes(buffer[PASSWORD_SIZE_OFFSET..]);
            int PASSWORD_OFFSET = PASSWORD_SIZE_OFFSET + sizeof(int);
            TEXT_ENCODING.GetBytes(Password, buffer.Slice(PASSWORD_OFFSET, passwordSize));

            return true;
        }

        /// <inheritdoc/>
        public bool Equals(LoginRequestPacket? other) =>
            PacketType == other?.PacketType && UserName == other?.UserName && Password == other?.Password;
    }
}