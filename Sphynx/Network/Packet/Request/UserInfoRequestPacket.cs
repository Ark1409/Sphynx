using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Sphynx.Utils;

namespace Sphynx.Network.Packet.Request
{
    /// <inheritdoc cref="SphynxPacketType.USER_INFO_REQ"/>
    public sealed class UserInfoRequestPacket : SphynxRequestPacket, IEquatable<UserInfoRequestPacket>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.LOGOUT_REQ;

        /// <summary>
        /// The user IDs of the users for which to retrieve information. Each ID should be distinct.
        /// </summary>
        /// <remarks>Each user ID should be distinct.</remarks>
        public Guid[] UserIds { get; set; }

        private static readonly int USER_COUNT_OFFSET = DEFAULT_CONTENT_SIZE;
        private static readonly int USER_IDS_OFFSET = USER_COUNT_OFFSET + sizeof(int);

        /// <summary>
        /// Creates a new <see cref="UserInfoRequestPacket"/>.
        /// </summary>
        /// <param name="userId">The user ID of the requesting user.</param>
        /// <param name="sessionId">The session ID for the requesting user.</param>
        public UserInfoRequestPacket(Guid userId, Guid sessionId) : base(userId, sessionId)
        {
        }

        /// <summary>
        /// Creates a new <see cref="UserInfoRequestPacket"/>.
        /// </summary>
        /// <param name="userId">The user ID of the requesting user.</param>
        /// <param name="sessionId">The session ID for the requesting user.</param>
        /// <param name="userIds">The user IDs of the users for which to retrieve information.</param>
        public UserInfoRequestPacket(Guid userId, Guid sessionId, params Guid[] userIds) : base(userId, sessionId)
        {
            UserIds = userIds;
        }

        /// <summary>
        /// Creates a new <see cref="UserInfoRequestPacket"/>.
        /// </summary>
        /// <param name="userId">The user ID of the requesting user.</param>
        /// <param name="sessionId">The session ID for the requesting user.</param>
        /// <param name="userIds">The user IDs of the users for which to retrieve information.</param>
        public UserInfoRequestPacket(Guid userId, Guid sessionId, IEnumerable<Guid> userIds) : this(userId, sessionId,
            userIds as Guid[] ?? userIds.ToArray())
        {
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="UserInfoRequestPacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        /// <param name="packet">The deserialized packet.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out UserInfoRequestPacket? packet)
        {
            if (!TryDeserializeDefaults(contents, out var userId, out var sessionId))
            {
                packet = null;
                return false;
            }

            try
            {
                int userCount = contents[USER_COUNT_OFFSET..].ReadInt32();
                var userIds = new Guid[userCount];

                for (int i = 0; i < userCount; i++)
                {
                    userIds[i] = new Guid(contents.Slice(USER_IDS_OFFSET + i * GUID_SIZE, GUID_SIZE));
                }

                packet = new UserInfoRequestPacket(userId.Value, sessionId.Value, userIds);
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
            int contentSize = DEFAULT_CONTENT_SIZE + sizeof(int) + GUID_SIZE * UserIds.Length; // userCount, userIds
            int bufferSize = SphynxPacketHeader.HEADER_SIZE + contentSize;

            if (!TrySerialize(packetBytes = new byte[bufferSize]))
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

            int contentSize = DEFAULT_CONTENT_SIZE + sizeof(int) + GUID_SIZE * UserIds.Length; // userCount, userIds

            int bufferSize = SphynxPacketHeader.HEADER_SIZE + contentSize;
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

            UserIds.Length.WriteBytes(buffer[USER_COUNT_OFFSET..]);
            for (int i = 0; i < UserIds.Length; i++)
            {
                Debug.Assert(UserIds[i].TryWriteBytes(buffer.Slice(USER_IDS_OFFSET + i * GUID_SIZE, GUID_SIZE)));
            }

            return true;
        }

        /// <inheritdoc/>
        public bool Equals(UserInfoRequestPacket? other) => base.Equals(other) && MemoryUtils.SequenceEqual(UserIds, other?.UserIds);
    }
}