using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace Sphynx.Packet.Response
{
    /// <inheritdoc cref="SphynxPacketType.CHAT_JOIN_RES"/>
    public sealed class ChatJoinResponsePacket : SphynxResponsePacket, IEquatable<ChatJoinResponsePacket>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.CHAT_JOIN_RES;

        /// <summary>
        /// Creates a new <see cref="ChatJoinResponsePacket"/> with <see cref="SphynxErrorCode.SUCCESS"/>.
        /// </summary>
        public ChatJoinResponsePacket() : this(SphynxErrorCode.SUCCESS)
        {
        }

        /// <summary>
        /// Creates a new <see cref="ChatJoinResponsePacket"/>.
        /// </summary>
        /// <param name="errorCode">Error code for login attempt.</param>
        public ChatJoinResponsePacket(SphynxErrorCode errorCode) : base(errorCode)
        {
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="ChatJoinResponsePacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        /// <param name="packet">The deserialized packet.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out ChatJoinResponsePacket? packet)
        {
            if (TryDeserializeDefaults(contents, out SphynxErrorCode? errorCode))
            {
                packet = new ChatJoinResponsePacket(errorCode.Value);
                return true;
            }

            packet = null;
            return false;
        }

        /// <inheritdoc/>
        public override bool TrySerialize([NotNullWhen(true)] out byte[]? packetBytes)
        {
            int contentSize = DEFAULT_CONTENT_SIZE;
            int bufferSize = SphynxPacketHeader.HEADER_SIZE + contentSize;

            if (!TrySerializeHeader(packetBytes = new byte[bufferSize]) && TrySerializeDefaults(packetBytes.AsSpan()[SphynxPacketHeader.HEADER_SIZE..]))
            {
                packetBytes = null;
                return false;
            }

            return true;
        }

        /// <inheritdoc/>
        public override async Task<bool> TrySerializeAsync(Stream stream)
        {
            if (!stream.CanWrite) return false;

            int contentSize = DEFAULT_CONTENT_SIZE + GUID_SIZE;

            int bufferSize = SphynxPacketHeader.HEADER_SIZE + contentSize;
            var rawBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            var buffer = rawBuffer.AsMemory()[..bufferSize];

            try
            {
                if (TrySerializeHeader(buffer.Span) && TrySerializeDefaults(buffer.Span[SphynxPacketHeader.HEADER_SIZE..]))
                {
                    await stream.WriteAsync(buffer);
                    return true;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rawBuffer);
            }

            return false;
        }

        /// <inheritdoc/>
        public bool Equals(ChatJoinResponsePacket? other) => base.Equals(other);
    }
}
