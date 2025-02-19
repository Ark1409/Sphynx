using System.Diagnostics.CodeAnalysis;
using Sphynx.Network.PacketV2;
using Sphynx.Network.Serialization;
using Version = Sphynx.Core.Version;

namespace Sphynx.Network.Transport
{
    /// <summary>
    /// Represents the transport header of a <see cref="SphynxPacket"/>.
    /// </summary>
    public readonly struct SphynxPacketHeader : IEquatable<SphynxPacketHeader?>, IEquatable<SphynxPacketHeader>
    {
        /// <summary>
        /// The packet signature to safeguard against corrupted packets.
        /// </summary>
        public const ushort SIGNATURE = 0x5350;

        /// <summary>
        /// The (exact) serialization size of this header in bytes.
        /// </summary>
        public static readonly int Size = BinarySerializer.SizeOf<Version>() +
                                          BinarySerializer.SizeOf<SphynxPacketType>() + BinarySerializer.SizeOf<int>();

        /// <summary>
        /// The schema version against which the packet was serialized.
        /// </summary>
        public Version Version { get; }

        /// <summary>
        /// The type of this packet.
        /// </summary>
        public SphynxPacketType PacketType { get; }

        /// <summary>
        /// The size of the content in this packet in bytes.
        /// </summary>
        public int ContentSize { get; }

        /// <summary>
        /// Creates a new <see cref="SphynxPacketHeader"/>.
        /// </summary>
        /// <param name="version">The schema version against which the packet was serialized.</param>
        /// <param name="packetType">The type of packet.</param>
        /// <param name="contentSize">The size of the packet's contents.</param>
        public SphynxPacketHeader(Version version, SphynxPacketType packetType, int contentSize)
        {
            Version = version;
            PacketType = packetType;
            ContentSize = contentSize;
        }

        /// <summary>
        /// Creates a new <see cref="SphynxPacketHeader"/> from the <paramref name="packetHeader"/>.
        /// </summary>
        /// <param name="packetHeader">The raw bytes for a <see cref="SphynxPacketHeader"/>.</param>
        /// <param name="header">The deserialized header.</param>
        public static bool TryDeserialize(
            ReadOnlySpan<byte> packetHeader,
            [NotNullWhen(true)] out SphynxPacketHeader? header)
        {
            var deserializer = new BinaryDeserializer(packetHeader);
            return TryDeserialize(ref deserializer, out header);
        }

        /// <summary>
        /// Creates a new <see cref="SphynxPacketHeader"/> from the <paramref name="deserializer"/>.
        /// </summary>
        /// <param name="deserializer">The deserializer containing the bytes for a
        /// <see cref="SphynxPacketHeader"/>.</param>
        /// <param name="header">The deserialized header.</param>
        public static bool TryDeserialize(
            ref BinaryDeserializer deserializer,
            [NotNullWhen(true)] out SphynxPacketHeader? header)
        {
            if (deserializer.CurrentSpan.Length < Size)
            {
                header = null;
                return false;
            }

            var version = deserializer.ReadVersion();
            var packetType = deserializer.ReadEnum<SphynxPacketType>();
            int contentSize = deserializer.ReadInt32();

            header = new SphynxPacketHeader(version, packetType, contentSize);
            return true;
        }

        /// <summary>
        /// Serializes this packet header into a tightly-packed byte array.
        /// </summary>
        /// <return>This packet header serialized as a byte array.</return>
        public byte[] Serialize()
        {
            byte[] packetBytes = new byte[Size];
            TrySerialize(packetBytes);
            return packetBytes;
        }

        /// <summary>
        /// Attempts to serialize this header into a buffer of bytes.
        /// </summary>
        /// <param name="buffer">The buffer to serialize this header into.</param>
        public bool TrySerialize(Span<byte> buffer)
        {
            var serializer = new BinarySerializer(buffer);
            return TrySerialize(ref serializer);
        }

        /// <summary>
        /// Attempts to serialize this header using the specified <paramref name="serializer"/>.
        /// </summary>
        /// <param name="serializer">The buffer to serialize this header into.</param>
        public bool TrySerialize(ref BinarySerializer serializer)
        {
            if (serializer.CurrentSpan.Length < Size)
            {
                return false;
            }

            serializer.WriteVersion(Version);
            serializer.WriteEnum(PacketType);
            serializer.WriteInt32(ContentSize);
            return true;
        }

        /// <inheritdoc/>
        public bool Equals(SphynxPacketHeader? other) => other.HasValue && Equals(other.Value);

        /// <inheritdoc/>
        public bool Equals(SphynxPacketHeader other) => Equals(in other);

        /// <inheritdoc cref="Equals(SphynxPacketHeader)"/>
        public bool Equals(in SphynxPacketHeader other) =>
            Version == other.Version && PacketType == other.PacketType && ContentSize == other.ContentSize;
    }
}
