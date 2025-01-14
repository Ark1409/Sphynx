using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace Sphynx.Network.PacketV2.Broadcast
{
    /// <inheritdoc cref="SphynxPacketType.CHAT_JOIN_BCAST"/>
    public sealed class RoomLeaveBroadcastPacket : SphynxPacket, IEquatable<RoomLeaveBroadcastPacket>
    {
        /// <summary>
        /// Room ID of the room the user has left.
        /// </summary>
        public Guid RoomId { get; set; }

        /// <summary>
        /// The user ID of the user who left the room.
        /// </summary>
        public Guid LeaverId { get; set; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.CHAT_JOIN_BCAST;

        private const int ROOM_ID_OFFSET = 0;
        private static readonly int LEAVER_ID_OFFSET = ROOM_ID_OFFSET + GUID_SIZE;

        /// <summary>
        /// Creates a new <see cref="RoomLeaveBroadcastPacket"/>.
        /// </summary>
        /// <param name="roomId">Room ID of the room the user has left.</param>
        /// <param name="leaverId">The user ID of the user who left the room.</param>
        public RoomLeaveBroadcastPacket(Guid roomId, Guid leaverId)
        {
            RoomId = roomId;
            LeaverId = leaverId;
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="RoomLeaveBroadcastPacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        /// <param name="packet">The deserialized packet.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out RoomLeaveBroadcastPacket? packet)
        {
            int contentSize = LEAVER_ID_OFFSET + GUID_SIZE;

            if (contents.Length < contentSize)
            {
                packet = null;
                return false;
            }

            var roomId = new Guid(contents.Slice(ROOM_ID_OFFSET, GUID_SIZE));
            var leaverId = new Guid(contents.Slice(LEAVER_ID_OFFSET, GUID_SIZE));
            packet = new RoomLeaveBroadcastPacket(roomId, leaverId);
            return true;
        }

        /// <inheritdoc/>
        public override bool TrySerialize([NotNullWhen(true)] out byte[]? packetBytes)
        {
            int contentSize = GUID_SIZE + GUID_SIZE;
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

            int contentSize = GUID_SIZE + GUID_SIZE;

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
                RoomId.TryWriteBytes(buffer.Slice(ROOM_ID_OFFSET, GUID_SIZE));
                LeaverId.TryWriteBytes(buffer.Slice(LEAVER_ID_OFFSET, GUID_SIZE));
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public bool Equals(RoomLeaveBroadcastPacket? other) => base.Equals(other) && RoomId == other?.RoomId && LeaverId == other?.LeaverId;
    }
}
