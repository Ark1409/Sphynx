using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Sphynx.Packet
{
    /// <summary>
    /// The packet header for a request sent from the server to a client.
    /// </summary>
    public sealed class SphynxResponseHeader : SphynxPacketHeader
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

            if (!VerifySignature(packet.Slice(SIGNATURE_OFFSET, sizeof(ushort))))
                throw new ArgumentException("Packet unidentifiable", nameof(packet));

            PacketType = (SphynxPacketType)MemoryMarshal.Cast<byte, uint>(packet.Slice(PACKET_TYPE_OFFSET, sizeof(SphynxPacketType)))[0];
            
            if (((int)PacketType) > 0)
                throw new ArgumentException($"Raw packet ({PacketType}) type must be response packet", nameof(PacketType));

            ContentSize = MemoryMarshal.Cast<byte, int>(packet.Slice(CONTENT_SIZE_OFFSET, sizeof(int)))[0];
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

            SerializeSignature(buffer.Slice(SIGNATURE_OFFSET, sizeof(ushort)));
            SerializePacketType(buffer.Slice(PACKET_TYPE_OFFSET, sizeof(SphynxPacketType)), PacketType);
            SerializeContentSize(buffer.Slice(CONTENT_SIZE_OFFSET, sizeof(int)));
        }
    }
}
