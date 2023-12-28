using System.Runtime.InteropServices;

namespace Sphynx.Packet
{
    /// <summary>
    /// Represents the header of a <see cref="SphynxPacket"/>.
    /// </summary>
    public abstract class SphynxPacketHeader
    {
        /// <summary>
        /// The packet signature to safe-guards against corrupted packets.
        /// </summary>
        public const ushort SIGNATURE = 0x5350;

        /// <summary>
        /// The type of this packet.
        /// </summary>
        public SphynxPacketType PacketType { get; set; }

        /// <summary>
        /// The size of the content in this packet in bytes. 
        /// </summary>
        public int ContentSize { get; set; }

        /// <summary>
        /// Returns the size of this particular header in bytes.
        /// </summary>
        public abstract int HeaderSize { get; }

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
        /// Serializes this header into a buffer of bytes.
        /// </summary>
        /// <param name="buffer">The buffer to serialize this header into.</param>
        public abstract void Serialize(Span<byte> buffer);

        /// <summary>
        /// Verifies signature bytes against <see cref="SIGNATURE"/>.
        /// </summary>
        /// <param name="serializedSig">Signature bytes.</param>
        /// <returns><see langword="true"/> if the signature is correct; <see langword="false"/> otherwise.</returns>
        protected bool VerifySignature(ReadOnlySpan<byte> serializedSig)
        {
            return MemoryMarshal.Cast<byte, ushort>(serializedSig)[0] == SIGNATURE;
        }

        /// <summary>
        /// Serializes the <see cref="SIGNATURE"/> into the <paramref name="buffer"/>.
        /// </summary>
        /// <param name="buffer">The buffer to serialize into.</param>
        protected unsafe virtual void SerializeSignature(Span<byte> buffer)
        {
            ReadOnlySpan<ushort> sigBytes = stackalloc ushort[] { SIGNATURE };
            ReadOnlySpan<byte> serializedSig = MemoryMarshal.Cast<ushort, byte>(sigBytes);
            serializedSig.CopyTo(buffer);
        }

        /// <summary>
        /// Serializes a <see cref="SphynxPacketType"/> into the <paramref name="buffer"/>.
        /// </summary>
        /// <param name="buffer">The buffer to serialize into.</param>
        /// <param name="packetType">The packet type.</param>
        protected unsafe virtual void SerializePacketType(Span<byte> buffer, SphynxPacketType packetType)
        {
            ReadOnlySpan<uint> packetTypeBytes = stackalloc uint[] { (uint)packetType };
            ReadOnlySpan<byte> serializedPacketType = MemoryMarshal.Cast<uint, byte>(packetTypeBytes);
            serializedPacketType.CopyTo(buffer);
        }

        /// <summary>
        /// Serializes the <see cref="ContentSize"/> into the <paramref name="buffer"/>.
        /// </summary>
        /// <param name="buffer">The buffer to serialize into.</param>
        protected unsafe virtual void SerializeContentSize(Span<byte> buffer)
        {
            ReadOnlySpan<int> contentSizeBytes = stackalloc int[] { ContentSize };
            ReadOnlySpan<byte> serializedContentSize = MemoryMarshal.Cast<int, byte>(contentSizeBytes);
            serializedContentSize.CopyTo(buffer);
        }
    }
}
