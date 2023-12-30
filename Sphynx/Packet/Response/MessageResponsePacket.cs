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
        public override SphynxPacketType PacketType => SphynxPacketType.MSG_REQ;

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
        public MessageResponsePacket(ReadOnlySpan<byte> contents)
        {
            RecipientIsUser = contents[RECIPIENT_TYPE_OFFSET] != 0;
            RecipientId = new Guid(contents.Slice(RECIPIENT_ID_OFFSET, RECIPIENT_ID_SIZE));

            int messageLength = contents.Slice(MESSAGE_LENGTH_OFFSET, sizeof(int)).ReadInt32();
            Message = TEXT_ENCODING.GetString(contents.Slice(MESSAGE_OFFSET, messageLength));
        }

        /// <summary>
        /// Creates a new <see cref="MessageResponsePacket"/>.
        /// </summary>
        /// <param name="recipientIsUser">Whether the recipient is a room or a single end-user. Used to protect against case
        /// where room has same id as user.</param>
        /// <param name="recipientId">ID of intended recipient (whether it be a room or user).</param>
        /// <param name="message">The contents of the chat message.</param>
        public MessageResponsePacket(bool recipientIsUser, Guid recipientId, string message)
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

            byte[] serializedBytes = new byte[SphynxResponseHeader.HEADER_SIZE + contentSize];
            var serializationSpan = new Span<byte>(serializedBytes);

            // Serialize contents first instead of header to prepare for NOP case
            if (SerializeContents(serializationSpan.Slice(SphynxResponseHeader.HEADER_SIZE), messageLength))
            {
                SerializeHeader(serializationSpan.Slice(0, SphynxResponseHeader.HEADER_SIZE), contentSize);
            }
            else
            {
                var header = new SphynxResponseHeader(SphynxPacketType.NOP, serializationSpan.Slice(SphynxResponseHeader.HEADER_SIZE).Length);
                header.Serialize(serializationSpan.Slice(0, SphynxResponseHeader.HEADER_SIZE));
            }

            return serializedBytes;
        }

        private bool SerializeContents(Span<byte> buffer, int messageLength)
        {
            buffer[RECIPIENT_TYPE_OFFSET] = (byte)(RecipientIsUser ? 1 : 0);

            // Prepare NOP on failure - good way to simply ignore message
            if (!RecipientId.TryWriteBytes(buffer.Slice(RECIPIENT_ID_OFFSET, RECIPIENT_ID_SIZE)))
            {
                return false;
            }

            messageLength.WriteBytes(buffer.Slice(MESSAGE_LENGTH_OFFSET, sizeof(int)));
            TEXT_ENCODING.GetBytes(Message, buffer.Slice(MESSAGE_OFFSET, messageLength));

            return true;
        }

        /// <inheritdoc/>
        public bool Equals(MessageResponsePacket? other) => RecipientIsUser == other?.RecipientIsUser && RecipientId == other?.RecipientId && Message == other?.Message;
    }
}
