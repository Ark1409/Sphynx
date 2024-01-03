using System.Diagnostics.CodeAnalysis;
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
        /// <see langword="sizeof"/>(<see cref="Guid"/>)
        /// </summary>
        protected const int GUID_SIZE = 16;

        /// <summary>
        /// Packet type for this packet.
        /// </summary>
        public abstract SphynxPacketType PacketType { get; }

        /// <summary>
        /// Attempts to serialize this packet into a tightly-packed byte array.
        /// </summary>
        /// <param name="packetBytes">This packet serialized as a byte array.</param>
        public abstract bool TrySerialize([NotNullWhen(true)] out byte[]? packetBytes);

        /// <summary>
        /// Serializes a packet header into the specified <paramref name="buffer"/>.
        /// </summary>
        /// <param name="buffer">The buffer to serialize this header into.</param>
        /// <param name="contentSize">The <see cref="SphynxPacketHeader.ContentSize"/>.</param>
        protected virtual bool TrySerializeHeader(Span<byte> buffer, int contentSize)
        {
            var header = new SphynxPacketHeader(PacketType, contentSize);
            return header.TrySerialize(buffer[..SphynxPacketHeader.HEADER_SIZE]);
        }

        /// <summary>
        /// Indicates whether the current packet has the same packet type as another packet.
        /// </summary>
        /// <param name="other">A packet to compare with this packet.</param>
        /// <returns>true if the current packet is equal to the other parameter; otherwise, false.</returns>
        protected virtual bool Equals(SphynxPacket? other) => PacketType == other?.PacketType;
    }
}
