using System.Diagnostics.CodeAnalysis;

using Sphynx.Utils;

namespace Sphynx.Packet.Request
{
    /// <inheritdoc cref="SphynxPacketType.MSG_REQ"/>.
    public sealed class MessageRequestPacket : SphynxRequestPacket, IEquatable<MessageRequestPacket>
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

        private const int RECIPIENT_TYPE_OFFSET = 0;
        private const int RECIPIENT_ID_OFFSET = RECIPIENT_TYPE_OFFSET + RECIPIENT_TYPE_SIZE;
        private const int MESSAGE_LENGTH_OFFSET = RECIPIENT_ID_OFFSET + GUID_SIZE;
        private const int MESSAGE_OFFSET = MESSAGE_LENGTH_OFFSET + sizeof(int);

        /// <summary>
        /// Creates a new <see cref="MessageRequestPacket"/>.
        /// </summary>
        /// <param name="recipientIsUser">Whether the recipient is a room or a single end-user.</param>
        /// <param name="recipientId">ID of recipient (whether it be a room or user).</param>
        /// <param name="message">The contents of the chat message.</param>
        public MessageRequestPacket(bool recipientIsUser, Guid recipientId, string message) : this(Guid.Empty, Guid.Empty, recipientIsUser, recipientId, message)
        {

        }

        /// <summary>
        /// Creates a new <see cref="MessageRequestPacket"/>.
        /// </summary>
        /// <param name="userId">The user ID of the requesting user.</param>
        /// <param name="sessionId">The session ID for the requesting user.</param>
        /// <param name="recipientIsUser">Whether the recipient is a room or a single end-user.</param>
        /// <param name="recipientId">ID of recipient (whether it be a room or user).</param>
        /// <param name="message">The contents of the chat message.</param>
        public MessageRequestPacket(Guid userId, Guid sessionId, bool recipientIsUser, Guid recipientId, string message) : base(userId, sessionId)
        {
            RecipientIsUser = recipientIsUser;
            RecipientId = recipientId;
            Message = message ?? throw new ArgumentNullException(nameof(message)); // Exceptions OK on client
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="MessageRequestPacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        /// <param name="packet">The deserialized packet.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> contents, out MessageRequestPacket? packet)
        {
            if (TryDeserialize(contents[..DEFAULT_CONTENT_SIZE], out var userId, out var sessionId) &&
                TryDeserializeContents(contents[DEFAULT_CONTENT_SIZE..], out packet))
            {
                packet.UserId = userId.Value;
                packet.SessionId = sessionId.Value;
                return true;
            }

            packet = null;
            return false;
        }

        private static bool TryDeserializeContents(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out MessageRequestPacket? packet)
        {
            // Avoid exceptions on server
            try
            {
                bool recipientIsUser = contents[RECIPIENT_TYPE_OFFSET] != 0;
                var recipientId = new Guid(contents.Slice(RECIPIENT_ID_OFFSET, GUID_SIZE));

                int messageLength = contents.Slice(MESSAGE_LENGTH_OFFSET, sizeof(int)).ReadInt32();
                string message = TEXT_ENCODING.GetString(contents.Slice(MESSAGE_OFFSET, messageLength));

                packet = new MessageRequestPacket(recipientIsUser, recipientId, message);
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
            int contentSize = DEFAULT_CONTENT_SIZE + RECIPIENT_TYPE_SIZE + GUID_SIZE + sizeof(int) + messageSize;

            packetBytes = new byte[SphynxPacketHeader.HEADER_SIZE + contentSize];
            var packetSpan = new Span<byte>(packetBytes);

            if (TrySerializeHeader(packetSpan[..SphynxPacketHeader.HEADER_SIZE], contentSize) &&
                TrySerialize(packetSpan.Slice(SphynxPacketHeader.HEADER_SIZE, DEFAULT_CONTENT_SIZE)))
            {
                SerializeContents(packetSpan[DEFAULT_CONTENT_SIZE..], messageSize);
                return true;
            }

            packetBytes = null;
            return false;
        }

        private void SerializeContents(Span<byte> buffer, int messageSize)
        {
            buffer[RECIPIENT_TYPE_OFFSET] = (byte)(RecipientIsUser ? 1 : 0);

            RecipientId.TryWriteBytes(buffer.Slice(RECIPIENT_ID_OFFSET, GUID_SIZE));

            messageSize.WriteBytes(buffer, MESSAGE_LENGTH_OFFSET);
            TEXT_ENCODING.GetBytes(Message, buffer.Slice(MESSAGE_OFFSET, messageSize));
        }

        /// <inheritdoc/>
        public bool Equals(MessageRequestPacket? other) => base.Equals(other) &&
            RecipientIsUser == other?.RecipientIsUser && RecipientId == other?.RecipientId && Message == other?.Message;
    }
}
