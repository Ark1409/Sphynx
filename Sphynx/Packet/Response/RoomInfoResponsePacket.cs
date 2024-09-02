using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Sphynx.ChatRoom;
using Sphynx.Utils;

namespace Sphynx.Packet.Response
{
    /// <inheritdoc cref="SphynxPacketType.ROOM_INFO_RES"/>
    public sealed class RoomInfoResponsePacket : SphynxResponsePacket, IEquatable<RoomInfoResponsePacket>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.ROOM_INFO_RES;

        /// <summary>
        /// The information for the chat room with the requested room ID.
        /// </summary>
        public ChatRoomInfo? RoomInfo { get; set; }

        private const int ROOM_TYPE_OFFSET = DEFAULT_CONTENT_SIZE;
        private const int ROOM_ID_OFFSET = ROOM_TYPE_OFFSET + sizeof(ChatRoomType);

        private static readonly int USER_ID_ONE_OFFSET = ROOM_ID_OFFSET + GUID_SIZE;
        private static readonly int USER_ID_TWO_OFFSET = USER_ID_ONE_OFFSET + GUID_SIZE;
        private static readonly int ROOM_NAME_SIZE_OFFSET = USER_ID_TWO_OFFSET + GUID_SIZE;
        private static readonly int ROOM_NAME_OFFSET = ROOM_NAME_SIZE_OFFSET + sizeof(int);

        private static readonly int OWNER_ID_OFFSET = ROOM_ID_OFFSET + GUID_SIZE;
        private static readonly int VISIBILITY_OFFSET = OWNER_ID_OFFSET + GUID_SIZE;
        private static readonly int USER_COUNT_OFFSET = VISIBILITY_OFFSET + sizeof(bool);
        private static readonly int PWD_SIZE_OFFSET = USER_COUNT_OFFSET + sizeof(int);
        private static readonly int PWD_OFFSET = PWD_SIZE_OFFSET + sizeof(int);

        /// <summary>
        /// Creates a new <see cref="RoomInfoResponsePacket"/>.
        /// </summary>
        /// <param name="errorCode">The error code for the response packet.</param>
        public RoomInfoResponsePacket(SphynxErrorCode errorCode) : base(errorCode)
        {
        }

