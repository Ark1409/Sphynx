using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace Sphynx.Network.Packet.Broadcast
{
    /// <inheritdoc cref="SphynxPacketType.LOGOUT_BCAST"/>
    public sealed class LogoutBroadcastPacket : SphynxPacket, IEquatable<LogoutBroadcastPacket>
    {
        /// <summary>
        /// User ID of the user who went offline.
        /// </summary>
        public Guid UserId { get; set; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.LOGOUT_BCAST;

        private const int USER_ID_OFFSET = 0;

        /// <summary>
        /// Creates a new <see cref="LogoutBroadcastPacket"/>.
        /// </summary>
        /// <param name="userId">User ID of the user who went offline.</param>
        public LogoutBroadcastPacket(Guid userId)
        {
            UserId = userId;
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="LogoutBroadcastPacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        /// <param name="packet">The deserialized packet.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out LogoutBroadcastPacket? packet)
        {
            int contentSize = GUID_SIZE; // UserId

            if (contents.Length < contentSize)
            {
                packet = null;
                return false;
            }

            var userId = new Guid(contents.Slice(USER_ID_OFFSET, GUID_SIZE));
            packet = new LogoutBroadcastPacket(userId);
            return true;
        }

        /// <inheritdoc/>
        public override bool TrySerialize([NotNullWhen(true)] out byte[]? packetBytes)
        {
            int contentSize = GUID_SIZE; // UserId
            int bufferSize = SphynxPacketHeader.HEADER_SIZE + contentSize;

            if (!TrySerialize(packetBytes = new byte[bufferSize]))
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

            int contentSize = GUID_SIZE; // UserId

            int bufferSize = SphynxPacketHeader.HEADER_SIZE + contentSize;
            byte[] rawBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            var buffer = rawBuffer.AsMemory()[..bufferSize];

            try
            {
                if (TrySerialize(buffer.Span))
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

        private bool TrySerialize(Span<byte> buffer)
        {
            if (TrySerializeHeader(buffer))
            {
                buffer = buffer[SphynxPacketHeader.HEADER_SIZE..];
                UserId.TryWriteBytes(buffer.Slice(USER_ID_OFFSET, GUID_SIZE));
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public bool Equals(LogoutBroadcastPacket? other) => base.Equals(other) && UserId == other?.UserId;
    }
}