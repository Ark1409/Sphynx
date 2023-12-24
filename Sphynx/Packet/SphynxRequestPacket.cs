namespace Sphynx.Packet
{
    /// <summary>
    /// Represents a request packet.
    /// </summary>
    public abstract class SphynxRequestPacket : SphynxPacket
    {
        /// <summary>
        /// Request header for this packet.
        /// </summary>
        public new SphynxRequestHeader Header { get; protected set; }

        /// <summary>
        /// Creates a new <see cref="SphynxRequestPacket"/>.
        /// </summary>
        /// <param name="header">The request packet header.</param>
        public SphynxRequestPacket(SphynxRequestHeader header) : base(header)
        {
            Header = header;
        }
    }
}
