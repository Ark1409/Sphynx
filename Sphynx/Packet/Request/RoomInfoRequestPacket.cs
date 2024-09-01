using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace Sphynx.Packet.Request
{
    /// <inheritdoc cref="SphynxPacketType.ROOM_INFO_REQ"/>
    public sealed class RoomInfoRequestPacket : SphynxRequestPacket, IEquatable<RoomInfoRequestPacket>
    {
        /// <summary>
        /// The ID of the room to get the information of.
        /// </summary>
        public Guid RoomId { get; set; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.ROOM_INFO_REQ;

        private static readonly int ROOM_ID_OFFSET = DEFAULT_CONTENT_SIZE;

        /// <summary>
        /// Creates a new <see cref="RoomInfoRequestPacket"/>.
        /// </summary>
        /// <param name="roomId">The ID of the room to get the information of.</param>
        public RoomInfoRequestPacket(Guid roomId) : this(Guid.Empty, Guid.Empty, roomId)
        {
        }

        /// <summary>
        /// Creates a new <see cref="RoomInfoRequestPacket"/>.
        /// </summary>
        /// <param name="userId">The user ID of the requesting user.</param>
        /// <param name="sessionId">The session ID for the requesting user.</param>
        /// <param name="roomId">The ID of the room to get the information of.</param>
        public RoomInfoRequestPacket(Guid userId, Guid sessionId, Guid roomId) : base(userId, sessionId)
        {
            RoomId = roomId;
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="RoomInfoRequestPacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        /// <param name="packet">The deserialized packet.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out RoomInfoRequestPacket? packet)
        {
            int contentSize = DEFAULT_CONTENT_SIZE + GUID_SIZE;

            if (contents.Length < contentSize || !TryDeserializeDefaults(contents[..DEFAULT_CONTENT_SIZE], out var userId, out var sessionId))
            {
                packet = null;
                return false;
            }

            var roomId = new Guid(contents.Slice(ROOM_ID_OFFSET, GUID_SIZE));
            packet = new RoomInfoRequestPacket(userId.Value, sessionId.Value, roomId);
            return true;
        }

        /// <inheritdoc/>
        public override bool TrySerialize([NotNullWhen(true)] out byte[]? packetBytes)
        {
            int contentSize = DEFAULT_CONTENT_SIZE + GUID_SIZE;
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

            int contentSize = DEFAULT_CONTENT_SIZE + GUID_SIZE;

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
            catch
            {
                return false;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rawBuffer);
            }

            return false;
        }

        private bool TrySerialize(Span<byte> buffer)
        {
            if (TrySerializeHeader(buffer) && TrySerializeDefaults(buffer = buffer[SphynxPacketHeader.HEADER_SIZE..]))
            {
                RoomId.TryWriteBytes(buffer.Slice(ROOM_ID_OFFSET, GUID_SIZE));
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public bool Equals(RoomInfoRequestPacket? other) => base.Equals(other) && RoomId == other?.RoomId;
    }
}