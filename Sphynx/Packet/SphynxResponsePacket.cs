namespace Sphynx.Packet
{
    /// <summary>
    /// Represents a response packet.
    /// </summary>
    public abstract class SphynxResponsePacket : SphynxPacket
    {
        /// <summary>
        /// Response header for this packet.
        /// </summary>
        public new SphynxResponseHeader Header { get; protected set; }

        /// <summary>
        /// Creates a new <see cref="SphynxResponsePacket"/>.
        /// </summary>
        /// <param name="header">The response packet header.</param>
        public SphynxResponsePacket(SphynxResponseHeader header) : base(header)
        {
            Header = header;
        }
    }
}
