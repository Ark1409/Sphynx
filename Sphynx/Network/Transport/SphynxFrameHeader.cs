// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using FastEnumUtility;
using Sphynx.Network.Packet;
using Sphynx.Network.Serialization;
using Sphynx.Utils;
using Version = Sphynx.Core.Version;

namespace Sphynx.Network.Transport
{
    public readonly struct SphynxFrameHeader : IEquatable<SphynxFrameHeader>, IEquatable<SphynxFrameHeader?>
    {
        /// <summary>
        /// The (exact) serialization size of this header in bytes.
        /// </summary>
        public const int SIZE = 17; // SP (2), Version (4), FrameType (1), ChannelId (8), FrameSize (2)

        /// <summary>
        /// The packet signature to identify Sphynx frames.
        /// </summary>
        public static readonly ReadOnlyMemory<byte> Signature = new(new byte[] { 0x53, 0x50 }); // SP

        /// <summary>
        /// The protocol version against which the frame was serialized.
        /// </summary>
        public Version Version { get; init; }

        /// <summary>
        /// The type of this frame.
        /// </summary>
        public SphynxFrameType FrameType { get; init; }

        /// <summary>
        /// The logical channel through which this frame will be sent.
        /// </summary>
        public long ChannelId { get; init; }

        /// <summary>
        /// The size of the message content within this frame in bytes.
        /// </summary>
        public short FrameSize { get; init; }

        /// <summary>
        /// Creates a new <see cref="SphynxFrameHeader"/>.
        /// </summary>
        public SphynxFrameHeader()
        {
        }

        /// <summary>
        /// Creates a new <see cref="SphynxFrameHeader"/>.
        /// </summary>
        /// <param name="version">The schema version against which the frame was serialized.</param>
        /// <param name="frameType">The type of frame.</param>
        ///  <param name="channelId">The logical channel through which this frame will be sent.</param>
        /// <param name="frameSize">The size of the content within this frame in bytes.</param>
        public SphynxFrameHeader(Version version, SphynxFrameType frameType, long channelId, short frameSize)
        {
            Version = version;
            FrameType = frameType;
            ChannelId = channelId;
            FrameSize = frameSize;
        }

        // We're assuming that frame reading will be required in high throughput scenarios (e.g. parsing messages on the server-side)
        // so pooling should ease the allocation burden.
        private static readonly ArrayPool<byte> _networkArrayPool = ArrayPool<byte>.Create(SIZE, 50);

        /// <summary>
        /// Continuously reads from a <paramref name="stream"/> until a valid <see cref="SphynxFrameHeader"/> has
        /// been successfully consumed, or cancellation is requested.
        /// </summary>
        /// <param name="stream">The stream from which to consume the header.</param>
        /// <param name="cancellationToken">The cancellation token to abort the consumption request.</param>
        /// <returns>The first successfully consumed <see cref="SphynxFrameHeader"/>.</returns>
        public static ValueTask<SphynxFrameHeader> ReceiveAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return ValueTask.FromCanceled<SphynxFrameHeader>(cancellationToken);

            if (!stream.CanRead)
                return ValueTask.FromException<SphynxFrameHeader>(new ArgumentException("Stream must be readable", nameof(stream)));

            return Core(stream, _networkArrayPool.Rent(SIZE), cancellationToken);

            [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
            static async ValueTask<SphynxFrameHeader> Core(Stream stream, byte[] rentArray, CancellationToken token)
            {
                try
                {
                    var rentBuffer = rentArray.AsMemory()[..SIZE];

                    await stream.FillAsync(rentBuffer, cancellationToken: token).ConfigureAwait(false);

                    SphynxFrameHeader? header;

                    while (!TryDeserialize(rentBuffer.Span, out header))
                    {
                        token.ThrowIfCancellationRequested();

                        rentBuffer.ShiftLeft(1);

                        await stream.FillAsync(rentBuffer[^1..], cancellationToken: token).ConfigureAwait(false);
                    }

                    return header.Value;
                }
                finally
                {
                    _networkArrayPool.Return(rentArray);
                }
            }
        }

        public static ValueTask SendAsync(in SphynxFrameHeader header, Stream stream, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return ValueTask.FromCanceled(cancellationToken);

            if (!stream.CanWrite)
                return ValueTask.FromException(new ArgumentException("Stream must be writable", nameof(stream)));

            byte[] rentArray = _networkArrayPool.Rent(SIZE);
            bool isSerialized = false;

            try
            {
                if (!header.TrySerialize(rentArray.AsSpan()[..SIZE]))
                    return ValueTask.FromException(new SerializationException($"Could not serialize frame header: {header}"));

                isSerialized = true;
            }
            catch (Exception ex)
            {
                return ValueTask.FromException(ex);
            }
            finally
            {
                if (!isSerialized)
                    _networkArrayPool.Return(rentArray);
            }

            return Core(stream, rentArray, cancellationToken);

            [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
            static async ValueTask Core(Stream stream, byte[] rentArray, CancellationToken token)
            {
                try
                {
                    await stream.WriteAsync(rentArray.AsMemory()[..SIZE], cancellationToken: token).ConfigureAwait(false);
                }
                finally
                {
                    _networkArrayPool.Return(rentArray);
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="SphynxFrameHeader"/> from the <paramref name="frameHeader"/>.
        /// </summary>
        /// <param name="frameHeader">The raw bytes for a <see cref="SphynxFrameHeader"/>.</param>
        /// <param name="header">The deserialized header.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> frameHeader, [NotNullWhen(true)] out SphynxFrameHeader? header)
        {
            var deserializer = new BinaryDeserializer(frameHeader[..SIZE]);
            return TryDeserialize(ref deserializer, out header);
        }

        /// <summary>
        /// Creates a new <see cref="SphynxFrameHeader"/> from the <paramref name="deserializer"/>.
        /// </summary>
        /// <param name="deserializer">The deserializer containing the bytes for a <see cref="SphynxFrameHeader"/>.</param>
        /// <param name="header">The deserialized header.</param>
        public static bool TryDeserialize(ref BinaryDeserializer deserializer, [NotNullWhen(true)] out SphynxFrameHeader? header)
        {
            if (deserializer.Length - deserializer.Offset < SIZE)
            {
                header = null;
                return false;
            }

            long oldOffset = deserializer.Offset;

            Span<byte> signature = stackalloc byte[Signature.Length];
            deserializer.ReadRaw(signature);

            if (!signature.SequenceEqual(Signature.Span))
            {
                deserializer.Offset = oldOffset;
                header = null;
                return false;
            }

            var version = Version.FromInt32(deserializer.ReadInt32());

            Debug.Assert(Enum.GetUnderlyingType(typeof(SphynxFrameType)) == typeof(byte));
            var frameType = (SphynxFrameType)deserializer.ReadUInt8();

            if (!FastEnum.IsDefined(frameType))
            {
                deserializer.Offset = oldOffset;
                header = null;
                return false;
            }

            long channelId = deserializer.ReadInt64();
            short frameSize = deserializer.ReadInt16();

            Debug.Assert(deserializer.Offset - oldOffset == SIZE);

            header = new SphynxFrameHeader(version, frameType, channelId, frameSize);
            return true;
        }

        /// <summary>
        /// Serializes this packet header into a tightly-packed byte array.
        /// </summary>
        /// <returns>This packet header serialized as a byte array.</returns>
        public byte[] Serialize()
        {
            byte[] packetBytes = new byte[SIZE];
            Serialize(packetBytes.AsSpan());
            return packetBytes;
        }

        /// <summary>
        /// Serialize this header into a buffer of bytes.
        /// </summary>
        /// <param name="buffer">The buffer to serialize this header into.</param>
        /// <exception cref="SerializationException">The buffer is too small to serialize this header.</exception>
        public void Serialize(Span<byte> buffer)
        {
            if (!TrySerialize(buffer))
                throw new SerializationException($"Could not serialize frame header: {this}");
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
        /// Serializes this header into a buffer of bytes.
        /// </summary>
        /// <param name="serializer">The buffer to serialize this header into.</param>
        /// <exception cref="SerializationException">The buffer is too small to serialize this header.</exception>
        public void Serialize(ref BinarySerializer serializer)
        {
            if (!TrySerialize(ref serializer))
                throw new SerializationException($"Could not serialize frame header: {this}");
        }

        /// <summary>
        /// Attempts to serialize this header using the specified <paramref name="serializer"/>.
        /// </summary>
        /// <param name="serializer">The buffer to serialize this header into.</param>
        public bool TrySerialize(ref BinarySerializer serializer)
        {
            // Ensure we have enough space to serialize into
            if (serializer.HasSpan && serializer.Span.Slice((int)serializer.BytesWritten).Length < SIZE)
                return false;

            long oldOffset = serializer.BytesWritten;

            serializer.WriteRaw(Signature.Span);
            serializer.WriteInt32(Version.ToInt32());

            Debug.Assert(Enum.GetUnderlyingType(typeof(SphynxFrameType)) == typeof(byte));
            serializer.WriteUInt8((byte)FrameType);

            serializer.WriteInt64(ChannelId);
            serializer.WriteInt16(FrameSize);

            Debug.Assert(serializer.BytesWritten - oldOffset == SIZE);
            return true;
        }

        public bool Equals(in SphynxFrameHeader other) => Version == other.Version
                                                          && FrameType == other.FrameType
                                                          && FrameSize == other.FrameSize
                                                          && ChannelId == other.ChannelId;

        public bool Equals(SphynxFrameHeader other) => Equals(in other);

        public bool Equals(SphynxFrameHeader? other) => Version == other?.Version
                                                        && FrameType == other?.FrameType
                                                        && FrameSize == other?.FrameSize
                                                        && ChannelId == other?.ChannelId;

        public override string ToString() =>
            "{ " +
            $"{nameof(Version)}: {Version}, " +
            $"{nameof(FrameType)}: {FrameType}, " +
            $"{nameof(ChannelId)}: {ChannelId}, " +
            $"{nameof(FrameSize)}: {FrameSize}, " +
            "}";
    }
}
