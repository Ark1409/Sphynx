namespace Sphynx.Packet
{
    /// <summary>
    /// Represents a request packet.
    /// </summary>
    public abstract class SphynxRequestPacket : SphynxPacket
    {
        /// <summary>
        /// Serializes a packet header into the specified <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The stream to serialize this header into.</param>
        /// <param name="contentSize">The <see cref="SphynxPacketHeader.ContentSize"/>.</param>
        protected virtual SphynxRequestHeader SerializeHeader(Span<byte> stream, int contentSize)
        {
            var header = new SphynxRequestHeader(PacketType, contentSize);
            header.Serialize(stream.Slice(0, SphynxRequestHeader.HEADER_SIZE));
            return header;
        }
    }
}