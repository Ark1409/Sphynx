namespace Sphynx.Packet
{
    /// <summary>
    /// Represents a response packet.
    /// </summary>
    public abstract class SphynxResponsePacket : SphynxPacket
    {
        /// <summary>
        /// Serializes a packet header into the specified <paramref name="buffer"/>.
        /// </summary>
        /// <param name="buffer">The buffer to serialize this header into.</param>
        /// <param name="contentSize">The <see cref="SphynxPacketHeader.ContentSize"/>.</param>
        protected virtual SphynxResponseHeader SerializeHeader(Span<byte> buffer, int contentSize)
        {
            var header = new SphynxResponseHeader(PacketType, contentSize);
            header.Serialize(buffer.Slice(0, SphynxResponseHeader.HEADER_SIZE));
            return header;
        }
    }
}