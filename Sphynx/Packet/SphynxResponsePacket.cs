using Sphynx.Packet.Response;

namespace Sphynx.Packet
{
    /// <summary>
    /// Represents a response packet.
    /// </summary>
    public abstract class SphynxResponsePacket : SphynxPacket, IEquatable<SphynxResponsePacket>
    {
        /// <summary>
        /// <inheritdoc cref="SphynxErrorCode"/>
        /// </summary>
        public SphynxErrorCode ErrorCode { get; set; }

        /// <summary>
        /// Creates a new <see cref="SphynxResponsePacket"/>.
        /// </summary>
        /// <param name="errorCode">The error code for the response packet.</param>
        public SphynxResponsePacket(SphynxErrorCode errorCode)
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Serializes a packet header into the specified <paramref name="buffer"/>.
        /// </summary>
        /// <param name="buffer">The buffer to serialize this header into.</param>
        /// <param name="contentSize">The <see cref="SphynxPacketHeader.ContentSize"/>.</param>
        protected override SphynxResponseHeader SerializeHeader(Span<byte> buffer, int contentSize)
        {
            var header = new SphynxResponseHeader(PacketType, ErrorCode, contentSize);
            header.Serialize(buffer.Slice(0, SphynxResponseHeader.HEADER_SIZE));
            return header;
        }

        /// <inheritdoc/>
        public virtual bool Equals(SphynxResponsePacket? other) => PacketType == other?.PacketType && ErrorCode == other?.ErrorCode;
    }
}