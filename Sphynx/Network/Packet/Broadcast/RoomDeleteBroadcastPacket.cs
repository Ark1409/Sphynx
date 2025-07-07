using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Sphynx.Network.Transport;

namespace Sphynx.Network.Packet.Broadcast
{
    /// <inheritdoc cref="SphynxPacketType.CHAT_DEL_BCAST"/>
    public sealed class RoomDeleteBroadcastPacket : SphynxPacket, IEquatable<RoomDeleteBroadcastPacket>
    {
        /// <summary>
        /// Room ID of the deleted room.
        /// </summary>
        public Guid RoomId { get; set; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.CHAT_DEL_BCAST;

        private const int ROOM_ID_OFFSET = 0;

        /// <summary>
        /// Creates a new <see cref="RoomDeleteBroadcastPacket"/>.
        /// </summary>
        /// <param name="roomId">Room ID of the deleted room.</param>
        public RoomDeleteBroadcastPacket(Guid roomId)
        {
            RoomId = roomId;
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="RoomDeleteBroadcastPacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        /// <param name="packet">The deserialized packet.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out RoomDeleteBroadcastPacket? packet)
        {
            int contentSize = GUID_SIZE;

            if (contents.Length < contentSize)
            {
                packet = null;
                return false;
            }

            var roomId = new Guid(contents.Slice(ROOM_ID_OFFSET, GUID_SIZE));
            packet = new RoomDeleteBroadcastPacket(roomId);
            return true;
        }

        /// <inheritdoc/>
        public override bool TrySerialize([NotNullWhen(true)] out byte[]? packetBytes)
        {
            int contentSize = GUID_SIZE;
            int bufferSize = SphynxPacketHeader.Size + contentSize;

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

            int contentSize = GUID_SIZE;

            int bufferSize = SphynxPacketHeader.Size + contentSize;
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
                buffer = buffer[SphynxPacketHeader.Size..];
                RoomId.TryWriteBytes(buffer.Slice(ROOM_ID_OFFSET, GUID_SIZE));
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public bool Equals(RoomDeleteBroadcastPacket? other) => base.Equals(other) && RoomId == other?.RoomId;
    }
}
