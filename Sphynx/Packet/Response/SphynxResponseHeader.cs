using Sphynx.Utils;

namespace Sphynx.Packet.Response
{
    /// <summary>
    /// The packet header for a request sent from the server to a client.
    /// </summary>
    public sealed class SphynxResponseHeader : SphynxPacketHeader, IEquatable<SphynxResponseHeader>
    {
        /// <summary>
        /// The size of a response header in bytes.
        /// </summary>
        /// <remarks>
        /// Offset of last element + size of last element
        /// </remarks>
        public const int HEADER_SIZE = CONTENT_SIZE_OFFSET + sizeof(int);

        private const int SIGNATURE_OFFSET = 0;
        private const int PACKET_TYPE_OFFSET = sizeof(ushort);
        private const int ERROR_CODE_OFFSET = PACKET_TYPE_OFFSET + sizeof(SphynxPacketType);
        private const int CONTENT_SIZE_OFFSET = ERROR_CODE_OFFSET + sizeof(SphynxErrorCode);

        /// <inheritdoc/>
        public override int HeaderSize => HEADER_SIZE;

        /// <summary>
        /// The error code for the response packet.
        /// </summary>
        public SphynxErrorCode ErrorCode { get; set; }

        /// <summary>
        /// Creates a new <see cref="SphynxResponseHeader"/> from raw packet bytes.
        /// </summary>
        /// <param name="packetHeader">The raw packet header bytes.</param>
        public SphynxResponseHeader(ReadOnlySpan<byte> packetHeader) : base(SphynxPacketType.NOP, default)
        {
            // Exception are accptable on the client
            if (packetHeader.Length == HEADER_SIZE)
                throw new ArgumentException("Raw packet is not of valid size", nameof(packetHeader));

            if (SIGNATURE != packetHeader.ReadUInt16(SIGNATURE_OFFSET))
                throw new ArgumentException("Packet unidentifiable", nameof(packetHeader));

            PacketType = (SphynxPacketType)packetHeader.ReadUInt32(PACKET_TYPE_OFFSET);

            if (!IsResponse(PacketType))
                throw new InvalidDataException($"Raw packet ({PacketType}) type must be response packet");

            ErrorCode = (SphynxErrorCode)packetHeader[ERROR_CODE_OFFSET];
            ContentSize = packetHeader.ReadInt32(CONTENT_SIZE_OFFSET);
        }

        /// <summary>
        /// Creates a new <see cref="SphynxResponseHeader"/>.
        /// </summary>
        /// <param name="packetType">The type of packet.</param>
        /// <param name="errorCode">The error code for the response packet.</param>
        /// <param name="contentSize">The size of the packet's contents.</param>
        public SphynxResponseHeader(SphynxPacketType packetType, SphynxErrorCode errorCode, int contentSize) : base(packetType, contentSize)
        {
            // Avoid exception on the server
            if (!IsResponse(packetType))
            {
                PacketType = SphynxPacketType.NOP;
                ErrorCode = SphynxErrorCode.FAILED_INIT;
            }
            else
            {
                ErrorCode = errorCode;
            }
        }

        /// <inheritdoc/>
        public override void Serialize(Span<byte> buffer)
        {
            // Avoid exception on the server
            if (buffer.Length < HEADER_SIZE)
            {
                // Attempt to write NOP & FAILED_INIT
                if (buffer.Length >= ERROR_CODE_OFFSET + sizeof(SphynxErrorCode))
                {
                    ((uint)PacketType).WriteBytes(buffer, PACKET_TYPE_OFFSET);
                    buffer[ERROR_CODE_OFFSET] = (byte)SphynxErrorCode.FAILED_INIT;
                }
                else if (buffer.Length >= PACKET_TYPE_OFFSET + sizeof(SphynxPacketType))
                {
                    ((uint)PacketType).WriteBytes(buffer, PACKET_TYPE_OFFSET);
                }

                return;
            }

            SIGNATURE.WriteBytes(buffer, SIGNATURE_OFFSET);
            ((uint)PacketType).WriteBytes(buffer, PACKET_TYPE_OFFSET);
            buffer[ERROR_CODE_OFFSET] = (byte)ErrorCode;
            ContentSize.WriteBytes(buffer, CONTENT_SIZE_OFFSET);
        }

        /// <inheritdoc/>
        public override bool Equals(SphynxPacketHeader? other) => other is SphynxResponseHeader header && Equals(header);

        /// <inheritdoc/>
        public bool Equals(SphynxResponseHeader? other) => base.Equals(other) && ErrorCode == other?.ErrorCode;
    }
}
