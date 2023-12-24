using Sphynx.Utils;

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
        public static readonly byte[] SIGNATURE = { 0x53, 0x50 };

        /// <summary>
        /// The type of this packet.
        /// </summary>
        public SphynxPacketType PacketType { get; protected set; }

        /// <summary>
        /// The size of the content in this packet in bytes. 
        /// </summary>
        public int ContentSize { get; protected set; }

        /// <summary>
        /// Returns the size of this particular header in bytes.
        /// </summary>
        public abstract int Size { get; }

        /// <summary>
        /// Serializes this header into a stream of bytes.
        /// </summary>
        /// <param name="stream">The stream to serialize this header into.</param>
        public abstract void Serialize(Span<byte> stream);

        /// <summary>
        /// Serializes the <see cref="SIGNATURE"/> into the <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The stream to serialize into.</param>
        protected virtual void SerializeSignature(Span<byte> stream)
        {
            stream.CopyFrom(SIGNATURE);
        }

        /// <summary>
        /// Serializes a <see cref="SphynxPacketType"/> into the <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The stream to serialize into.</param>
        /// <param name="packetType">The packet type.</param>
        protected unsafe virtual void SerializePacketType(Span<byte> stream, SphynxPacketType packetType)
        {
            uint rawPacketType = (uint)packetType;
            byte* serializedPacketType = stackalloc byte[sizeof(SphynxPacketType)];

            for (int i = 0; i < sizeof(SphynxPacketType); i++)
            {
                serializedPacketType[i] = (byte)((rawPacketType >> 8 * i) & 0xFF);
            }

            stream.CopyFrom(serializedPacketType, sizeof(SphynxPacketType));
        }

        /// <summary>
        /// Serializes the <see cref="ContentSize"/> into the <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The stream to serialize into.</param>
        protected unsafe virtual void SerializeContentSize(Span<byte> stream)
        {
            const int CONTENT_SIZE_SIZE = sizeof(int);
            byte* serializedPacketType = stackalloc byte[CONTENT_SIZE_SIZE];

            for (int i = 0; i < CONTENT_SIZE_SIZE; i++)
            {
                serializedPacketType[i] = (byte)((ContentSize >> 8 * i) & 0xFF);
            }

            stream.CopyFrom(serializedPacketType, CONTENT_SIZE_SIZE);
        }
    }
}
