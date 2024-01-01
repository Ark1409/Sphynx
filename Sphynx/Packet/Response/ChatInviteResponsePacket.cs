namespace Sphynx.Packet.Response
{
    /// <inheritdoc cref="SphynxPacketType.CHAT_INV_RES"/>
    public sealed class ChatInviteResponsePacket : SphynxResponsePacket, IEquatable<ChatInviteResponsePacket>
    {
        /// <summary>
        /// The ID of the requesting user.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// The ID of the room to which the requesting user performed the invitation to.
        /// </summary>
        public Guid RoomId { get; set; }

        private const int GUID_SIZE = 16;

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.LOGIN_RES;

        /// <summary>
        /// Creates a <see cref="ChatInviteResponsePacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        public ChatInviteResponsePacket(ReadOnlySpan<byte> contents) : base(SphynxErrorCode.FAILED_INIT)
        {
        }

        /// <summary>
        /// Creates a new <see cref="ChatDeleteResponsePacket"/>.
        /// </summary>
        /// <param name="errorCode">Error code for chat invitation..</param>
        /// <param name="errorMessage">Error message for <paramref name="errorCode"/>.</param>
        public ChatInviteResponsePacket(SphynxErrorCode errorCode, string errorMessage) : base(SphynxErrorCode.FAILED_INIT)
        {

        }

        /// <inheritdoc/>
        public override byte[] Serialize()
        {
            const int CONTENT_SIZE = GUID_SIZE + GUID_SIZE;

            byte[] packetBytes = new byte[SphynxResponseHeader.HEADER_SIZE + CONTENT_SIZE];
            var packetSpan = new Span<byte>(packetBytes);

            SerializeHeader(packetSpan.Slice(0, SphynxResponseHeader.HEADER_SIZE), CONTENT_SIZE);
            SerializeContents(packetSpan.Slice(SphynxResponseHeader.HEADER_SIZE));

            return packetBytes;
        }

        private void SerializeContents(Span<byte> buffer)
        {
            // Assume it writes; already performed length check
            UserId.TryWriteBytes(buffer);
            RoomId.TryWriteBytes(buffer);
        }

        /// <inheritdoc/>
        public bool Equals(ChatInviteResponsePacket? other) => base.Equals(other) && UserId == other?.UserId && RoomId == other?.RoomId;
    }
}
