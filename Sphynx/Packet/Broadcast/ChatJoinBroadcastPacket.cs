using System.Diagnostics.CodeAnalysis;

namespace Sphynx.Packet.Broadcast
{
    /// <inheritdoc cref="SphynxPacketType.CHAT_JOIN_BCAST"/>
    public sealed class ChatJoinBroadcastPacket : SphynxPacket, IEquatable<ChatJoinBroadcastPacket>
    {
        /// <summary>
        /// Room ID of the room the user has joined.
        /// </summary>
        public Guid RoomId { get; set; }

        /// <summary>
        /// The user ID of the user who joined the room.
        /// </summary>
        public Guid JoinerId { get; set; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.CHAT_JOIN_BCAST;

        private const int ROOM_ID_OFFSET = 0;
        private const int JOINER_ID_OFFSET = ROOM_ID_OFFSET + GUID_SIZE;

        /// <summary>
        /// Creates a new <see cref="ChatJoinBroadcastPacket"/>.
        /// </summary>
        /// <param name="roomId">Room ID of the room the user has joined.</param>
        /// <param name="joinerId">The user ID of the user who joined the room.</param>
        public ChatJoinBroadcastPacket(Guid roomId, Guid joinerId)
        {
            RoomId = roomId;
            JoinerId = joinerId;
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="ChatJoinBroadcastPacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        /// <param name="packet">The deserialized packet.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out ChatJoinBroadcastPacket? packet)
        {
            if (contents.Length >= JOINER_ID_OFFSET + GUID_SIZE)
            {
                var roomId = new Guid(contents.Slice(ROOM_ID_OFFSET, GUID_SIZE));
                var joinerId = new Guid(contents.Slice(JOINER_ID_OFFSET, GUID_SIZE));
                packet = new ChatJoinBroadcastPacket(roomId, joinerId);
                return true;
            }

            packet = null;
            return false;
        }

        /// <inheritdoc/>
        public override bool TrySerialize([NotNullWhen(true)] out byte[]? packetBytes)
        {
            int contentSize = GUID_SIZE + GUID_SIZE;

            packetBytes = new byte[SphynxPacketHeader.HEADER_SIZE + contentSize];
            var packetSpan = new Span<byte>(packetBytes);

            if (TrySerializeHeader(packetSpan[..SphynxPacketHeader.HEADER_SIZE], contentSize))
            {
                RoomId.TryWriteBytes(packetSpan.Slice(ROOM_ID_OFFSET, GUID_SIZE));
                JoinerId.TryWriteBytes(packetSpan.Slice(JOINER_ID_OFFSET, GUID_SIZE));
                return true;
            }

            packetBytes = null;
            return false;
        }

        /// <inheritdoc/>
        public bool Equals(ChatJoinBroadcastPacket? other) => base.Equals(other) && RoomId == other?.RoomId && JoinerId == other?.JoinerId;
    }
}
