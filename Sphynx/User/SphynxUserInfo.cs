using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Sphynx.Packet;
using Sphynx.Packet.Response;
using Sphynx.Utils;

namespace Sphynx.User
{
    /// <summary>
    /// A type which holds information about a specific Sphynx user.
    /// </summary>
    public class SphynxUserInfo : IEquatable<SphynxUserInfo>
    {
        #region Model

        /// <summary>
        /// The user ID for this Sphynx user.
        /// </summary>
        public virtual Guid UserId { get; set; }

        /// <summary>
        /// The username for this Sphynx user.
        /// </summary>
        public virtual string UserName { get; set; }

        /// <summary>
        /// The activity status of this Sphynx user.
        /// </summary>
        public virtual SphynxUserStatus UserStatus { get; set; }

        /// <summary>
        /// User IDs of friends for this user.
        /// </summary>
        public virtual ISet<Guid>? Friends { get; set; }

        /// <summary>
        /// Room IDs of chat rooms which this user is in (including DMs).
        /// </summary>
        public virtual ISet<Guid>? Rooms { get; set; }

        /// <summary>
        /// Rooms which contain pending (unread) messages, along with the message IDs of said messages.
        /// </summary>
        public virtual IList<PendingRoomMessageInfo>? PendingRoomMessages { get; set; }

        /// <summary>
        /// The user IDs of outgoing friend requests sent by this user.
        /// </summary>
        public virtual ISet<Guid>? OutgoingFriendRequests { get; set; }

        /// <summary>
        /// The user IDs of incoming friend requests sent to this user.
        /// </summary>
        public virtual ISet<Guid>? IncomingFriendRequests { get; set; }

        #endregion

        #region Serialization

        private static readonly unsafe int GUID_SIZE = sizeof(Guid);

        private const int USER_ID_OFFSET = 0;
        private static readonly int USER_STATUS_OFFSET = USER_ID_OFFSET + GUID_SIZE;
        private static readonly int USERNAME_SIZE_OFFSET = USER_STATUS_OFFSET + sizeof(SphynxUserStatus);
        private static readonly int USERNAME_OFFSET = USERNAME_SIZE_OFFSET + sizeof(int);

        #endregion

        /// <summary>
        /// Creates a new <see cref="SphynxUserInfo"/>.
        /// </summary>
        /// <param name="userName">The username of the Sphynx user.</param>
        /// <param name="status">The activity status of the Sphynx user.</param>
        /// <param name="friends">User IDs of friends for this user.</param>
        /// <param name="rooms">Room IDs of chat rooms which this user is in (including DMs).</param>
        public SphynxUserInfo(string userName, SphynxUserStatus status, IEnumerable<Guid>? friends = null,
            IEnumerable<Guid>? rooms = null) : this(default, userName, status, friends, rooms)
        {
        }

        /// <summary>
        /// Creates a new <see cref="SphynxUserInfo"/>.
        /// </summary>
        /// <param name="userId">The user ID of the Sphynx user.</param>
        /// <param name="userName">The username of the Sphynx user.</param>
        /// <param name="status">The activity status of the Sphynx user.</param>
        /// <param name="friends">User IDs of friends for this user.</param>
        /// <param name="rooms">Room IDs of chat rooms which this user is in (including DMs).</param>
        public SphynxUserInfo(Guid userId, string userName, SphynxUserStatus status, IEnumerable<Guid>? friends = null,
            IEnumerable<Guid>? rooms = null)
        {
            UserId = userId;
            UserName = userName;
            UserStatus = status;
            Friends = CollectionUtils.CreateNullableSet(friends);
            Rooms = CollectionUtils.CreateNullableSet(rooms);
        }

        #region Serialization

        /// <summary>
        /// Attempts to deserialize this full user from the <paramref name="userBytes"/>.
        /// </summary>
        /// <param name="userBytes">The userBytes to read the user from.</param>
        /// <param name="userInfo">The deserialized user.</param>
        /// <returns>true if deserialization was successful; false otherwise.</returns>
        public static bool TryDeserialize(ReadOnlySpan<byte> userBytes, [NotNullWhen(true)] out SphynxUserInfo? userInfo)
        {
            return TryDeserialize(userBytes, false, out userInfo);
        }

