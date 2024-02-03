using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace Sphynx.Packet.Response
{
    /// <inheritdoc cref="SphynxPacketType.CHAT_KICK_RES"/>
    public sealed class ChatKickResponsePacket : SphynxResponsePacket, IEquatable<ChatKickResponsePacket>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.CHAT_KICK_RES;

        /// <summary>
        /// Creates a new <see cref="ChatKickResponsePacket"/> with <see cref="SphynxErrorCode.SUCCESS"/>.
        /// </summary>
        public ChatKickResponsePacket() : this(SphynxErrorCode.SUCCESS)
        {
        }

        /// <summary>
        /// Creates a new <see cref="ChatKickResponsePacket"/>.
        /// </summary>
        /// <param name="errorCode">Error code for kick attempt.</param>
        public ChatKickResponsePacket(SphynxErrorCode errorCode) : base(errorCode)
        {
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="ChatLeaveResponsePacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        /// <param name="packet">The deserialized packet.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out ChatKickResponsePacket? packet)
        {
            if (TryDeserializeDefaults(contents, out SphynxErrorCode? errorCode))
            {
                packet = new ChatKickResponsePacket(errorCode.Value);
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

            if (!TrySerializeHeader(packetBytes = new byte[bufferSize]) || !TrySerializeDefaults(packetBytes.AsSpan()[SphynxPacketHeader.HEADER_SIZE..]))
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
        public bool Equals(ChatKickResponsePacket? other) => base.Equals(other);
    }
}
