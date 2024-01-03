using System.Diagnostics.CodeAnalysis;

namespace Sphynx.Packet.Broadcast
{
    /// <inheritdoc cref="SphynxPacketType.CHAT_JOIN_BCAST"/>
    public sealed class ChatLeaveBroadcastPacket : SphynxPacket, IEquatable<ChatLeaveBroadcastPacket>
    {
        /// <summary>
        /// Room ID of the room the user has left.
        /// </summary>
        public Guid RoomId { get; set; }

        /// <summary>
        /// The user ID of the user who left the room.
        /// </summary>
        public Guid LeaverId { get; set; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.CHAT_JOIN_BCAST;

        private const int ROOM_ID_OFFSET = 0;
        private const int LEAVER_ID_OFFSET = ROOM_ID_OFFSET + GUID_SIZE;

        /// <summary>
        /// Creates a new <see cref="ChatLeaveBroadcastPacket"/>.
        /// </summary>
        /// <param name="roomId">Room ID of the room the user has left.</param>
        /// <param name="leaverId">The user ID of the user who left the room.</param>
        public ChatLeaveBroadcastPacket(Guid roomId, Guid leaverId)
        {
            RoomId = roomId;
            LeaverId = leaverId;
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="ChatLeaveBroadcastPacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        /// <param name="packet">The deserialized packet.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out ChatLeaveBroadcastPacket? packet)
        {
            if (contents.Length >= LEAVER_ID_OFFSET + GUID_SIZE)
            {
                var roomId = new Guid(contents.Slice(ROOM_ID_OFFSET, GUID_SIZE));
                var leaverId = new Guid(contents.Slice(LEAVER_ID_OFFSET, GUID_SIZE));
                packet = new ChatLeaveBroadcastPacket(roomId, leaverId);
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
                packetSpan = packetSpan[SphynxPacketHeader.HEADER_SIZE..];
                RoomId.TryWriteBytes(packetSpan.Slice(ROOM_ID_OFFSET, GUID_SIZE));
                LeaverId.TryWriteBytes(packetSpan.Slice(LEAVER_ID_OFFSET, GUID_SIZE));
                return true;
            }

            packetBytes = null;
            return false;
        }

        /// <inheritdoc/>
        public bool Equals(ChatLeaveBroadcastPacket? other) => base.Equals(other) && RoomId == other?.RoomId && LeaverId == other?.LeaverId;
    }
}
