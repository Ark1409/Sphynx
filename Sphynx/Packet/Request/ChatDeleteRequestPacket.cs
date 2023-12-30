namespace Sphynx.Packet.Request
{
    /// <inheritdoc cref="SphynxPacketType.CHAT_DEL_REQ"/>
    public sealed class ChatDeleteRequestPacket : SphynxRequestPacket, IEquatable<ChatDeleteRequestPacket>
    {
        /// <summary>
        /// The ID of the room to delete.
        /// </summary>
        public Guid RoomId { get; set; }

        /// <summary>
        /// The password for the room to delete, if the room was guarded with a password. 
        /// This is a sort of confirmation to ensure the user understands the action they are about to perform.
        /// </summary>
        public string? Password { get; set; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType { get; }

        private const int ROOM_ID_OFFSET = 0;
        private const int ROOM_ID_SIZE = 16;
        private const int PASSWORD_OFFSET = ROOM_ID_OFFSET + ROOM_ID_SIZE;
        private const int PASSWORD_SIZE = 256;

        /// <summary>
        /// Creates a <see cref="ChatDeleteRequestPacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        public ChatDeleteRequestPacket(ReadOnlySpan<byte> contents)
        {
            RoomId = new Guid(contents.Slice(ROOM_ID_OFFSET, ROOM_ID_SIZE));

            // ---------------------------- //
            // TODO: Read password bytes    //
            // ---------------------------- //
        }

        /// <summary>
        /// Creates new <see cref="ChatDeleteRequestPacket"/>.
        /// </summary>
        /// <param name="roomId">The ID of the room to delete.</param>
        /// <param name="password">The password for the room to delete, if the room was guarded with a password.</param>
        public ChatDeleteRequestPacket(Guid roomId, string? password)
        {
            RoomId = roomId;
            Password = password;
        }

        /// <inheritdoc/>
        public override byte[] Serialize()
        {
            int contentSize = ROOM_ID_SIZE + PASSWORD_SIZE;

            byte[] serializedBytes = new byte[SphynxRequestHeader.HEADER_SIZE + contentSize];
            var serializationSpan = new Span<byte>(serializedBytes);

            SerializeHeader(serializationSpan.Slice(0, SphynxRequestHeader.HEADER_SIZE), contentSize);
            SerializeContents(serializationSpan.Slice(SphynxRequestHeader.HEADER_SIZE));

            return serializedBytes;
        }

        private void SerializeContents(Span<byte> buffer)
        {
            // TODO: Handle writing error
            RoomId.TryWriteBytes(buffer.Slice(PASSWORD_OFFSET, PASSWORD_SIZE));

            // -------------------------------- //
            // TODO: Serialize hashed password  //
            // -------------------------------- //
        }

        /// <inheritdoc/>
        public bool Equals(ChatDeleteRequestPacket? other) => RoomId == other?.RoomId && Password == other?.Password;
    }
}
