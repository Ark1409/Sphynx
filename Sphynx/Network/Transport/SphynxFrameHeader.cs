// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Sphynx.Network.Serialization;
using Sphynx.Utils;
using Version = Sphynx.Core.Version;

namespace Sphynx.Network.Transport
{
    public readonly struct SphynxFrameHeader : IEquatable<SphynxFrameHeader>, IEquatable<SphynxFrameHeader?>
    {
        /// <summary>
        /// The maximum size of a frame.
        /// </summary>
        public const int MAX_FRAME_SIZE = short.MaxValue; // ~32KB

        /// <summary>
        /// The currently supported protocol version.
        /// </summary>
        public static readonly Version ProtocolVersion = new(1);

        /// <summary>
        /// The (exact) serialization size of this header in bytes.
        /// </summary>
        public const int SIZE = 8; // SP (2), Version (1), FrameType + Flags (1), ChannelId (2), FrameSize (2)

        /// <summary>
        /// The packet signature to identify Sphynx frames.
        /// </summary>
        public static readonly ReadOnlyMemory<byte> Signature = new byte[] { 0x53, 0x50 }; // SP

        /// <summary>
        /// The protocol version against which the frame was serialized.
        /// </summary>
        public Version Version { get; private init; } = ProtocolVersion;

        /// <summary>
        /// The type of this frame.
        /// </summary>
        public SphynxFrameType FrameType { get; init; }

        /// <summary>
        /// The control flags for this frame type.
        /// </summary>
        /// <seealso cref="ChannelDataFlags"/>
        /// <seealso cref="ChannelReleaseFlags"/>
        public byte Flags { get; init; }

        /// <summary>
        /// The logical channel through which this frame will be sent.
        /// </summary>
        public ChannelId ChannelId { get; init; }

        /// <summary>
        /// The size of the content within this frame in bytes.
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
        /// <param name="frameType">The type of frame.</param>
        ///  <param name="channelId">The logical channel through which this frame will be sent.</param>
        /// <param name="frameSize">The size of the content within this frame in bytes.</param>
        public SphynxFrameHeader(SphynxFrameType frameType, ChannelId channelId, short frameSize)
            : this(frameType, 0, channelId, frameSize)
        {
        }

        /// <summary>
        /// Creates a new <see cref="SphynxFrameHeader"/>.
        /// </summary>
        /// <param name="frameType">The type of frame.</param>
        /// <param name="flags">The control flags for this frame type.</param>
        /// <param name="channelId">The logical channel through which this frame will be sent.</param>
        /// <param name="frameSize">The size of the content within this frame in bytes.</param>
        public SphynxFrameHeader(SphynxFrameType frameType, byte flags, ChannelId channelId, short frameSize)
        {
            FrameType = frameType;
            Flags = flags;
            ChannelId = channelId;
            FrameSize = frameSize;
        }

        // We're assuming that frame reading will be required in high throughput scenarios (e.g. parsing messages on the server-side)
        // so pooling should ease the allocation burden.
        private static readonly ArrayPool<byte> _networkArrayPool = ArrayPool<byte>.Create(SIZE, 50);

        // TODO: Allow ReceiveAsync-ing from a PipeReader

        /// <summary>
        /// Continuously reads from a <paramref name="stream"/> until a <see cref="SphynxFrameHeader"/> has been successfully consumed.
        /// </summary>
        /// <param name="stream">The stream from which to consume the header.</param>
        /// <param name="cancellationToken">The cancellation token to abort the receive request.</param>
        /// <param name="skipInvalid">Whether to allow <see cref="IsValid">invalid</see> frames to be read.</param>
        /// <exception cref="ArgumentException">If <paramref name="stream"/> is not readable.</exception>
        /// <returns>The first successfully consumed <see cref="SphynxFrameHeader"/>.</returns>
        public static ValueTask<SphynxFrameHeader> ReceiveAsync(Stream stream, bool skipInvalid = true, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return ValueTask.FromCanceled<SphynxFrameHeader>(cancellationToken);

            if (!stream.CanRead)
                return ValueTask.FromException<SphynxFrameHeader>(new ArgumentException("Stream must be readable", nameof(stream)));

            return Core(stream, _networkArrayPool.Rent(SIZE), skipInvalid, cancellationToken);

            [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
            static async ValueTask<SphynxFrameHeader> Core(Stream stream, byte[] rentArray, bool skipInvalid, CancellationToken token)
            {
                try
                {
                    var rentBuffer = rentArray.AsMemory()[..SIZE];

                    await stream.ReadExactlyAsync(rentBuffer, cancellationToken: token).ConfigureAwait(false);

                    SphynxFrameHeader? header;

                    while (!TryDeserialize(rentBuffer.Span, out header, skipInvalid))
                    {
                        token.ThrowIfCancellationRequested();

                        rentBuffer.ShiftLeft(1);

                        await stream.ReadExactlyAsync(rentBuffer[^1..], cancellationToken: token).ConfigureAwait(false);
                    }

                    return header.Value;
                }
                finally
                {
                    _networkArrayPool.Return(rentArray);
                }
            }
        }

        /// <summary>
        /// Serializes this header into the <paramref name="stream"/> while also performing basic validity checks.
        /// </summary>
        /// <param name="header">The header to serialize.</param>
        /// <param name="stream">The stream to serialize into.</param>
        /// <param name="cancellationToken">A cancellation token for the serialization operation.</param>
        /// <exception cref="ArgumentException">Stream is not writeable.</exception>
        /// <exception cref="InvalidOperationException">If the frame is invalid.</exception>
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
                    return ValueTask.FromException(new InvalidOperationException($"Could not serialize invalid frame header: {header}"));

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
                    await stream.WriteAsync(rentArray.AsMemory()[..SIZE], token).ConfigureAwait(false);
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
        /// <param name="verify">Whether the validity of the frame should be checked when deserializing.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> frameHeader, [NotNullWhen(true)] out SphynxFrameHeader? header, bool verify = true)
        {
            var deserializer = new BinaryDeserializer(frameHeader);
            return TryDeserialize(ref deserializer, out header, verify);
        }

        /// <summary>
        /// Creates a new <see cref="SphynxFrameHeader"/> from the <paramref name="deserializer"/>.
        /// </summary>
        /// <param name="deserializer">The deserializer containing the bytes for a <see cref="SphynxFrameHeader"/>.</param>
        /// <param name="header">The deserialized header.</param>
        /// <param name="verify">Whether the validity of the frame should be checked when deserializing.</param>
        public static bool TryDeserialize(ref BinaryDeserializer deserializer, [NotNullWhen(true)] out SphynxFrameHeader? header, bool verify = true)
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

            byte version = deserializer.ReadUInt8();
            byte typeAndFlags = deserializer.ReadUInt8();
            Debug.Assert(Enum.GetUnderlyingType(typeof(SphynxFrameType)) == typeof(byte));
            var frameType = (SphynxFrameType)((typeAndFlags >> 4) & 0x0F);
            byte flags = (byte)(typeAndFlags & 0x0F);
            Debug.Assert(ChannelId.SIZE == sizeof(ushort));
            ushort channelId = deserializer.ReadUInt16();
            short frameSize = deserializer.ReadInt16();

            Debug.Assert(deserializer.Offset - oldOffset == SIZE);

            var wireHeader = new SphynxFrameHeader
            {
                Version = new Version(version),
                ChannelId = channelId,
                FrameType = frameType,
                Flags = flags,
                FrameSize = frameSize,
            };

            if (verify && !wireHeader.IsValid())
            {
                deserializer.Offset = oldOffset;
                header = null;
                return false;
            }

            header = wireHeader;
            return true;
        }

        /// <summary>
        /// Serializes this packet header into a tightly-packed byte array.
        /// </summary>
        /// <param name="verify">Whether the validity of this frame should be checked before serializing.</param>
        /// <returns>This packet header serialized as a byte array.</returns>
        /// <exception cref="InvalidOperationException">If <paramref name="verify"/> is true and the frame is invalid.</exception>
        public byte[] Serialize(bool verify = true)
        {
            byte[] packetBytes = new byte[SIZE];
            Serialize(packetBytes.AsSpan());
            return packetBytes;
        }

        /// <summary>
        /// Serialize this header into a buffer of bytes.
        /// </summary>
        /// <param name="buffer">The buffer to serialize this header into.</param>
        /// <param name="verify">Whether the validity of this frame should be checked before serializing.</param>
        /// <exception cref="ArgumentException">If the buffer is too small to serialize this header.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="verify"/> is true and the frame is invalid.</exception>
        public void Serialize(Span<byte> buffer, bool verify = true)
        {
            if (buffer.Length < SIZE)
                ThrowSerializeException();

            var serializer = new BinarySerializer(buffer);
            Serialize(ref serializer, verify);

            [DoesNotReturn]
            static void ThrowSerializeException() => throw new ArgumentException("Could not serialize frame header into buffer", nameof(buffer));
        }

        /// <summary>
        /// Attempts to serialize this header into a buffer of bytes.
        /// </summary>
        /// <param name="buffer">The buffer to serialize this header into.</param>
        public bool TrySerialize(Span<byte> buffer, bool verify = true)
        {
            var serializer = new BinarySerializer(buffer);
            return TrySerialize(ref serializer);
        }

        /// <summary>
        /// Serializes this header into a buffer of bytes.
        /// </summary>
        /// <param name="serializer">The buffer to serialize this header into.</param>
        /// <param name="verify">Whether the validity of this frame should be checked before serializing.</param>
        /// <exception cref="SerializationException">The buffer is too small to serialize this header.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="verify"/> is true and the frame is invalid.</exception>
        public void Serialize(ref BinarySerializer serializer, bool verify = true)
        {
            if (!TrySerialize(ref serializer, verify))
                ThrowSerializeException(in this, ref serializer, verify);

            [DoesNotReturn]
            static void ThrowSerializeException(in SphynxFrameHeader header, ref BinarySerializer bs, bool verify)
            {
                bool insufficientSpace = bs.HasSpan && bs.Span[(int)bs.BytesWritten..].Length < SIZE;

                if (insufficientSpace)
                    throw new ArgumentException("Could not serialize frame header into serializer", nameof(serializer));

                Debug.Assert(verify);
                throw new InvalidOperationException($"Could not serialize invalid frame header: {header}");
            }
        }

        /// <summary>
        /// Attempts to serialize this header using the specified <paramref name="serializer"/>.
        /// </summary>
        /// <param name="serializer">The buffer to serialize this header into.</param>
        /// <param name="verify">Whether validity of this frame should be checked before serializing.</param>
        public bool TrySerialize(ref BinarySerializer serializer, bool verify = true)
        {
            bool insufficientSpace = serializer.HasSpan && serializer.Span[(int)serializer.BytesWritten..].Length < SIZE;

            if (insufficientSpace || (verify && !IsValid()))
                return false;

            long oldOffset = serializer.BytesWritten;

            serializer.WriteRaw(Signature.Span);
            serializer.WriteUInt8(Version.Major);
            Debug.Assert(Enum.GetUnderlyingType(typeof(SphynxFrameType)) == typeof(byte));
            serializer.WriteUInt8((byte)(((byte)FrameType << 4) | Flags));
            Debug.Assert(ChannelId.SIZE == sizeof(ushort));
            serializer.WriteUInt16(ChannelId);
            serializer.WriteInt16(FrameSize);

            Debug.Assert(serializer.BytesWritten - oldOffset == SIZE);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasFlags(byte flags) => (Flags & flags) == flags;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasAnyFlag(byte flags) => (Flags & flags) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SphynxFrameHeader WithFlags(byte flags) => this with { Flags = (byte)(Flags | flags) };

        /// <summary>
        /// Checks the validity of this header.
        /// </summary>
        /// <returns>Whether this header is valid.</returns>
        public bool IsValid()
        {
            if (Version != ProtocolVersion || ChannelId < ChannelId.MinValue || ChannelId > ChannelId.MaxValue)
                return false;

            return FrameType switch
            {
                SphynxFrameType.CHANNEL_DATA => Flags is 0 or <= (ChannelDataFlags.CHANNEL_END | ChannelDataFlags.CHANNEL_END - 1),
                SphynxFrameType.CHANNEL_RELEASE => Flags is 0 or ChannelReleaseFlags.CHANNEL_REJECTED && FrameSize == 0,
                _ => Flags == 0 && FrameSize == 0,
            };
        }

        public bool Equals(in SphynxFrameHeader other) => Version == other.Version
                                                          && FrameType == other.FrameType
                                                          && FrameSize == other.FrameSize
                                                          && Flags == other.Flags
                                                          && ChannelId == other.ChannelId;

        public bool Equals(SphynxFrameHeader other) => Equals(in other);

        public bool Equals(SphynxFrameHeader? other) => other.HasValue && Equals(other.Value);

        public override int GetHashCode() => HashCode.Combine(Version, FrameType, Flags, ChannelId, FrameSize);

        public override string ToString() =>
            "{ " +
            $"{nameof(Version)}: {Version}, " +
            $"{nameof(ChannelId)}: {ChannelId}, " +
            $"{nameof(FrameType)}: {FrameType}, " +
            $"{nameof(Flags)}: {Flags}, " +
            $"{nameof(FrameSize)}: {FrameSize}, " +
            "}";
    }
}
