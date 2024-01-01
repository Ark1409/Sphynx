using System.Text;

namespace Sphynx.Packet
{
    /// <summary>
    /// Represents a packet sent between nodes on a Sphynx network.
    /// </summary>
    public abstract class SphynxPacket
    {
        /// <summary>
        /// Encoding used for text.
        /// </summary>
        public static readonly Encoding TEXT_ENCODING = Encoding.UTF8;

        /// <summary>
        /// Packet type for this packet.
        /// </summary>
        public abstract SphynxPacketType PacketType { get; }

        /// <summary>
        /// Serializes this packet into a tightly-packed byte array.
        /// </summary>
        /// <returns>This packet serialized as a byte array.</returns>
        public abstract byte[] Serialize();

        /// <summary>
        /// Serializes a packet header into the specified <paramref name="buffer"/>.
        /// </summary>
        /// <param name="buffer">The buffer to serialize this header into.</param>
        /// <param name="contentSize">The <see cref="SphynxPacketHeader.ContentSize"/>.</param>
        protected abstract SphynxPacketHeader SerializeHeader(Span<byte> buffer, int contentSize);
    }
}