        /// <inheritdoc cref="TryDeserialize(System.ReadOnlySpan{byte},out SphynxUserInfo?)"/>
        /// <param name="compactUser">Whether this user should be serialized as a compact user (i.e. a user other than
        /// the currently logged-in user).</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> userBytes, bool compactUser, [NotNullWhen(true)] out SphynxUserInfo? userInfo)
        {
            return TryDeserialize(userBytes, compactUser, out userInfo, out _);
        }

        /// <inheritdoc cref="TryDeserialize(System.ReadOnlySpan{byte},bool,out SphynxUserInfo?)"/>
        /// <param name="bytesRead">The number of bytes read from <paramref name="userBytes"/>.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> userBytes, bool compactUser, [NotNullWhen(true)] out SphynxUserInfo? userInfo,
            out int bytesRead)
        {
            try
            {
                bytesRead = ReadCompactInfo(userBytes, out var userId, out var userStatus, out string userName);

                if (compactUser)
                {
                    userInfo = new SphynxUserInfo(userId, userName, userStatus);
                    return true;
                }

                if (!TryReadFullInfo(userBytes[bytesRead..],
                        out var friends,
                        out var rooms,
                        out var pendingRoomMsgs,
                        out var outgoingFriendReqs,
                        out var incomingFriendReqs,
                        out int fullInfoSize))
                {
                    bytesRead += fullInfoSize;
                    userInfo = null;
                    return false;
                }

                userInfo = new SphynxUserInfo(userId, userName, userStatus, friends, rooms)
                {
                    PendingRoomMessages = pendingRoomMsgs,
                    OutgoingFriendRequests = outgoingFriendReqs,
                    IncomingFriendRequests = incomingFriendReqs,
                };
                return true;
            }
            catch
            {
                bytesRead = 0;
                userInfo = null;
                return false;
            }
        }

        private static int ReadCompactInfo(ReadOnlySpan<byte> compactInfoBytes, out Guid userId, out SphynxUserStatus userStatus, out string userName)
        {
            userId = new Guid(compactInfoBytes.Slice(USER_ID_OFFSET, GUID_SIZE));
            userStatus = (SphynxUserStatus)compactInfoBytes[USER_STATUS_OFFSET];
            int usernameSize = compactInfoBytes[USERNAME_SIZE_OFFSET..].ReadInt32();
            userName = SphynxPacket.TEXT_ENCODING.GetString(compactInfoBytes.Slice(USERNAME_OFFSET, usernameSize));

            int bytesRead = GetCompactSize(usernameSize);
            return bytesRead;
        }

        private static bool TryReadFullInfo(ReadOnlySpan<byte> restBytes,
            out ISet<Guid>? friends,
            out ISet<Guid>? rooms,
            out IList<PendingRoomMessageInfo>? pendingRoomMsgs,
            out ISet<Guid>? outgoingFriendReqs,
            out ISet<Guid>? incomingFriendReqs,
            out int bytesRead)
        {
            // Deserialize friend IDs
            bytesRead = restBytes.ReadGuidSet(out friends);

            // Deserialize joined rooms IDs
            bytesRead += restBytes[bytesRead..].ReadGuidSet(out rooms);

            // Deserialize pending msg info and error check
            if (!PendingRoomMessageInfo.TryReadRoomMessageInfoList(restBytes[bytesRead..], out pendingRoomMsgs, out int roomMsgInfoSize))
            {
                outgoingFriendReqs = null;
                incomingFriendReqs = null;
                return false;
            }

            bytesRead += roomMsgInfoSize;

            // Deserialize incoming and outgoing friend requests
            bytesRead += restBytes[bytesRead..].ReadGuidSet(out outgoingFriendReqs);
            bytesRead += restBytes[bytesRead..].ReadGuidSet(out incomingFriendReqs);

            return true;
        }

        /// <summary>
        /// Attempts to serialize this user into the <paramref name="buffer"/>.
        /// </summary>
        /// <param name="buffer">The buffer to serialize this user into.</param>
        /// <returns>true if serialization was successful; false otherwise.</returns>
        public bool TrySerialize(Span<byte> buffer) => TrySerialize(buffer, out _);

