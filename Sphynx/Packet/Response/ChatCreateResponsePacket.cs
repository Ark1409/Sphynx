namespace Sphynx.Packet.Response
{
    /// <inheritdoc cref="SphynxPacketType.CHAT_CREATE_RES"/>
    public sealed class ChatCreateResponsePacket : SphynxResponsePacket, IEquatable<ChatCreateResponsePacket>
    {
        /// <summary>
        /// Room ID assigned to the newly created room.
        /// </summary>
        public Guid RoomId { get; set; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.CHAT_CREATE_RES;

        private const int ROOM_ID_OFFSET = 0;
        private const int ROOM_ID_SIZE = 16;

        /// <summary>
        /// Creates a <see cref="ChatCreateResponsePacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        public ChatCreateResponsePacket(ReadOnlySpan<byte> contents) : base(SphynxErrorCode.FAILED_INIT)
        {
            RoomId = new Guid(contents.Slice(ROOM_ID_OFFSET, ROOM_ID_SIZE));
            ErrorCode = SphynxErrorCode.SUCCESS;
        }

        /// <summary>
        /// Creates a new <see cref="ChatCreateResponsePacket"/>.
        /// </summary>
        /// <param name="roomId">Room ID assigned to the newly created room.</param>
        public ChatCreateResponsePacket(Guid roomId) : base(SphynxErrorCode.SUCCESS)
        {
            RoomId = roomId;
        }

        /// <inheritdoc/>
        public override byte[] Serialize()
        {
            int contentSize = ROOM_ID_SIZE;

            byte[] packetBytes = new byte[SphynxResponseHeader.HEADER_SIZE + contentSize];
            var packetSpan = new Span<byte>(packetBytes);

            SerializeHeader(packetSpan.Slice(0, SphynxResponseHeader.HEADER_SIZE), contentSize);
            SerializeContents(packetSpan.Slice(SphynxResponseHeader.HEADER_SIZE));

            return packetBytes;
        }

        private void SerializeContents(Span<byte> buffer)
        {
            // Assume it writes; already performed length check
            RoomId.TryWriteBytes(buffer.Slice(ROOM_ID_OFFSET, ROOM_ID_SIZE));
        }

        /// <inheritdoc/>
        public bool Equals(ChatCreateResponsePacket? other) => RoomId == other?.RoomId;
    }
}
