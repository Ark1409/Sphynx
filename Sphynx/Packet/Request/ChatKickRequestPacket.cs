using System.Diagnostics.CodeAnalysis;

namespace Sphynx.Packet.Request
{
    /// <inheritdoc cref="SphynxPacketType.CHAT_KICK_REQ"/>
    public sealed class ChatKickRequestPacket : SphynxRequestPacket, IEquatable<ChatKickRequestPacket>
    {
        /// <summary>
        /// Room ID of the room to kick the user from.
        /// </summary>
        public Guid RoomId { get; set; }

        /// <summary>
        /// User ID of the user to kick from the room.
        /// </summary>
        public Guid KickId { get; set; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.CHAT_KICK_REQ;

        private const int ROOM_ID_OFFSET = DEFAULT_CONTENT_SIZE;
        private const int KICK_ID_OFFSET = ROOM_ID_OFFSET + GUID_SIZE;

        /// <summary>
        /// Creates a new <see cref="ChatKickRequestPacket"/>.
        /// </summary>
        /// <param name="roomId">Room ID of the room to kick the user from.</param>
        /// <param name="kickId">User ID of the user to kick from the room.</param>
        public ChatKickRequestPacket(Guid roomId, Guid kickId) : this(Guid.Empty, Guid.Empty, roomId, kickId)
        {
        }

        /// <summary>
        /// Creates a new <see cref="ChatLeaveRequestPacket"/>.
        /// </summary>
        /// <param name="userId">The user ID of the requesting user.</param>
        /// <param name="sessionId">The session ID for the requesting user.</param>
        /// <param name="roomId">Room ID of the room to leave.</param>
        /// <param name="kickId">User ID of the user to kick from the room.</param>
        public ChatKickRequestPacket(Guid userId, Guid sessionId, Guid roomId, Guid kickId) : base(userId, sessionId)
        {
            RoomId = roomId;
            KickId = kickId;
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="ChatKickRequestPacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        /// <param name="packet">The deserialized packet.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out ChatKickRequestPacket? packet)
        {
            if (contents.Length < KICK_ID_OFFSET + GUID_SIZE || !TryDeserialize(contents, out var userId, out var sessionId))
            {
                packet = null;
                return false;
            }

            var roomId = new Guid(contents.Slice(ROOM_ID_OFFSET, GUID_SIZE));
            var kickId = new Guid(contents.Slice(KICK_ID_OFFSET, GUID_SIZE));
            packet = new ChatKickRequestPacket(userId.Value, sessionId.Value, roomId, kickId);
            return true;
        }

        /// <inheritdoc/>
        public override bool TrySerialize([NotNullWhen(true)] out byte[]? packetBytes)
        {
            int contentSize = DEFAULT_CONTENT_SIZE + GUID_SIZE + GUID_SIZE;

            packetBytes = new byte[SphynxPacketHeader.HEADER_SIZE + contentSize];
            var packetSpan = new Span<byte>(packetBytes);

            if (TrySerializeHeader(packetSpan[..SphynxPacketHeader.HEADER_SIZE], contentSize) &&
                TrySerialize(packetSpan = packetSpan[SphynxPacketHeader.HEADER_SIZE..]))
            {
                RoomId.TryWriteBytes(packetSpan.Slice(ROOM_ID_OFFSET, GUID_SIZE));
                KickId.TryWriteBytes(packetSpan.Slice(KICK_ID_OFFSET, GUID_SIZE));
                return true;
            }

            packetBytes = null;
            return false;
        }

        /// <inheritdoc/>
        public bool Equals(ChatKickRequestPacket? other) => base.Equals(other) && RoomId == other?.RoomId && KickId == other?.KickId;
    }
}
