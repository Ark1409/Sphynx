using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Sphynx.Core;
using Sphynx.Utils;

namespace Sphynx.Packet.Response
{
    /// <inheritdoc cref="SphynxPacketType.LOGIN_RES"/>
    public sealed class LoginResponsePacket : SphynxResponsePacket, IEquatable<LoginResponsePacket>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.LOGIN_RES;

        /// <summary>
        /// Holds the authenticated user's information.
        /// </summary>
        public SphynxUserInfo? UserInfo { get; set; }

        /// <summary>
        /// The session ID for the client.
        /// </summary>
        public Guid? SessionId { get; set; }

        private const int SESSION_ID_OFFSET = DEFAULT_CONTENT_SIZE;
        private static readonly int USER_ID_OFFSET = SESSION_ID_OFFSET + GUID_SIZE;
        private static readonly int USER_STATUS_OFFSET = USER_ID_OFFSET + GUID_SIZE;
        private static readonly int USERNAME_SIZE_OFFSET = USER_STATUS_OFFSET + sizeof(SphynxUserStatus);
        private static readonly int USERNAME_OFFSET = USERNAME_SIZE_OFFSET + sizeof(int);

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

        /// <summary>
        /// Creates a new <see cref="LoginResponsePacket"/>.
        /// </summary>
        /// <param name="userInfo">Holds the authenticated user's information.</param>
        /// <param name="sessionId">The session ID for the client.</param>
        public LoginResponsePacket(SphynxUserInfo userInfo, Guid sessionId) : this(SphynxErrorCode.SUCCESS)
        {
            UserInfo = userInfo;
            SessionId = sessionId;
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="LoginResponsePacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        /// <param name="packet">The deserialized packet.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out LoginResponsePacket? packet)
        {
            int minContentSize = DEFAULT_CONTENT_SIZE + GUID_SIZE + GUID_SIZE + sizeof(SphynxUserStatus) + sizeof(int) + sizeof(int) +
                                 sizeof(int); // SessionId, UserId, UserStatus, UsernameSize, FriendCount, RoomCount

            if (!TryDeserializeDefaults(contents, out SphynxErrorCode? errorCode) ||
                (errorCode.Value == SphynxErrorCode.SUCCESS && contents.Length < minContentSize))
            {
                packet = null;
                return false;
            }

            // We only serialize user info when authentication is successful
            if (errorCode != SphynxErrorCode.SUCCESS)
            {
                packet = new LoginResponsePacket(errorCode.Value);
                return true;
            }

            // Deserialize session ID and user info
            try
            {
                var sessionId = new Guid(contents.Slice(SESSION_ID_OFFSET, GUID_SIZE));

                var userId = new Guid(contents.Slice(USER_ID_OFFSET, GUID_SIZE));
                var userStatus = (SphynxUserStatus)contents[USER_STATUS_OFFSET];
                int usernameSize = contents.ReadInt32(USERNAME_SIZE_OFFSET);
                string userName = TEXT_ENCODING.GetString(contents.Slice(USERNAME_OFFSET, usernameSize));

                // Deserialize friends
                int FRIEND_COUNT_OFFSET = USERNAME_OFFSET + usernameSize;
                int friendCount = contents.ReadInt32(FRIEND_COUNT_OFFSET);

                int FRIENDS_OFFSET = FRIEND_COUNT_OFFSET + sizeof(int);
                var friends = new Guid[friendCount];
                for (int i = 0; i < friends.Length; i++)
                {
                    friends[i] = new Guid(contents.Slice(FRIENDS_OFFSET + GUID_SIZE * i, GUID_SIZE));
                }

                // Deserialize joined rooms
                int ROOM_COUNT_OFFSET = FRIENDS_OFFSET + friendCount * GUID_SIZE;
                int roomCount = contents.ReadInt32(ROOM_COUNT_OFFSET);

                int ROOMS_OFFSET = ROOM_COUNT_OFFSET + sizeof(int);
                var rooms = new Guid[roomCount];
                for (int i = 0; i < rooms.Length; i++)
                {
                    rooms[i] = new Guid(contents.Slice(ROOMS_OFFSET + GUID_SIZE * i, GUID_SIZE));
                }

                var userInfo = new SphynxUserInfo(userId, userName, userStatus, friends, rooms);

                packet = new LoginResponsePacket(userInfo, sessionId);
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
            GetPacketInfo(out int usernameSize, out int contentSize);
            int bufferSize = SphynxPacketHeader.HEADER_SIZE + contentSize;

            if (!TrySerialize(packetBytes = new byte[bufferSize], usernameSize))
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

            GetPacketInfo(out int usernameSize, out int contentSize);

            int bufferSize = SphynxPacketHeader.HEADER_SIZE + contentSize;
            byte[] rawBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            var buffer = rawBuffer.AsMemory()[..bufferSize];

            try
            {
                if (TrySerialize(buffer.Span, usernameSize))
                {
                    await stream.WriteAsync(buffer);
                    return true;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rawBuffer);
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GetPacketInfo(out int usernameSize, out int contentSize)
        {
            usernameSize = !SessionId.HasValue || string.IsNullOrEmpty(UserInfo?.UserName)
                ? 0
                : TEXT_ENCODING.GetByteCount(UserInfo.UserName);
            contentSize = DEFAULT_CONTENT_SIZE;

            // We only serialize user info when authentication is successful
            if (SessionId.HasValue)
            {
                contentSize += GUID_SIZE + GUID_SIZE + sizeof(SphynxUserStatus) +
                               sizeof(int) + usernameSize +
                               sizeof(int) + (UserInfo!.Friends?.Count ?? 0) * GUID_SIZE +
                               sizeof(int) + (UserInfo!.Rooms?.Count ?? 0) * GUID_SIZE;
            }
        }

        private bool TrySerialize(Span<byte> buffer, int usernameSize)
        {
            if (!TrySerializeHeader(buffer) || !TrySerializeDefaults(buffer = buffer[SphynxPacketHeader.HEADER_SIZE..]))
            {
                return false;
            }

            // We only serialize user info when authentication is successful
            if (ErrorCode != SphynxErrorCode.SUCCESS) return true;

            SessionId!.Value.TryWriteBytes(buffer.Slice(SESSION_ID_OFFSET, GUID_SIZE));
            UserInfo!.UserId.TryWriteBytes(buffer.Slice(USER_ID_OFFSET, GUID_SIZE));
            buffer[USER_STATUS_OFFSET] = (byte)(UserInfo?.UserStatus ?? 0);
            usernameSize.WriteBytes(buffer, USERNAME_SIZE_OFFSET);
            TEXT_ENCODING.GetBytes(UserInfo!.UserName, buffer.Slice(USERNAME_OFFSET, usernameSize));

            // Serialize friends
            int FRIEND_COUNT_OFFSET = USERNAME_OFFSET + usernameSize;
            (UserInfo!.Friends?.Count ?? 0).WriteBytes(buffer, FRIEND_COUNT_OFFSET);

            int FRIENDS_OFFSET = FRIEND_COUNT_OFFSET + sizeof(int);
            if (UserInfo.Friends is not null)
            {
                int index = 0;
                foreach (var friendId in UserInfo!.Friends)
                {
                    friendId.TryWriteBytes(buffer.Slice(FRIENDS_OFFSET + GUID_SIZE * index++, GUID_SIZE));
                }
            }

            int ROOM_COUNT_OFFSET = FRIENDS_OFFSET + (UserInfo!.Friends?.Count ?? 0) * GUID_SIZE;
            (UserInfo!.Rooms?.Count ?? 0).WriteBytes(buffer, ROOM_COUNT_OFFSET);

            // Serialize joined rooms
            int ROOMS_OFFSET = ROOM_COUNT_OFFSET + sizeof(int);
            if (UserInfo!.Rooms is not null)
            {
                int index = 0;
                foreach (var roomId in UserInfo.Rooms)
                {
                    roomId.TryWriteBytes(buffer.Slice(ROOMS_OFFSET + GUID_SIZE * index++, GUID_SIZE));
                }
            }

            return true;
        }

        /// <inheritdoc/>
        public bool Equals(LoginResponsePacket? other) =>
            base.Equals(other) && SessionId == other?.SessionId && (UserInfo?.Equals(other?.UserInfo) ?? true);
    }
}