        /// <inheritdoc cref="TrySerialize(System.Span{byte})"/>
        /// <param name="bytesWritten">The number of bytes that were written to the buffer to.</param>
        public bool TrySerialize(Span<byte> buffer, out int bytesWritten) => TrySerialize(buffer, false, out bytesWritten);

        /// <inheritdoc cref="TrySerialize(System.Span{byte},out int)"/>
        /// <param name="compactUser">Whether this user should be serialized as a compact user (i.e. a user other than
        /// the currently logged-in user).</param>
        public virtual bool TrySerialize(Span<byte> buffer, bool compactUser, out int bytesWritten)
        {
            GetPacketInfo(compactUser, out int usernameSize, out int contentSize);

            if (buffer.Length < contentSize)
            {
                bytesWritten = 0;
                return false;
            }

            try
            {
                return (bytesWritten = SerializeContents(buffer, compactUser, usernameSize)) > 0;
            }
            catch
            {
                bytesWritten = 0;
                return false;
            }
        }

        /// <summary>
        /// Attempts to serialize this user asynchronously.
        /// </summary>
        /// <param name="stream">The stream to serialize this user into.</param>
        /// <param name="compactUser">Whether this user should be serialized as a compact user (i.e. a user other than
        /// the currently logged-in user).</param>
        /// <returns>A tuple, the first value indicating whether serialization was successful, and
        /// the second denoting the number of bytes that were written to the stream.</returns>
        public virtual async Task<(bool, int)> TrySerializeAsync(Stream stream, bool compactUser = false)
        {
            if (!stream.CanWrite) return (false, 0);

            GetPacketInfo(compactUser, out int usernameSize, out int contentSize);
            byte[] rawBuffer = ArrayPool<byte>.Shared.Rent(contentSize);
            var buffer = rawBuffer.AsMemory()[..contentSize];

            int bytesWritten = 0;

            try
            {
                bytesWritten = SerializeContents(buffer.Span, compactUser, usernameSize);
                await stream.WriteAsync(buffer);
                return (true, bytesWritten);
            }
            catch
            {
                return (false, bytesWritten);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rawBuffer);
            }
        }

        private int SerializeContents(Span<byte> buffer, bool compactUser, int usernameSize)
        {
            int bytesWritten = SerializeCompactInfo(buffer, usernameSize);

            if (!compactUser)
            {
                bytesWritten += SerializeFullInfo(buffer[bytesWritten..]);
            }

            return bytesWritten;
        }

        private int SerializeCompactInfo(Span<byte> buffer, int usernameSize)
        {
            UserId.TryWriteBytes(buffer.Slice(USER_ID_OFFSET, GUID_SIZE));
            buffer[USER_STATUS_OFFSET] = (byte)UserStatus;
            usernameSize.WriteBytes(buffer[USERNAME_SIZE_OFFSET..]);
            SphynxPacket.TEXT_ENCODING.GetBytes(UserName, buffer.Slice(USERNAME_OFFSET, usernameSize));

            return GetCompactSize(usernameSize);
        }

        private int SerializeFullInfo(Span<byte> buffer)
        {
            // Serialize friend IDs
            int bytesWritten = MemoryUtils.WriteGuidCollection(Friends, buffer);

            // Serialize room IDs
            bytesWritten += MemoryUtils.WriteGuidCollection(Rooms, buffer[bytesWritten..]);

            // Serialize pending msg info
            for (int i = 0; i < PendingRoomMessages?.Count; i++)
            {
                var roomMsgInfo = PendingRoomMessages[i];
                if (roomMsgInfo.TrySerialize(buffer[bytesWritten..]))
                {
                    bytesWritten += roomMsgInfo.ContentSize;
                }
            }

            // Serialize incoming and outgoing friend requests
            bytesWritten += MemoryUtils.WriteGuidCollection(OutgoingFriendRequests, buffer[bytesWritten..]);
            bytesWritten += MemoryUtils.WriteGuidCollection(IncomingFriendRequests, buffer[bytesWritten..]);

            return bytesWritten;
        }

        internal void GetPacketInfo(bool compactUser, out int usernameSize, out int contentSize)
        {
            usernameSize = string.IsNullOrEmpty(UserName) ? 0 : SphynxPacket.TEXT_ENCODING.GetByteCount(UserName);
            contentSize = GetCompactSize(usernameSize); // Compact user

            if (compactUser) return;

            contentSize += sizeof(int) + (Friends?.Count ?? 0) * GUID_SIZE + // Friends
                           sizeof(int) + (Rooms?.Count ?? 0) * GUID_SIZE; // Rooms

            // PendingRoomMessages
            contentSize += sizeof(int);
            for (int i = 0; i < PendingRoomMessages?.Count; i++)
            {
                contentSize += PendingRoomMessages[i].ContentSize;
            }

            contentSize += sizeof(int) + (OutgoingFriendRequests?.Count ?? 0) * GUID_SIZE + // OutgoingFriendRequests
                           sizeof(int) + (IncomingFriendRequests?.Count ?? 0) * GUID_SIZE; // IncomingFriendRequests
        }

        //   UserId            UserStatus                  UserName
        // GUID_SIZE + sizeof(SphynxUserStatus) + sizeof(int) + usernameSize; 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetCompactSize(int usernameSize) => GUID_SIZE + sizeof(SphynxUserStatus) + sizeof(int) + usernameSize;

        internal static int GetMinimumSize(bool compactUser = false)
        {
            int minContentSize = GetCompactSize(0); // Compact user
            if (compactUser)
            {
                return minContentSize;
            }

            minContentSize += sizeof(int) + sizeof(int) + sizeof(int) + // Friends, Rooms, PendingRoomMessages
                              2 * sizeof(int); // OutgoingFriendRequests, IncomingFriendRequests

            return minContentSize;
        }

        #endregion

        #region Interfaces

        /// <inheritdoc />
        public bool Equals(SphynxUserInfo? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return UserId.Equals(other.UserId);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((SphynxUserInfo)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode() => UserId.GetHashCode();

        #endregion
    }

    /// <summary>
    /// Represents a list of messages within a particular room that have yet to be read by the user.
    /// </summary>
    public class PendingRoomMessageInfo
    {
        /// <summary>
        /// The room ID to which these messages belong.
        /// </summary>
        public virtual Guid RoomId { get; set; }

        /// <summary>
        /// Message IDs of the messages which have yet to be read by the user.
        /// </summary>
        public virtual IList<Guid> PendingMessages { get; set; }

        // Serialization
        internal int ContentSize
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => PENDING_MSGS_OFFSET + PendingMessages.Count * GUID_SIZE;
        }

        private static readonly unsafe int GUID_SIZE = sizeof(Guid);

        private const int ROOM_ID_OFFSET = 0;
        private static readonly int PENDING_MSGS_COUNT_OFFSET = ROOM_ID_OFFSET + GUID_SIZE;
        private static readonly int PENDING_MSGS_OFFSET = PENDING_MSGS_COUNT_OFFSET + sizeof(int);

        /// <summary>
        /// Creates a new <see cref="PendingRoomMessageInfo"/>.
        /// </summary>
        /// <param name="roomId">The room ID to which these messages belong.</param>
        /// <param name="pendingMessages">Message IDs of the messages which have yet to be read by the user.</param>
        public PendingRoomMessageInfo(Guid roomId, IEnumerable<Guid> pendingMessages)
        {
            RoomId = roomId;
            PendingMessages = pendingMessages as List<Guid> ?? new List<Guid>(pendingMessages);
        }

        /// <summary>
        /// Creates a new <see cref="PendingRoomMessageInfo"/>.
        /// </summary>
        /// <param name="roomId">The room ID to which these messages belong.</param>
        /// <param name="pendingMessages">Message IDs of the messages which have yet to be read by the user.</param>
        public PendingRoomMessageInfo(Guid roomId, params Guid[] pendingMessages) : this(roomId, (IEnumerable<Guid>)pendingMessages)
        {
        }

        /// <summary>
        /// Creates a new <see cref="PendingRoomMessageInfo"/>.
        /// </summary>
        /// <param name="roomId">The room ID to which these messages belong.</param>
        /// <param name="pendingMessages">Message IDs of the messages which have yet to be read by the user.</param>
        public PendingRoomMessageInfo(Guid roomId, IList<Guid> pendingMessages)
        {
            RoomId = roomId;
            PendingMessages = pendingMessages;
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="PendingRoomMessageInfo"/>.
        /// </summary>
        /// <param name="bytes">The room message information, serialized as bytes.</param>
        /// <param name="pendingRoomMsgInfo">The deserialized information.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> bytes, [NotNullWhen(true)] out PendingRoomMessageInfo? pendingRoomMsgInfo)
        {
            return TryDeserialize(bytes, out pendingRoomMsgInfo, out _);
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="PendingRoomMessageInfo"/>.
        /// </summary>
        /// <param name="bytes">The room message information, serialized as bytes.</param>
        /// <param name="pendingRoomMsgInfo">The deserialized information.</param>
        /// <param name="bytesRead">The number of bytes that were read from <paramref name="bytes"/> in order to
        /// deserialize the <paramref name="pendingRoomMsgInfo"/>.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> bytes, [NotNullWhen(true)] out PendingRoomMessageInfo? pendingRoomMsgInfo,
            out int bytesRead)
        {
            try
            {
                var roomId = new Guid(bytes.Slice(ROOM_ID_OFFSET, GUID_SIZE));
                bytesRead = GUID_SIZE;
                bytesRead += bytes[PENDING_MSGS_OFFSET..].ReadGuidList(out var pendingMsgs);

                // pendingMsgs.Count should always be greater than 0 since this class is only instantiated when there are pending messages
                pendingRoomMsgInfo = new PendingRoomMessageInfo(roomId, pendingMsgs!);
                return true;
            }
            catch
            {
                pendingRoomMsgInfo = null;
                bytesRead = 0;
                return false;
            }
        }

        internal static bool TryReadRoomMessageInfoList(
            ReadOnlySpan<byte> countAndBytes,
            out IList<PendingRoomMessageInfo>? pendingRoomMsgs,
            out int bytesRead)
        {
            int pendingRoomMsgInfoCount = countAndBytes.ReadInt32();
            bytesRead = sizeof(int);

            if (pendingRoomMsgInfoCount > 0)
            {
                pendingRoomMsgs = new List<PendingRoomMessageInfo>(pendingRoomMsgInfoCount);

                for (int i = 0; i < pendingRoomMsgInfoCount; i++)
                {
                    if (!TryDeserialize(countAndBytes[bytesRead..], out var roomMsgs, out int roomBytesRead))
                    {
                        pendingRoomMsgs = null;
                        return false;
                    }

                    pendingRoomMsgs.Add(roomMsgs);
                    bytesRead += roomBytesRead;
                }
            }
            else
            {
                pendingRoomMsgs = null;
            }

            return true;
        }

        /// <summary>
        /// Attempts to serialize this <see cref="PendingRoomMessageInfo"/> into the <paramref name="buffer"/>.
        /// </summary>
        /// <param name="buffer">The buffer into which the message info should be serialized.</param>
        /// <returns>true if serialization was successful; false otherwise.</returns>
        public bool TrySerialize(Span<byte> buffer)
        {
            if (buffer.Length < ContentSize) return false;

            try
            {
                RoomId.TryWriteBytes(buffer[ROOM_ID_OFFSET..]);
                PendingMessages.WriteGuidCollection(buffer[PENDING_MSGS_COUNT_OFFSET..]);

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Attempts to asynchronously serialize this <see cref="PendingRoomMessageInfo"/> into the <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The buffer into which the message info should be serialized.</param>
        /// <returns>true if serialization was successful; false otherwise.</returns>
        public async Task<bool> TrySerializeAsync(Stream stream)
        {
            if (!stream.CanWrite) return false;

            byte[] rawBuffer = ArrayPool<byte>.Shared.Rent(ContentSize);
            var buffer = rawBuffer.AsMemory()[..ContentSize];

            // Serialize to buffer
            if (!TrySerialize(buffer.Span))
            {
                ArrayPool<byte>.Shared.Return(rawBuffer);
                return false;
            }

            try
            {
                await stream.WriteAsync(buffer);
                return true;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rawBuffer);
            }
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            int hashCode = 0;
            for (int i = 0; i < PendingMessages.Count; i++)
            {
                hashCode = HashCode.Combine(hashCode, PendingMessages[i].GetHashCode());
            }

            return HashCode.Combine(hashCode, RoomId);
        }
    }
}