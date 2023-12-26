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
    }
}
