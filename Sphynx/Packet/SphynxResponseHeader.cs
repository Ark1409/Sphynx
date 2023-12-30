using Sphynx.Utils;

namespace Sphynx.Packet
{
    /// <summary>
    /// The packet header for a request sent from the server to a client.
    /// </summary>
    public sealed class SphynxResponseHeader : SphynxPacketHeader, IEquatable<SphynxResponseHeader>
    {
        /// <summary>
        /// The size of a response header in bytes.
        /// </summary>
        public const int HEADER_SIZE = 10;

        private const int SIGNATURE_OFFSET = 0;
        private const int PACKET_TYPE_OFFSET = sizeof(ushort);
        private const int CONTENT_SIZE_OFFSET = PACKET_TYPE_OFFSET + sizeof(SphynxPacketType);

        /// <inheritdoc/>
        public override int HeaderSize => HEADER_SIZE;

        /// <summary>
        /// Creates a new <see cref="SphynxResponseHeader"/> from raw packet bytes.
        /// </summary>
        /// <param name="packet">The raw packet bytes.</param>
        public SphynxResponseHeader(byte[] packet) : this(new ReadOnlySpan<byte>(packet))
        {

        }

        /// <summary>
        /// Creates a new <see cref="SphynxResponseHeader"/> from raw packet bytes.
        /// </summary>
        /// <param name="packet">The raw packet bytes.</param>
        public SphynxResponseHeader(ReadOnlySpan<byte> packet) : base(SphynxPacketType.NOP, packet.Length)
        {
            if (packet.Length != HEADER_SIZE)
                throw new ArgumentException("Raw packet is not of valid size", nameof(packet));

            if (SIGNATURE != packet.Slice(SIGNATURE_OFFSET, sizeof(ushort)).ReadUInt16())
                throw new ArgumentException("Packet unidentifiable", nameof(packet));

            PacketType = (SphynxPacketType)packet.Slice(PACKET_TYPE_OFFSET, sizeof(SphynxPacketType)).ReadUInt32();

            if (((int)PacketType) > 0)
                throw new InvalidDataException($"Raw packet ({PacketType}) type must be response packet");

            ContentSize = packet.Slice(CONTENT_SIZE_OFFSET, sizeof(int)).ReadInt32();
        }

        /// <summary>
        /// Creates a new <see cref="SphynxResponseHeader"/>.
        /// </summary>
        /// <param name="packetType">The type of packet.</param>
        /// <param name="contentSize">The size of the packet's contents.</param>
        public SphynxResponseHeader(SphynxPacketType packetType, int contentSize) : base(packetType, contentSize)
        {
            // Check if first bit off
            if (((int)packetType) > 0)
                throw new ArgumentException("Packet type must be response packet", nameof(packetType));
        }

        /// <inheritdoc/>
        public override void Serialize(Span<byte> buffer)
        {
            if (buffer.Length < HEADER_SIZE)
                throw new ArgumentException($"Cannot serialize response header into {buffer.Length} bytes");

            SIGNATURE.WriteBytes(buffer.Slice(SIGNATURE_OFFSET, sizeof(ushort)));
            ((uint)PacketType).WriteBytes(buffer.Slice(PACKET_TYPE_OFFSET, sizeof(SphynxPacketType)));
            ContentSize.WriteBytes(buffer.Slice(CONTENT_SIZE_OFFSET, sizeof(int)));
        }

        /// <inheritdoc/>
        public override bool Equals(SphynxPacketHeader? other) => other is SphynxResponseHeader && base.Equals(other);

        /// <inheritdoc/>
        public bool Equals(SphynxResponseHeader? other) => Equals(other as SphynxPacketHeader);
    }
}
