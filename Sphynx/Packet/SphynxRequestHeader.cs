namespace Sphynx.Packet
{
    /// <summary>
    /// The packet header for a request sent from a client to the server.
    /// </summary>
    public sealed class SphynxRequestHeader : SphynxPacketHeader
    {
        /// <summary>
        /// <see langword="sizeof"/>(<see cref="Guid"/>).
        /// </summary>
        private const int GUID_SIZE = 16;

        /// <summary>
        /// The user ID of the requesting user.
        /// </summary>
        public Guid UserId { get; }

        /// <summary>
        /// The session ID for the requesting user.
        /// </summary>
        public Guid SessionId { get; }

        /// <inheritdoc/>
        public override int Size => 42;

        /// <inheritdoc/>
        public override void Serialize(Span<byte> stream)
        {
            // Write packet sig
            SerializeSignature(stream);

            // Write packet type
            int PACKET_TYPE_OFFSET = SIGNATURE.Length;
            SerializePacketType(stream.Slice(PACKET_TYPE_OFFSET, sizeof(SphynxPacketType)), PacketType);

            // Write user and session IDs
            int USER_ID_OFFSET = PACKET_TYPE_OFFSET + sizeof(SphynxPacketType);
            int SESSION_ID_OFFSET = USER_ID_OFFSET + GUID_SIZE;

            // Prepare NOP packet on failure
            if (!UserId.TryWriteBytes(stream.Slice(USER_ID_OFFSET, GUID_SIZE)) ||
               !SessionId.TryWriteBytes(stream.Slice(SESSION_ID_OFFSET, GUID_SIZE)))
            {
                SerializePacketType(stream.Slice(PACKET_TYPE_OFFSET, sizeof(SphynxPacketType)), SphynxPacketType.NOP);
            }

            // Write packet content size
            int CONTENT_SIZE_OFFSET = SESSION_ID_OFFSET + GUID_SIZE;
            SerializeContentSize(stream.Slice(CONTENT_SIZE_OFFSET, sizeof(int)));
        }
    }
}
