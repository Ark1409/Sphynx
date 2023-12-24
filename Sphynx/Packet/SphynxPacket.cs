namespace Sphynx.Packet
{
    /// <summary>
    /// Represents a packet sent between nodes on a Sphynx network.
    /// </summary>
    public abstract class SphynxPacket
    {
        /// <summary>
        /// The header for this packet.
        /// </summary>
        public SphynxPacketHeader Header { get; protected set; }

        /// <summary>
        /// Creates a <see cref="SphynxPacket"/>.
        /// </summary>
        /// <param name="header">The packet header.</param>
        public SphynxPacket(SphynxPacketHeader header)
        {
            Header = header ?? throw new ArgumentNullException(nameof(header));
        }

        /// <summary>
        /// Serializes this packet into a tightly-packed byte array.
        /// </summary>
        /// <returns>This packet serialized as a byte array.</returns>
        public abstract byte[] Serialize();

        /// <summary>
        /// Serializes this packet's contents into a stream of bytes.
        /// </summary>
        /// <param name="stream">The stream to serialize this header into.</param>
        public abstract void SerializeContents(Span<byte> stream);
    }
}
