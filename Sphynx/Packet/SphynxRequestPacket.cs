namespace Sphynx.Packet
{
    /// <summary>
    /// Represents a request packet.
    /// </summary>
    public abstract class SphynxRequestPacket : SphynxPacket
    {
        /// <summary>
        /// Serializes a packet header into the specified <paramref name="buffer"/>.
        /// </summary>
        /// <param name="buffer">The buffer to serialize this header into.</param>
        /// <param name="contentSize">The <see cref="SphynxPacketHeader.ContentSize"/>.</param>
        protected virtual SphynxRequestHeader SerializeHeader(Span<byte> buffer, int contentSize)
        {
            var header = new SphynxRequestHeader(PacketType, contentSize);
            header.Serialize(buffer.Slice(0, SphynxRequestHeader.HEADER_SIZE));
            return header;
        }
    }
}