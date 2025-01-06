using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Sphynx.Core;
using Sphynx.Model.User;
using Sphynx.Utils;

namespace Sphynx.Network.Packet.Response
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

        private int ContentSize
        {
            get
            {
                int contentSize = DEFAULT_CONTENT_SIZE;

                // We must switch to an error state since nothing will be serialized
                if (Users is null || ErrorCode != SphynxErrorCode.SUCCESS)
                {
                    if (ErrorCode == SphynxErrorCode.SUCCESS) ErrorCode = SphynxErrorCode.INVALID_USER;
                    return contentSize;
                }

                contentSize += sizeof(int); // userCount

                for (int i = 0; i < Users.Length; i++)
                {
                    Users[i].GetPacketInfo(true, out _, out int userInfoSize);
                    contentSize += userInfoSize;
                }

                return contentSize;
            }
        }

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
                int userCount = contents[USER_COUNT_OFFSET..].ReadInt32();
                var users = new SphynxUserInfo[userCount];

                for (int i = 0, cursorOffset = USERS_OFFSET; i < userCount; i++)
                {
                    if (!SphynxUserInfo.TryDeserialize(contents[cursorOffset..], true, out var userInfo, out int bytesRead))
                    {
                        packet = null;
                        return false;
                    }

                    users[i] = userInfo;
                    cursorOffset += bytesRead;
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
            int bufferSize = SphynxPacketHeader.HEADER_SIZE + ContentSize;

            try
            {
                if (!TrySerialize(packetBytes = new byte[bufferSize]))
                {
                    packetBytes = null;
                    return false;
                }
            }
            catch
            {
                packetBytes = null;
                return false;
            }

            return true;
        }

        /// <inheritdoc/>
        public override async Task<bool> TrySerializeAsync(Stream stream)
        {
            if (!stream.CanWrite) return false;

            int bufferSize = SphynxPacketHeader.HEADER_SIZE + ContentSize;
            byte[] rawBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            var buffer = rawBuffer.AsMemory()[..bufferSize];

            try
            {
                if (TrySerialize(buffer.Span))
                {
                    await stream.WriteAsync(buffer);
                    return true;
                }
            }
            catch
            {
                return false;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rawBuffer);
            }

            return false;
        }

        private bool TrySerialize(Span<byte> buffer)
        {
            if (!TrySerializeHeader(buffer) || !TrySerializeDefaults(buffer = buffer[SphynxPacketHeader.HEADER_SIZE..]))
            {
                return false;
            }

            // We only serialize users on success
            if (ErrorCode != SphynxErrorCode.SUCCESS) return true;

            int userCount = Users?.Length ?? 0;
            userCount.WriteBytes(buffer[USER_COUNT_OFFSET..]);

            for (int i = 0, cursorOffset = USERS_OFFSET; i < userCount; i++)
            {
                if (!Users![i].TrySerialize(buffer, true, out int bytesWritten))
                {
                    return false;
                }

                cursorOffset += bytesWritten;
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