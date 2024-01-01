using Sphynx.Utils;

namespace Sphynx.Packet.Response
{
    /// <inheritdoc cref="SphynxPacketType.MSG_RES"/>
    public sealed class MessageResponsePacket : SphynxResponsePacket, IEquatable<MessageResponsePacket>
    {
        /// <summary>
        /// Whether the recipient is a room or a single end-user. Used to protect against case
        /// where room has same id as user.
        /// </summary>
        public bool RecipientIsUser { get; set; }

        /// <summary>
        /// ID of intended recipient (whether it be a room or user).
        /// </summary>
        public Guid RecipientId { get; set; }

        /// <summary>
        /// The contents of the chat message.
        /// </summary>
        public string Message { get; set; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.MSG_RES;

        private const int RECIPIENT_TYPE_SIZE = sizeof(bool);
        private const int RECIPIENT_ID_SIZE = 16;

        private const int RECIPIENT_TYPE_OFFSET = 0;
        private const int RECIPIENT_ID_OFFSET = RECIPIENT_TYPE_OFFSET + RECIPIENT_TYPE_SIZE;
        private const int MESSAGE_LENGTH_OFFSET = RECIPIENT_ID_OFFSET + RECIPIENT_ID_SIZE;
        private const int MESSAGE_OFFSET = MESSAGE_LENGTH_OFFSET + sizeof(int);

        /// <summary>
        /// Creates a <see cref="MessageResponsePacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        public MessageResponsePacket(ReadOnlySpan<byte> contents) : base(SphynxErrorCode.FAILED_INIT)
        {
            RecipientIsUser = contents[RECIPIENT_TYPE_OFFSET] != 0;
            RecipientId = new Guid(contents.Slice(RECIPIENT_ID_OFFSET, RECIPIENT_ID_SIZE));

            int messageLength = contents.ReadInt32(MESSAGE_LENGTH_OFFSET);
            Message = TEXT_ENCODING.GetString(contents.Slice(MESSAGE_OFFSET, messageLength));
            ErrorCode = SphynxErrorCode.SUCCESS;
        }

        /// <summary>
        /// Creates a new <see cref="MessageResponsePacket"/>.
        /// </summary>
        /// <param name="recipientIsUser">Whether the recipient is a room or a single end-user. Used to protect against case
        /// where room has same id as user.</param>
        /// <param name="recipientId">ID of intended recipient (whether it be a room or user).</param>
        /// <param name="message">The contents of the chat message.</param>
        public MessageResponsePacket(bool recipientIsUser, Guid recipientId, string message) : base(SphynxErrorCode.SUCCESS)
        {
            RecipientIsUser = recipientIsUser;
            RecipientId = recipientId;
            Message = message;
        }

        /// <inheritdoc/>
        public override byte[] Serialize()
        {
            int messageLength = TEXT_ENCODING.GetByteCount(Message);
            int contentSize = RECIPIENT_TYPE_SIZE + RECIPIENT_ID_SIZE + sizeof(int) + messageLength;

            byte[] packetBytes = new byte[SphynxResponseHeader.HEADER_SIZE + contentSize];
            var packetSpan = new Span<byte>(packetBytes);

            SerializeHeader(packetSpan.Slice(0, SphynxResponseHeader.HEADER_SIZE), contentSize);
            SerializeContents(packetSpan.Slice(SphynxResponseHeader.HEADER_SIZE), messageLength);

            return packetBytes;
        }

        private void SerializeContents(Span<byte> buffer, int messageLength)
        {
            buffer[RECIPIENT_TYPE_OFFSET] = (byte)(RecipientIsUser ? 1 : 0);

            // Assume it writes; already performed length check
            RecipientId.TryWriteBytes(buffer.Slice(RECIPIENT_ID_OFFSET, RECIPIENT_ID_SIZE));

            messageLength.WriteBytes(buffer, MESSAGE_LENGTH_OFFSET);
            TEXT_ENCODING.GetBytes(Message, buffer.Slice(MESSAGE_OFFSET, messageLength));
        }

        /// <inheritdoc/>
        public override bool Equals(SphynxResponsePacket? other) => other is MessageResponsePacket res && Equals(res);

        /// <inheritdoc/>
        public bool Equals(MessageResponsePacket? other) =>
            base.Equals(other) && RecipientIsUser == other?.RecipientIsUser && RecipientId == other?.RecipientId && Message == other?.Message;
    }
}
