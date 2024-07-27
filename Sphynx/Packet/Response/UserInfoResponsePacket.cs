using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Sphynx.Core;
using Sphynx.Utils;

namespace Sphynx.Packet.Response
{
    /// <inheritdoc cref="SphynxPacketType.USER_INFO_RES"/>
    public sealed class UserInfoResponsePacket : SphynxResponsePacket, IEquatable<UserInfoResponsePacket>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.LOGOUT_RES;

        /// <summary>
        /// The resolved users' information.
        /// </summary>
        public SphynxUserInfo[]? Users { get; set; }

        private const int USER_COUNT_OFFSET = DEFAULT_CONTENT_SIZE;
        private const int USERS_OFFSET = USER_COUNT_OFFSET + sizeof(int);

        /// <summary>
        /// Creates a new <see cref="UserInfoResponsePacket"/>.
        /// </summary>
        /// <param name="errorCode">Error code for logout attempt.</param>
        public UserInfoResponsePacket(SphynxErrorCode errorCode) : base(errorCode)
        {
        }

        /// <summary>
        /// Creates a new <see cref="UserInfoResponsePacket"/> with <see cref="SphynxErrorCode.SUCCESS"/>.
        /// </summary>
        /// <param name="users">The resolved users' information.</param>
        public UserInfoResponsePacket(params SphynxUserInfo[] users) : this(SphynxErrorCode.SUCCESS)
        {
            Users = users;
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="LogoutResponsePacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        /// <param name="packet">The deserialized packet.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out UserInfoResponsePacket? packet)
        {
            int minContentSize = DEFAULT_CONTENT_SIZE + sizeof(int);

            if (!TryDeserializeDefaults(contents, out SphynxErrorCode? errorCode) ||
                (errorCode.Value == SphynxErrorCode.SUCCESS && contents.Length < minContentSize))
            {
                packet = null;
                return false;
            }

            // We only provide user info on success
            if (errorCode != SphynxErrorCode.SUCCESS)
            {
                packet = new UserInfoResponsePacket(errorCode.Value);
                return true;
            }

            try
            {
                int userCount = contents.ReadInt32(USER_COUNT_OFFSET);
                var users = new SphynxUserInfo[userCount];

                for (int i = 0, cursorOffset = USERS_OFFSET; i < userCount; i++)
                {
                    var userStatus = (SphynxUserStatus)contents[cursorOffset];
                    cursorOffset += sizeof(SphynxUserStatus);

                    var userId = new Guid(contents.Slice(cursorOffset, GUID_SIZE));
                    cursorOffset += GUID_SIZE;

                    int usernameSize = contents.ReadInt32(cursorOffset);
                    cursorOffset += sizeof(int);

                    string userName = TEXT_ENCODING.GetString(contents.Slice(cursorOffset, usernameSize));
                    cursorOffset += usernameSize;

                    users[i] = new SphynxUserInfo(userId, userName, userStatus);
                }

                packet = new UserInfoResponsePacket(users);
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
            int[] usernameSizes = ArrayPool<int>.Shared.Rent(Users?.Length ?? 0);
            GetPacketInfo(usernameSizes, out int contentSize);
            int bufferSize = SphynxPacketHeader.HEADER_SIZE + contentSize;

            try
            {
                if (!TrySerialize(packetBytes = new byte[bufferSize], usernameSizes))
                {
                    packetBytes = null;
                    return false;
                }
            }
            finally
            {
                ArrayPool<int>.Shared.Return(usernameSizes);
            }

            return true;
        }

        /// <inheritdoc/>
        public override async Task<bool> TrySerializeAsync(Stream stream)
        {
            if (!stream.CanWrite) return false;

            int[] usernameSizes = ArrayPool<int>.Shared.Rent(Users?.Length ?? 0);
            GetPacketInfo(usernameSizes, out int contentSize);

            int bufferSize = SphynxPacketHeader.HEADER_SIZE + contentSize;
            byte[] rawBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            var buffer = rawBuffer.AsMemory()[..bufferSize];

            try
            {
                if (TrySerialize(buffer.Span, usernameSizes))
                {
                    await stream.WriteAsync(buffer);
                    return true;
                }
            }
            finally
            {
                ArrayPool<int>.Shared.Return(usernameSizes);
                ArrayPool<byte>.Shared.Return(rawBuffer);
            }

            return false;
        }

        private void GetPacketInfo(int[] usernameSizes, out int contentSize)
        {
            contentSize = DEFAULT_CONTENT_SIZE;

            // We must switch to an error state since nothing will be serialized
            if (Users is null || ErrorCode != SphynxErrorCode.SUCCESS)
            {
                if (ErrorCode == SphynxErrorCode.SUCCESS)
                    ErrorCode = SphynxErrorCode.INVALID_USER;
                return;
            }

            int partialMsgLength = sizeof(SphynxUserStatus) + GUID_SIZE + 2 * sizeof(long) + 2 * sizeof(long) +
                                   sizeof(int); // userStatus, userId, usernameSize

            contentSize += Users.Length * partialMsgLength;

            for (int i = 0; i < Users.Length; i++)
            {
                string userName = Users[i].UserName;
                contentSize += (usernameSizes[i] = string.IsNullOrEmpty(userName) ? 0 : TEXT_ENCODING.GetByteCount(userName));
            }
        }

        private bool TrySerialize(Span<byte> buffer, int[] usernameSizes)
        {
            if (!TrySerializeHeader(buffer) || !TrySerializeDefaults(buffer = buffer[SphynxPacketHeader.HEADER_SIZE..]))
            {
                return false;
            }

            // We only serialize users on success
            if (ErrorCode != SphynxErrorCode.SUCCESS) return true;

            (Users?.Length ?? 0).WriteBytes(buffer, USER_COUNT_OFFSET);

            for (int i = 0, cursorOffset = USERS_OFFSET; i < (Users?.Length ?? 0); i++)
            {
                var user = Users![i];

                buffer[cursorOffset] = (byte)user.UserStatus;
                cursorOffset += sizeof(SphynxUserStatus);

                user.UserId.TryWriteBytes(buffer.Slice(cursorOffset, GUID_SIZE));
                cursorOffset += GUID_SIZE;

                usernameSizes[i].WriteBytes(buffer, cursorOffset);
                cursorOffset += sizeof(int);

                TEXT_ENCODING.GetBytes(user.UserName, buffer.Slice(cursorOffset, usernameSizes[i]));
                cursorOffset += usernameSizes[i];
            }

            return true;
        }

        /// <inheritdoc/>
        public bool Equals(UserInfoResponsePacket? other)
        {
            if (other is null || !base.Equals(other)) return false;
            if (Users is null && other.Users is null) return true;
            if (Users is null || other.Users is null) return false;

            return MemoryUtils.SequenceEqual(Users, other.Users);
        }
    }
}