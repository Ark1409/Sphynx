using Sphynx.Packet.Response;

namespace Sphynx.Packet.Request
{
    /// <inheritdoc cref="SphynxPacketType.CHAT_INV_REQ"/>
    public sealed class ChatInviteRequestPacket : SphynxRequestPacket, IEquatable<ChatInviteRequestPacket>
    {
        /// <summary>
        /// ID of user to add to the chat room.
        /// </summary>
        public Guid UserId { get; set; }
        
        /// <summary>
        /// Room ID assigned to the newly created room.
        /// </summary>
        public Guid RoomId { get; set; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.CHAT_CREATE_RES;

        private const int ROOM_ID_OFFSET = 0;
        private const int ROOM_ID_SIZE = 16;

        /// <summary>
        /// Creates a <see cref="ChatInviteRequestPacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        public ChatInviteRequestPacket(ReadOnlySpan<byte> contents)
        {
            RoomId = new Guid(contents.Slice(ROOM_ID_OFFSET, ROOM_ID_SIZE));
        }

        /// <summary>
        /// Creates a new <see cref="ChatCreateResponsePacket"/>.
        /// </summary>
        /// <param name="roomId">Room ID assigned to the newly created room.</param>
        public ChatInviteRequestPacket(Guid roomId)
        {
            RoomId = roomId;
        }

        /// <inheritdoc/>
        public override byte[] Serialize()
        {
            int contentSize = ROOM_ID_SIZE;

            byte[] serializedBytes = new byte[SphynxResponseHeader.HEADER_SIZE + contentSize];
            var serialzationSpan = new Span<byte>(serializedBytes);

            SerializeHeader(serialzationSpan.Slice(0, SphynxResponseHeader.HEADER_SIZE), contentSize);
            SerializeContents(serialzationSpan.Slice(SphynxResponseHeader.HEADER_SIZE));

            return serializedBytes;
        }

        private void SerializeContents(Span<byte> buffer)
        {
            // TODO: Handle writing error
            RoomId.TryWriteBytes(buffer.Slice(ROOM_ID_OFFSET, ROOM_ID_SIZE));
        }

        /// <inheritdoc/>
        public bool Equals(ChatInviteRequestPacket? other) => UserId == other?.UserId && RoomId == other?.RoomId;
    }
}
