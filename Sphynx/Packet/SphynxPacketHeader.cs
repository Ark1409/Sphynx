using System.Diagnostics.CodeAnalysis;

using Sphynx.Utils;

namespace Sphynx.Packet
{
    /// <summary>
    /// Represents the header of a <see cref="SphynxPacket"/>.
    /// </summary>
    public sealed class SphynxPacketHeader : IEquatable<SphynxPacketHeader>
    {
        /// <summary>
        /// The packet signature to safe-guards against corrupted packets.
        /// </summary>
        public const ushort SIGNATURE = 0x5350;

        /// <summary>
        /// The size of this particular header in bytes.
        /// </summary>
        public const short HEADER_SIZE = 10;

        /// <summary>
        /// The type of this packet.
        /// </summary>
        public SphynxPacketType PacketType { get; set; }

        /// <summary>
        /// The size of the content in this packet in bytes. 
        /// </summary>
        public int ContentSize { get; set; }

        private const int SIGNATURE_OFFSET = 0;
        private const int PACKET_TYPE_OFFSET = SIGNATURE_OFFSET + sizeof(ushort);
        private const int CONTENT_SIZE_OFFSET = PACKET_TYPE_OFFSET + sizeof(SphynxPacketType);

        /// <summary>
        /// Creates a new <see cref="SphynxPacketHeader"/>.
        /// </summary>
        /// <param name="packetType">The type of packet.</param>
        /// <param name="contentSize">The size of the packet's contents.</param>
        public SphynxPacketHeader(SphynxPacketType packetType, int contentSize)
        {
            PacketType = packetType;
            ContentSize = contentSize;
        }

        /// <summary>
        /// Creates a new <see cref="SphynxPacketHeader"/> from the <paramref name="packetHeader"/>.
        /// </summary>
        /// <param name="packetHeader">The raw bytes for a <see cref="SphynxPacketHeader"/>.</param>
        /// <param name="header">The deserialized header.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> packetHeader, [NotNullWhen(true)] out SphynxPacketHeader? header)
        {
            if (SIGNATURE == packetHeader.ReadUInt16(SIGNATURE_OFFSET) && packetHeader.Length >= HEADER_SIZE)
            {
                var packetType = (SphynxPacketType)packetHeader.ReadUInt32(PACKET_TYPE_OFFSET);
                int contentSize = packetHeader.ReadInt32(CONTENT_SIZE_OFFSET);

                header = new SphynxPacketHeader(packetType, contentSize);
                return true;
            }

            header = null;
            return false;
        }

        /// <summary>
        /// Serializes this packet header into a tightly-packed byte array.
        /// </summary>
        /// <returns>This packet header serialized as a byte array.</returns>
        public byte[] Serialize()
        {
            byte[] bytes = new byte[HEADER_SIZE];
            TrySerialize(bytes);
            return bytes;
        }

        /// <summary>
        /// Attempts to serializes this header into a buffer of bytes.
        /// </summary>
        /// <param name="buffer">The buffer to serialize this header into.</param>
        public bool TrySerialize(Span<byte> buffer)
        {
            if (buffer.Length >= HEADER_SIZE)
            {
                SIGNATURE.WriteBytes(buffer, SIGNATURE_OFFSET);
                ((uint)PacketType).WriteBytes(buffer, PACKET_TYPE_OFFSET);
                ContentSize.WriteBytes(buffer, CONTENT_SIZE_OFFSET);
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public bool Equals(SphynxPacketHeader? other) => PacketType == other?.PacketType && ContentSize == other?.ContentSize;
    }
}
