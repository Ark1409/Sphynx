using Sphynx.Utils;

namespace Sphynx.Packet.Request
{
    /// <inheritdoc cref="SphynxPacketType.MSG_REQ"/>.
    public sealed class MessageRequestPacket : SphynxRequestPacket, IEquatable<MessageRequestPacket>
    {
        /// <summary>
        /// The room ID for the message.
        /// </summary>
        public Guid RoomID { get; set; }

        /// <summary>
        /// The contents of the chat message.
        /// </summary>
        public string Message { get; set; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.MSG_REQ;

        private const int GUID_SIZE = 16;
        private const int ROOM_ID_OFFSET = 0;
        private const int MESSAGE_SIZE_OFFSET = ROOM_ID_OFFSET + GUID_SIZE;
        private const int MESSAGE_OFFSET = MESSAGE_SIZE_OFFSET + sizeof(int);

        /// <summary>
        /// Creates a <see cref="MessageRequestPacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        public MessageRequestPacket(ReadOnlySpan<byte> contents)
        {
            RoomID = new Guid(contents.Slice(ROOM_ID_OFFSET, GUID_SIZE));

            int messageLength = contents.ReadInt32(MESSAGE_SIZE_OFFSET);
            Message = TEXT_ENCODING.GetString(contents.Slice(MESSAGE_OFFSET, messageLength));
        }

        /// <summary>
        /// Creates a new <see cref="MessageRequestPacket"/>.
        /// </summary>
        /// <param name="roomId">The room ID for the message.</param>
        /// <param name="message">The contents of the chat message.</param>
        public MessageRequestPacket(Guid roomId, string message)
        {
            RoomID = roomId;
            Message = message;
        }

        /// <inheritdoc/>
        public override byte[] Serialize()
        {
            int messageSize = TEXT_ENCODING.GetByteCount(Message);
            int contentSize = GUID_SIZE + sizeof(int) + messageSize;

            byte[] packetBytes = new byte[SphynxRequestHeader.HEADER_SIZE + contentSize];
            var packetSpan = new Span<byte>(packetBytes);

            SerializeHeader(packetSpan.Slice(0, SphynxRequestHeader.HEADER_SIZE), contentSize);
            SerializeContents(packetSpan.Slice(SphynxRequestHeader.HEADER_SIZE), messageSize);

            return packetBytes;
        }

        private void SerializeContents(Span<byte> buffer, int messageSize)
        {
            // Assume it writes; already performed length check
            RoomID.TryWriteBytes(buffer.Slice(ROOM_ID_OFFSET, GUID_SIZE));

            messageSize.WriteBytes(buffer, MESSAGE_SIZE_OFFSET);
            TEXT_ENCODING.GetBytes(Message, buffer.Slice(MESSAGE_OFFSET, messageSize));
        }

        /// <inheritdoc/>
        public override bool Equals(SphynxRequestPacket? other) => other is MessageRequestPacket req && Equals(req);

        /// <inheritdoc/>
        public bool Equals(MessageRequestPacket? other) => base.Equals(other) &&
            RoomID == other?.RoomID && Message == other?.Message;
    }
}
