using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using FastEnumUtility;
using Sphynx.Network.PacketV2;
using Sphynx.Network.Serialization;
using Sphynx.Utils;
using Version = Sphynx.Core.Version;

namespace Sphynx.Network.Transport
{
    /// <summary>
    /// The transport header of a <see cref="SphynxPacket"/>.
    /// </summary>
    public readonly struct SphynxPacketHeader : IEquatable<SphynxPacketHeader>, IEquatable<SphynxPacketHeader?>
    {
        /// <summary>
        /// The (exact) Serialization size of <see cref="SIGNATURE"/> in bytes.
        /// </summary>
        public static readonly int SignatureSize = BinarySerializer.SizeOf<ushort>();

        /// <summary>
        /// The packet signature to safeguard against corrupted packets.
        /// </summary>
        public const ushort SIGNATURE = 0x5053;

        /// <summary>
        /// The (exact) serialization size of this header in bytes.
        /// </summary>
        public static readonly int Size = SignatureSize
                                          + BinarySerializer.SizeOf<Version>()
                                          + BinarySerializer.SizeOf<SphynxPacketType>()
                                          + BinarySerializer.SizeOf<int>();

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
        /// Continuously reads from the <paramref name="stream"/> until a valid <see cref="SphynxPacketHeader"/> has
        /// been successfully consumed, or cancellation is requested.
        /// </summary>
        /// <param name="stream">The stream from which to consume the header.</param>
        /// <param name="cancellationToken">The cancellation token to abort the consumption request.</param>
        /// <returns>The first successfully consumed <see cref="SphynxPacketHeader"/>.</returns>
        public static async Task<SphynxPacketHeader> ReceiveAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            if (!stream.CanRead)
                throw new ArgumentException("Stream must be readable", nameof(stream));

            byte[] rentBuffer = ArrayPool<byte>.Shared.Rent(Size);
            var buffer = rentBuffer.AsMemory()[..Size];

            try
            {
                await stream.FillAsync(buffer, cancellationToken).ConfigureAwait(false);
                SphynxPacketHeader? header;

                while (!TryDeserialize(buffer.Span, out header))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    buffer.ShiftLeft(1);

                    await stream.FillAsync(buffer[^1..], cancellationToken).ConfigureAwait(false);
                }

                return header.Value;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rentBuffer);
            }
        }

        /// <summary>
        /// Creates a new <see cref="SphynxPacketHeader"/> from the <paramref name="packetHeader"/>.
        /// </summary>
        /// <param name="packetHeader">The raw bytes for a <see cref="SphynxPacketHeader"/>.</param>
        /// <param name="header">The deserialized header.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> packetHeader, [NotNullWhen(true)] out SphynxPacketHeader? header)
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
        public static bool TryDeserialize(ref BinaryDeserializer deserializer, [NotNullWhen(true)] out SphynxPacketHeader? header)
        {
            if (deserializer.CurrentSpan.Length != Size)
            {
                header = null;
                return false;
            }

            ushort signature = deserializer.ReadUnmanaged<ushort>();

            if (signature != SIGNATURE)
            {
                header = null;
                return false;
            }

            var version = deserializer.ReadVersion();
            var packetType = deserializer.ReadEnum<SphynxPacketType>();

            if (!FastEnum.IsDefined(packetType))
            {
                header = null;
                return false;
            }

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

            if (!TrySerialize(packetBytes))
                throw new SerializationException("Could not serialize packet header");

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

            serializer.WriteUnmanaged(SIGNATURE);
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
