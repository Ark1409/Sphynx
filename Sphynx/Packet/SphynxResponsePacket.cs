namespace Sphynx.Packet
{
    /// <summary>
    /// Represents a response packet.
    /// </summary>
    public abstract class SphynxResponsePacket : SphynxPacket
    {
        /// <summary>
        /// Serializes a packet header into the specified <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The stream to serialize this header into.</param>
        /// <param name="contentSize">The <see cref="SphynxPacketHeader.ContentSize"/>.</param>
        protected virtual SphynxResponseHeader SerializeHeader(Span<byte> stream, int contentSize)
        {
            var header = new SphynxResponseHeader(PacketType, contentSize);
            header.Serialize(stream.Slice(0, SphynxResponseHeader.HEADER_SIZE));
            return header;
        }
    }
}