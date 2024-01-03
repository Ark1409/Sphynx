using System.Diagnostics.CodeAnalysis;

namespace Sphynx.Packet.Broadcast
{
    /// <inheritdoc cref="SphynxPacketType.CHAT_DEL_BCAST"/>
    public sealed class ChatDeleteBroadcastPacket : SphynxPacket, IEquatable<ChatDeleteBroadcastPacket>
    {
        /// <summary>
        /// Room ID of the deleted room.
        /// </summary>
        public Guid RoomId { get; set; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.CHAT_DEL_BCAST;

        private const int ROOM_ID_OFFSET = 0;

        /// <summary>
        /// Creates a new <see cref="ChatDeleteBroadcastPacket"/>.
        /// </summary>
        /// <param name="roomId">Room ID of the deleted room.</param>
        public ChatDeleteBroadcastPacket(Guid roomId)
        {
            RoomId = roomId;
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="ChatDeleteBroadcastPacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        /// <param name="packet">The deserialized packet.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out ChatDeleteBroadcastPacket? packet)
        {
            if (contents.Length >= ROOM_ID_OFFSET + GUID_SIZE)
            {
                var roomId = new Guid(contents.Slice(ROOM_ID_OFFSET, GUID_SIZE));
                packet = new ChatDeleteBroadcastPacket(roomId);
                return true;
            }

            packet = null;
            return false;
        }

        /// <inheritdoc/>
        public override bool TrySerialize([NotNullWhen(true)] out byte[]? packetBytes)
        {
            int contentSize = ROOM_ID_OFFSET + GUID_SIZE;

            packetBytes = new byte[SphynxPacketHeader.HEADER_SIZE + contentSize];
            var packetSpan = new Span<byte>(packetBytes);

            if (TrySerializeHeader(packetSpan[..SphynxPacketHeader.HEADER_SIZE], contentSize))
            {
                packetSpan = packetSpan[SphynxPacketHeader.HEADER_SIZE..];
                RoomId.TryWriteBytes(packetSpan.Slice(ROOM_ID_OFFSET, GUID_SIZE));
                return true;
            }

            packetBytes = null;
            return false;
        }

        /// <inheritdoc/>
        public bool Equals(ChatDeleteBroadcastPacket? other) => base.Equals(other) && RoomId == other?.RoomId;
    }
}
