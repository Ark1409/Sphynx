using Sphynx.Packet.Request;

using Sphynx.Utils;

namespace Sphynx.Packet.Broadcast
{
    /// <inheritdoc cref="SphynxPacketType.MSG_BCAST"/>
    public sealed class MessageBroadcastPacket : SphynxPacket
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
        /// User ID of sender.
        /// </summary>
        public Guid SenderId { get; set; }

        /// <summary>
        /// The contents of the chat message.
        /// </summary>
        public string Message { get; set; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.MSG_BCAST;

        private const int RECIPIENT_TYPE_SIZE = sizeof(bool);
        private const int RECIPIENT_TYPE_OFFSET = 0;

        /// <summary>
        /// Creates a new <see cref="MessageRequestPacket"/>, assuming the message is for a user.
        /// </summary>
        /// <param name="senderId">User ID of the sender.</param>
        /// <param name="message">The contents of the chat message.</param>
        public MessageBroadcastPacket(Guid senderId, string message) : this(true, Guid.Empty, senderId, message)
        {

        }

        /// <summary>
        /// Creates a new <see cref="MessageRequestPacket"/>.
        /// </summary>
        /// <param name="recipientIsUser">Whether the recipient is a room or a single end-user.</param>
        /// <param name="recipientId">ID of recipient (whether it be a room or user).</param>
        /// <param name="senderId">User ID of the sender.</param>
        /// <param name="message">The contents of the chat message.</param>
        public MessageBroadcastPacket(bool recipientIsUser, Guid recipientId, Guid senderId, string message)
        {
            RecipientIsUser = recipientIsUser;
            RecipientId = recipientId;
            SenderId = senderId;
            Message = message ?? string.Empty; // Avoid exceptions on server
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="MessageBroadcastPacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        /// <param name="packet">The deserialized packet.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> contents, out MessageBroadcastPacket? packet)
        {
            try
            {
                bool recipientIsUser = contents[RECIPIENT_TYPE_OFFSET] != 0;
                Guid recipientId = Guid.Empty;
                int offset = RECIPIENT_TYPE_OFFSET;

                if (!recipientIsUser)
                {
                    recipientId = new Guid(contents.Slice(offset, GUID_SIZE));
                    offset += GUID_SIZE;
                }

                var senderId = new Guid(contents.Slice(offset, GUID_SIZE));
                offset += GUID_SIZE;

                int messageLength = contents.ReadInt32(offset);
                offset += sizeof(int);

                string message = TEXT_ENCODING.GetString(contents.Slice(offset, messageLength));

                packet = recipientIsUser ? new MessageBroadcastPacket(senderId, message) :
                    new MessageBroadcastPacket(recipientIsUser, recipientId, senderId, message);

                return true;
            }
            catch
            {
                packet = null;
                return false;
            }
        }

        /// <inheritdoc/>
        public override bool TrySerialize(out byte[]? packetBytes)
        {
            int messageSize = TEXT_ENCODING.GetByteCount(Message);
            int contentSize;

            if (RecipientIsUser)
            {
                contentSize = RECIPIENT_TYPE_SIZE + GUID_SIZE + sizeof(int) + messageSize;
            }
            else
            {
                contentSize = RECIPIENT_TYPE_SIZE + GUID_SIZE + GUID_SIZE + sizeof(int) + messageSize;
            }

            packetBytes = new byte[SphynxPacketHeader.HEADER_SIZE + contentSize];
            var packetSpan = new Span<byte>(packetBytes);

            if (TrySerializeHeader(packetSpan[..SphynxPacketHeader.HEADER_SIZE], contentSize))
            {
                SerializeContents(packetSpan[SphynxPacketHeader.HEADER_SIZE..], messageSize);
                return true;
            }

            packetBytes = null;
            return false;
        }

        private void SerializeContents(Span<byte> buffer, int messageSize)
        {
            buffer[RECIPIENT_TYPE_OFFSET] = (byte)(RecipientIsUser ? 1 : 0);

            int offset = RECIPIENT_TYPE_OFFSET;

            if (!RecipientIsUser)
            {
                RecipientId.TryWriteBytes(buffer.Slice(offset, GUID_SIZE));
                offset += GUID_SIZE;
            }

            SenderId.TryWriteBytes(buffer.Slice(offset, GUID_SIZE));
            offset += GUID_SIZE;

            messageSize.WriteBytes(buffer, offset);
            offset += sizeof(int);

            TEXT_ENCODING.GetBytes(Message, buffer.Slice(offset, messageSize));
        }

        /// <inheritdoc/>
        public bool Equals(MessageRequestPacket? other) => base.Equals(other) &&
            RecipientIsUser == other?.RecipientIsUser && RecipientId == other?.RecipientId && Message == other?.Message;
    }
}