        /// <summary>
        /// Creates a new <see cref="RoomInfoResponsePacket"/>.
        /// </summary>
        /// <param name="roomInfo">The error code for the response packet.</param>
        public RoomInfoResponsePacket(ChatRoomInfo roomInfo) : this(SphynxErrorCode.SUCCESS)
        {
            RoomInfo = roomInfo;
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="RoomInfoResponsePacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        /// <param name="packet">The deserialized packet.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out RoomInfoResponsePacket? packet)
        {
            // GROUP (smaller than DIRECT) - Room type, RoomId, OwnerId, IsPublic, UserCount, Pwd size
            int minContentSize = DEFAULT_CONTENT_SIZE + sizeof(ChatRoomType) + GUID_SIZE + GUID_SIZE + sizeof(bool) + sizeof(int) + sizeof(int);

            if (!TryDeserializeDefaults(contents, out SphynxErrorCode? errorCode) ||
                (errorCode.Value == SphynxErrorCode.SUCCESS && contents.Length < minContentSize))
            {
                packet = null;
                return false;
            }

            // We only provide recent room info on success
            if (errorCode != SphynxErrorCode.SUCCESS)
            {
                packet = new RoomInfoResponsePacket(errorCode.Value);
                return true;
            }

            try
            {
                var roomType = (ChatRoomType)contents[ROOM_TYPE_OFFSET];

                switch (roomType)
                {
                    case ChatRoomType.DIRECT_MSG:
                        return TryDeserializeDirect(contents, out packet);

                    case ChatRoomType.GROUP:
                        return TryDeserializeGroup(contents, out packet);
                }

                packet = null;
                return false;
            }
            catch
            {
                packet = null;
                return false;
            }
        }

        private static bool TryDeserializeDirect(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out RoomInfoResponsePacket? packet)
        {
            var roomId = new Guid(contents.Slice(ROOM_ID_OFFSET, GUID_SIZE));

            var userOneId = new Guid(contents.Slice(USER_ID_ONE_OFFSET, GUID_SIZE));
            var userTwoId = new Guid(contents.Slice(USER_ID_TWO_OFFSET, GUID_SIZE));

            int roomNameSize = contents[ROOM_NAME_SIZE_OFFSET..].ReadInt32();
            string roomName = TEXT_ENCODING.GetString(contents.Slice(ROOM_NAME_OFFSET, roomNameSize));

            var roomInfo = new ChatRoomInfo.Direct(roomId, roomName, userOneId, userTwoId);
            packet = new RoomInfoResponsePacket(roomInfo);
            return true;
        }

        private static bool TryDeserializeGroup(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out RoomInfoResponsePacket? packet)
        {
            var roomId = new Guid(contents.Slice(ROOM_ID_OFFSET, GUID_SIZE));

            var ownerId = new Guid(contents.Slice(OWNER_ID_OFFSET, GUID_SIZE));
            bool isPublic = contents[VISIBILITY_OFFSET] != 0;
            int userCount = contents[USER_COUNT_OFFSET..].ReadInt32();

            int passwordSize = contents[PWD_SIZE_OFFSET..].ReadInt32();
            string password = TEXT_ENCODING.GetString(contents.Slice(PWD_OFFSET, passwordSize));

            int ROOM_NAME_SIZE_OFFSET = PWD_OFFSET + passwordSize;
            int roomNameSize = contents[ROOM_NAME_SIZE_OFFSET..].ReadInt32();

            int ROOM_NAME_OFFSET = ROOM_NAME_SIZE_OFFSET + sizeof(int);
            string roomName = TEXT_ENCODING.GetString(contents.Slice(ROOM_NAME_OFFSET, roomNameSize));

            int USERS_OFFSET = ROOM_NAME_OFFSET + roomNameSize;
            var users = new HashSet<Guid>(userCount);

            for (int i = 0; i < userCount; i++)
            {
                users.Add(new Guid(contents.Slice(USERS_OFFSET + i * GUID_SIZE, GUID_SIZE)));
            }

            var roomInfo = passwordSize > 0
                ? new ChatRoomInfo.Group(roomId, ownerId, roomName, password, isPublic, users)
                : new ChatRoomInfo.Group(roomId, ownerId, roomName, isPublic, users);

            packet = new RoomInfoResponsePacket(roomInfo);
            return true;
        }

        /// <inheritdoc/>
        public override bool TrySerialize([NotNullWhen(true)] out byte[]? packetBytes)
        {
            switch (RoomInfo?.RoomType)
            {
                case ChatRoomType.DIRECT_MSG:
                {
                    GetDirectPacketInfo(out int roomNameSize, out int contentSize);
                    int bufferSize = SphynxPacketHeader.HEADER_SIZE + contentSize;

                    try
                    {
                        if (!TrySerializeDirect(packetBytes = new byte[bufferSize], roomNameSize))
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

                case ChatRoomType.GROUP:
                {
                    GetGroupPacketInfo(out int pwdSize, out int roomNameSize, out int contentSize);
                    int bufferSize = SphynxPacketHeader.HEADER_SIZE + contentSize;

                    try
                    {
                        if (!TrySerializeGroup(packetBytes = new byte[bufferSize], pwdSize, roomNameSize))
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
            }

            const int NULL_BUFFER_SIZE = SphynxPacketHeader.HEADER_SIZE;
            if (!TrySerializeHeader(packetBytes = new byte[NULL_BUFFER_SIZE]) ||
                !TrySerializeDefaults(packetBytes.AsSpan()[SphynxPacketHeader.HEADER_SIZE..]))
            {
                return false;
            }

            // RoomInfo should only be null when it isn't a success
            return ErrorCode != SphynxErrorCode.SUCCESS;
        }

        /// <inheritdoc/>
        public override async Task<bool> TrySerializeAsync(Stream stream)
        {
            if (!stream.CanWrite) return false;

            switch (RoomInfo?.RoomType)
            {
                case ChatRoomType.DIRECT_MSG:
                {
                    GetDirectPacketInfo(out int roomNameSize, out int contentSize);

                    int bufferSize = SphynxPacketHeader.HEADER_SIZE + contentSize;
                    byte[] rawBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
                    var buffer = rawBuffer.AsMemory()[..bufferSize];

                    try
                    {
                        if (TrySerializeDirect(buffer.Span, roomNameSize))
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

                case ChatRoomType.GROUP:
                {
                    GetGroupPacketInfo(out int pwdSize, out int roomNameSize, out int contentSize);

                    int bufferSize = SphynxPacketHeader.HEADER_SIZE + contentSize;
                    byte[] rawBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
                    var buffer = rawBuffer.AsMemory()[..bufferSize];

                    try
                    {
                        if (TrySerializeGroup(buffer.Span, pwdSize, roomNameSize))
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
            }

            const int NULL_BUFFER_SIZE = SphynxPacketHeader.HEADER_SIZE;
            byte[] rawNullBuffer = ArrayPool<byte>.Shared.Rent(NULL_BUFFER_SIZE);
            var nullBuffer = rawNullBuffer.AsMemory()[..NULL_BUFFER_SIZE];
            try
            {
                if (!TrySerializeHeader(nullBuffer.Span) || !TrySerializeDefaults(nullBuffer.Span[SphynxPacketHeader.HEADER_SIZE..]))
                {
                    return false;
                }

                await stream.WriteAsync(nullBuffer);
            }
            catch
            {
                return false;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rawNullBuffer);
            }

            return ErrorCode != SphynxErrorCode.SUCCESS; // RoomInfo should only be null when it isn't a success
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GetDirectPacketInfo(out int roomNameSize, out int contentSize)
        {
            var roomInfo = RoomInfo as ChatRoomInfo.Direct;

            roomNameSize = string.IsNullOrEmpty(roomInfo?.Name) ? 0 : TEXT_ENCODING.GetByteCount(roomInfo.Name);
            contentSize = DEFAULT_CONTENT_SIZE;

            // We must switch to an error state since nothing will be serialized
            if (roomInfo is null || ErrorCode != SphynxErrorCode.SUCCESS)
            {
                if (ErrorCode == SphynxErrorCode.SUCCESS)
                    ErrorCode = SphynxErrorCode.INVALID_ROOM;
                return;
            }

            contentSize += sizeof(ChatRoomType) + GUID_SIZE + GUID_SIZE + GUID_SIZE + sizeof(int) +
                           roomNameSize; // Room type, RoomId, User 1 Id, User 2 Id, Room name size, Room name
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GetGroupPacketInfo(out int passwordSize, out int roomNameSize, out int contentSize)
        {
            var roomInfo = RoomInfo as ChatRoomInfo.Group;

            passwordSize = string.IsNullOrEmpty(roomInfo?.Password) ? 0 : TEXT_ENCODING.GetByteCount(roomInfo.Password);
            roomNameSize = string.IsNullOrEmpty(roomInfo?.Name) ? 0 : TEXT_ENCODING.GetByteCount(roomInfo.Name);
            contentSize = DEFAULT_CONTENT_SIZE;

            // We must switch to an error state since nothing will be serialized
            if (roomInfo is null || ErrorCode != SphynxErrorCode.SUCCESS)
            {
                if (ErrorCode == SphynxErrorCode.SUCCESS)
                    ErrorCode = SphynxErrorCode.INVALID_ROOM;
                return;
            }

            // Room type, RoomId, OwnerId, IsPublic, UserCount, Pwd size, Pwd, Room name size, Room name, Users 
            contentSize += sizeof(ChatRoomType) + GUID_SIZE + GUID_SIZE + sizeof(bool) + sizeof(int) + sizeof(int) + passwordSize + sizeof(int) +
                           roomNameSize + GUID_SIZE * roomInfo.Users.Count;
        }

        private bool TrySerializeDirect(Span<byte> buffer, int roomNameSize)
        {
            if (!TrySerializeHeader(buffer) || !TrySerializeDefaults(buffer = buffer[SphynxPacketHeader.HEADER_SIZE..]))
            {
                return false;
            }

            if (ErrorCode != SphynxErrorCode.SUCCESS) return true;

            var roomInfo = (ChatRoomInfo.Direct)RoomInfo!;

            buffer[ROOM_TYPE_OFFSET] = (byte)roomInfo.RoomType;
            roomInfo.RoomId.TryWriteBytes(buffer.Slice(ROOM_ID_OFFSET, GUID_SIZE));

            roomInfo.UserOne!.Value.TryWriteBytes(buffer.Slice(USER_ID_ONE_OFFSET, GUID_SIZE));
            roomInfo.UserTwo!.Value.TryWriteBytes(buffer.Slice(USER_ID_TWO_OFFSET, GUID_SIZE));

            roomNameSize.WriteBytes(buffer[ROOM_NAME_SIZE_OFFSET..]);
            TEXT_ENCODING.GetBytes(roomInfo.Name, buffer.Slice(ROOM_NAME_OFFSET, roomNameSize));
            return true;
        }

        private bool TrySerializeGroup(Span<byte> buffer, int pwdSize, int roomNameSize)
        {
            if (!TrySerializeHeader(buffer) || !TrySerializeDefaults(buffer = buffer[SphynxPacketHeader.HEADER_SIZE..]))
            {
                return false;
            }

            if (ErrorCode != SphynxErrorCode.SUCCESS) return true;

            var roomInfo = (ChatRoomInfo.Group)RoomInfo!;

            buffer[ROOM_TYPE_OFFSET] = (byte)roomInfo.RoomType;
            roomInfo.RoomId.TryWriteBytes(buffer.Slice(ROOM_ID_OFFSET, GUID_SIZE));

            roomInfo.OwnerId.TryWriteBytes(buffer.Slice(OWNER_ID_OFFSET, GUID_SIZE));
            buffer[VISIBILITY_OFFSET] = (byte)(roomInfo.Public ? 1 : 0);
            roomInfo.Users.Count.WriteBytes(buffer[USER_COUNT_OFFSET..]);

            pwdSize.WriteBytes(buffer[PWD_SIZE_OFFSET..]);
            TEXT_ENCODING.GetBytes(roomInfo.Password!, buffer.Slice(PWD_OFFSET, pwdSize));

            int ROOM_NAME_SIZE_OFFSET = PWD_OFFSET + pwdSize;
            roomNameSize.WriteBytes(buffer[ROOM_NAME_SIZE_OFFSET..]);

            int ROOM_NAME_OFFSET = ROOM_NAME_SIZE_OFFSET + sizeof(int);
            TEXT_ENCODING.GetBytes(roomInfo.Name, buffer.Slice(ROOM_NAME_OFFSET, roomNameSize));

            int USERS_OFFSET = ROOM_NAME_OFFSET + roomNameSize;
            int index = 0;
            foreach (var userId in roomInfo.Users)
            {
                userId.TryWriteBytes(buffer.Slice(USERS_OFFSET + GUID_SIZE * index++, GUID_SIZE));
            }

            return true;
        }

        /// <inheritdoc/>
        public bool Equals(RoomInfoResponsePacket? other) => base.Equals(other) && (RoomInfo?.Equals(other?.RoomInfo) ?? true);
    }
}