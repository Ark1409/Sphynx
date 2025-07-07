using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Sphynx.Network.Transport;

namespace Sphynx.Network.Packet.Broadcast
{
    /// <inheritdoc cref="SphynxPacketType.CHAT_JOIN_BCAST"/>
    public sealed class RoomJoinBroadcastPacket : SphynxPacket, IEquatable<RoomJoinBroadcastPacket>
    {
        /// <summary>
        /// Room ID of the room the user has joined.
        /// </summary>
        public Guid RoomId { get; set; }

        /// <summary>
        /// The user ID of the user who joined the room.
        /// </summary>
        public Guid JoinerId { get; set; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.CHAT_JOIN_BCAST;

        private const int ROOM_ID_OFFSET = 0;
        private static readonly int JOINER_ID_OFFSET = ROOM_ID_OFFSET + GUID_SIZE;

        /// <summary>
        /// Creates a new <see cref="RoomJoinBroadcastPacket"/>.
        /// </summary>
        /// <param name="roomId">Room ID of the room the user has joined.</param>
        /// <param name="joinerId">The user ID of the user who joined the room.</param>
        public RoomJoinBroadcastPacket(Guid roomId, Guid joinerId)
        {
            RoomId = roomId;
            JoinerId = joinerId;
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="RoomJoinBroadcastPacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        /// <param name="packet">The deserialized packet.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out RoomJoinBroadcastPacket? packet)
        {
            int contentSize = GUID_SIZE + GUID_SIZE;

            if (contents.Length < contentSize)
            {
                packet = null;
                return false;
            }

            var roomId = new Guid(contents.Slice(ROOM_ID_OFFSET, GUID_SIZE));
            var joinerId = new Guid(contents.Slice(JOINER_ID_OFFSET, GUID_SIZE));
            packet = new RoomJoinBroadcastPacket(roomId, joinerId);
            return true;
        }

        /// <inheritdoc/>
        public override bool TrySerialize([NotNullWhen(true)] out byte[]? packetBytes)
        {
            int contentSize = GUID_SIZE + GUID_SIZE;
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

            int contentSize = GUID_SIZE + GUID_SIZE;

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
                JoinerId.TryWriteBytes(buffer.Slice(JOINER_ID_OFFSET, GUID_SIZE));
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public bool Equals(RoomJoinBroadcastPacket? other) => base.Equals(other) && RoomId == other?.RoomId && JoinerId == other?.JoinerId;
    }
}
