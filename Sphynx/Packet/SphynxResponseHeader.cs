namespace Sphynx.Packet
{
    /// <summary>
    /// The packet header for a request sent from the server to a client.
    /// </summary>
    public sealed class SphynxResponseHeader : SphynxPacketHeader
    {
        /// <inheritdoc/>
        public override int Size => 10;

        /// <inheritdoc/>
        public override void Serialize(Span<byte> stream)
        {
            // Write packet sig
            SerializeSignature(stream);

            // Write packet type
            int PACKET_TYPE_OFFSET = SIGNATURE.Length;
            SerializePacketType(stream.Slice(PACKET_TYPE_OFFSET, sizeof(SphynxPacketType)), PacketType);

            // Write packet content size
            int CONTENT_SIZE_OFFSET = PACKET_TYPE_OFFSET + sizeof(SphynxPacketType);
            SerializeContentSize(stream.Slice(CONTENT_SIZE_OFFSET, sizeof(int)));
        }
    }
}
