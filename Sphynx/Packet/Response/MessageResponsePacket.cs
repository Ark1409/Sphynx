using Sphynx.Utils;

namespace Sphynx.Packet.Response
{
    /// <inheritdoc cref="SphynxPacketType.MSG_RES"/>
    public sealed class MessageResponsePacket : SphynxResponsePacket, IEquatable<MessageResponsePacket>
    {
        /// <summary>
        /// The room ID for the message.
        /// </summary>
        public Guid RooomId { get; set; }

        /// <summary>
        /// The contents of the chat message.
        /// </summary>
        public string Message { get; set; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.MSG_RES;

        private const int GUID_SIZE = 16;
        private const int ROOM_ID_OFFSET = 0;
        private const int MESSAGE_SIZE_OFFSET = ROOM_ID_OFFSET + GUID_SIZE;
        private const int MESSAGE_OFFSET = MESSAGE_SIZE_OFFSET + sizeof(int);

        /// <summary>
        /// Creates a <see cref="MessageResponsePacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        public MessageResponsePacket(ReadOnlySpan<byte> contents) : base(SphynxErrorCode.FAILED_INIT)
        {
            RooomId = new Guid(contents.Slice(ROOM_ID_OFFSET, GUID_SIZE));

            int messageSize = contents.ReadInt32(MESSAGE_SIZE_OFFSET);
            Message = TEXT_ENCODING.GetString(contents.Slice(MESSAGE_OFFSET, messageSize));
            ErrorCode = SphynxErrorCode.SUCCESS;
        }

        /// <summary>
        /// Creates a new <see cref="MessageResponsePacket"/>.
        /// </summary>
        /// <param name="roomId">The room ID for the message.</param>
        /// <param name="message">The contents of the chat message.</param>
        public MessageResponsePacket(Guid roomId, string message) : base(SphynxErrorCode.SUCCESS)
        {
            RooomId = roomId;
            Message = message;
        }

        /// <inheritdoc/>
        public override byte[] Serialize()
        {
            int messageSize = TEXT_ENCODING.GetByteCount(Message);
            int contentSize = GUID_SIZE + sizeof(int) + messageSize;

            byte[] packetBytes = new byte[SphynxResponseHeader.HEADER_SIZE + contentSize];
            var packetSpan = new Span<byte>(packetBytes);

            SerializeHeader(packetSpan.Slice(0, SphynxResponseHeader.HEADER_SIZE), contentSize);
            SerializeContents(packetSpan.Slice(SphynxResponseHeader.HEADER_SIZE), messageSize);

            return packetBytes;
        }

        private void SerializeContents(Span<byte> buffer, int messageSize)
        {
            // Assume it writes; already performed length check
            RooomId.TryWriteBytes(buffer.Slice(ROOM_ID_OFFSET, GUID_SIZE));

            messageSize.WriteBytes(buffer, MESSAGE_SIZE_OFFSET);
            TEXT_ENCODING.GetBytes(Message, buffer.Slice(MESSAGE_OFFSET, messageSize));
        }

        /// <inheritdoc/>
        public override bool Equals(SphynxResponsePacket? other) => other is MessageResponsePacket res && Equals(res);

        /// <inheritdoc/>
        public bool Equals(MessageResponsePacket? other) =>
            base.Equals(other) && RooomId == other?.RooomId && Message == other?.Message;
    }
}
