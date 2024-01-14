using System.Buffers;
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

        private static readonly int USERNAME_SIZE_OFFSET = DEFAULT_CONTENT_SIZE;
        private static readonly int USERNAME_OFFSET = USERNAME_SIZE_OFFSET + sizeof(int);

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
            int minContentSize = DEFAULT_CONTENT_SIZE + sizeof(int) + sizeof(int);

            if (contents.Length < minContentSize || !TryDeserializeDefaults(contents[..DEFAULT_CONTENT_SIZE], out var userId, out var sessionId))
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
            GetContents(out int usernameSize, out int passwordSize, out int contentSize);

            int bufferSize = SphynxPacketHeader.HEADER_SIZE + contentSize;

            if (!TrySerialize(packetBytes = new byte[bufferSize], usernameSize, passwordSize))
            {
                packetBytes = null;
                return false;
            }

            return true;
        }

        /// <inheritdoc/>
        public override bool TrySerialize(Stream stream)
        {
            if (!stream.CanWrite) return false;

            GetContents(out int nameSize, out int passwordSize, out int contentSize);

            int bufferSize = SphynxPacketHeader.HEADER_SIZE + contentSize;
            var rawBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            var buffer = rawBuffer.AsSpan()[..bufferSize];

            try
            {
                if (TrySerialize(buffer, nameSize, passwordSize))
                {
                    stream.Write(buffer);
                    return true;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rawBuffer);
            }

            return false;
        }

        private void GetContents(out int usernameSize, out int passwordSize, out int contentSize)
        {
            usernameSize = !string.IsNullOrEmpty(UserName) ? TEXT_ENCODING.GetByteCount(UserName) : 0;
            passwordSize = !string.IsNullOrEmpty(Password) ? TEXT_ENCODING.GetByteCount(Password) : 0;
            contentSize = DEFAULT_CONTENT_SIZE + sizeof(int) + usernameSize + sizeof(int) + passwordSize;
        }

        private bool TrySerialize(Span<byte> buffer, int usernameSize, int passwordSize)
        {
            if (TrySerializeHeader(buffer) && TrySerializeDefaults(buffer = buffer[SphynxPacketHeader.HEADER_SIZE..]))
            {
                usernameSize.WriteBytes(buffer, USERNAME_SIZE_OFFSET);
                TEXT_ENCODING.GetBytes(UserName, buffer.Slice(USERNAME_OFFSET, usernameSize));

                // TODO: Serialize hashed password
                int PASSWORD_SIZE_OFFSET = USERNAME_OFFSET + usernameSize;
                passwordSize.WriteBytes(buffer, PASSWORD_SIZE_OFFSET);

                int PASSWORD_OFFSET = PASSWORD_SIZE_OFFSET + sizeof(int);
                TEXT_ENCODING.GetBytes(Password, buffer.Slice(PASSWORD_OFFSET, passwordSize));
                return true;
            }

            return false;
        }

        // We need to be able to serialize a "null" UserId and SessionId
        /// <inheritdoc/>
        protected override bool TrySerializeDefaults(Span<byte> contents)
        {
            if (contents.Length < DEFAULT_CONTENT_SIZE)
            {
                return false;
            }

            UserId.TryWriteBytes(contents.Slice(USER_ID_OFFSET, GUID_SIZE));
            SessionId.TryWriteBytes(contents.Slice(SESSION_ID_OFFSET, GUID_SIZE));
            return true;
        }

        /// <inheritdoc/>
        public bool Equals(LoginRequestPacket? other) => base.Equals(other) && UserName == other?.UserName && Password == other?.Password;
    }
}
