using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace Sphynx.Packet.Response
{
    /// <inheritdoc cref="SphynxPacketType.LOGIN_RES"/>
    public sealed class LoginResponsePacket : SphynxResponsePacket, IEquatable<LoginResponsePacket>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.LOGIN_RES;

        /// <summary>
        /// Creates a new <see cref="LoginResponsePacket"/> with <see cref="SphynxErrorCode.SUCCESS"/>.
        /// </summary>
        public LoginResponsePacket() : this(SphynxErrorCode.SUCCESS)
        {
        }

        /// <summary>
        /// Creates a new <see cref="LoginResponsePacket"/>.
        /// </summary>
        /// <param name="errorCode">Error code for login attempt.</param>
        public LoginResponsePacket(SphynxErrorCode errorCode) : base(errorCode)
        {
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="LoginResponsePacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        /// <param name="packet">The deserialized packet.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out LoginResponsePacket? packet)
        {
            if (TryDeserializeDefaults(contents, out SphynxErrorCode? errorCode))
            {
                packet = new LoginResponsePacket(errorCode.Value);
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
        public bool Equals(LoginResponsePacket? other) => base.Equals(other);
    }
}
