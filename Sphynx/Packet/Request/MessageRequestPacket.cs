using System.Runtime.InteropServices;

namespace Sphynx.Packet.Request
{
    /// <inheritdoc cref="SphynxPacketType.MSG_REQ"/>.
    public sealed class MessageRequestPacket : SphynxRequestPacket
    {
        /// <summary>
        /// Whether the recipient is a room or a single end-user.
        /// </summary>
        public bool RecipientIsUser { get; set; }

        /// <summary>
        /// ID of recipient (whether it be a room or user).
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
        /// Creates a <see cref="MessageRequestPacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        public MessageRequestPacket(ReadOnlySpan<byte> contents)
        {
            RecipientIsUser = contents[RECIPIENT_TYPE_OFFSET] != 0;
            RecipientId = new Guid(contents.Slice(RECIPIENT_ID_OFFSET, RECIPIENT_ID_SIZE));

            int messageLength = MemoryMarshal.Cast<byte, int>(contents.Slice(MESSAGE_LENGTH_OFFSET, sizeof(int)))[0];
            Message = TEXT_ENCODING.GetString(contents.Slice(MESSAGE_OFFSET, messageLength));
        }

        /// <summary>
        /// Creates a new <see cref="MessageRequestPacket"/>.
        /// </summary>
        /// <param name="recipientIsUser">Whether the recipient is a room or a single end-user.</param>
        /// <param name="recipientId">ID of recipient (whether it be a room or user).</param>
        /// <param name="message">The contents of the chat message.</param>
        public MessageRequestPacket(bool recipientIsUser, Guid recipientId, string message)
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

            byte[] serializedBytes = new byte[SphynxRequestHeader.HEADER_SIZE + contentSize];
            var serializationSpan = new Span<byte>(serializedBytes);

            // Serialize contents first instead of header to prepare for NOP case
            if (SerializeContents(serializationSpan.Slice(SphynxRequestHeader.HEADER_SIZE), messageLength))
            {
                SerializeHeader(serializationSpan.Slice(0, SphynxRequestHeader.HEADER_SIZE), contentSize);
            }
            else
            {
                var header = new SphynxRequestHeader(SphynxPacketType.NOP, serializationSpan.Slice(SphynxRequestHeader.HEADER_SIZE).Length);
                header.Serialize(serializationSpan.Slice(0, SphynxRequestHeader.HEADER_SIZE));
            }

            return serializedBytes;
        }

        private bool SerializeContents(Span<byte> stream, int messageLength)
        {
            stream[RECIPIENT_TYPE_OFFSET] = (byte)(RecipientIsUser ? 1 : 0);

            // Prepare NOP on failure - good way to simply ignore message
            if (!RecipientId.TryWriteBytes(stream.Slice(RECIPIENT_ID_OFFSET, RECIPIENT_ID_SIZE)))
            {
                return false;
            }

            Span<byte> messageLengthBytes = MemoryMarshal.Cast<int, byte>(stackalloc int[] { messageLength });
            messageLengthBytes.CopyTo(stream.Slice(MESSAGE_LENGTH_OFFSET, sizeof(int)));

            TEXT_ENCODING.GetBytes(Message, stream.Slice(MESSAGE_OFFSET, messageLength));
            return true;
        }
    }
}
