using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Sphynx.Network.Transport;

namespace Sphynx.Network.Packet.Request
{
    /// <inheritdoc cref="SphynxPacketType.ROOM_KICK_REQ"/>
    public sealed class RoomKickRequestPacket : SphynxRequestPacket, IEquatable<RoomKickRequestPacket>
    {
        /// <summary>
        /// Room ID of the room to kick the user from.
        /// </summary>
        public Guid RoomId { get; set; }

        /// <summary>
        /// User ID of the user to kick from the room.
        /// </summary>
        public Guid KickId { get; set; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.ROOM_KICK_REQ;

        private static readonly int ROOM_ID_OFFSET = DEFAULT_CONTENT_SIZE;
        private static readonly int KICK_ID_OFFSET = ROOM_ID_OFFSET + GUID_SIZE;

        /// <summary>
        /// Creates a new <see cref="RoomKickRequestPacket"/>.
        /// </summary>
        /// <param name="roomId">Room ID of the room to kick the user from.</param>
        /// <param name="kickId">User ID of the user to kick from the room.</param>
        public RoomKickRequestPacket(Guid roomId, Guid kickId) : this(Guid.Empty, Guid.Empty, roomId, kickId)
        {
        }

        /// <summary>
        /// Creates a new <see cref="RoomLeaveRequestPacket"/>.
        /// </summary>
        /// <param name="userId">The user ID of the requesting user.</param>
        /// <param name="sessionId">The session ID for the requesting user.</param>
        /// <param name="roomId">Room ID of the room to leave.</param>
        /// <param name="kickId">User ID of the user to kick from the room.</param>
        public RoomKickRequestPacket(Guid userId, Guid sessionId, Guid roomId, Guid kickId) : base(userId, sessionId)
        {
            RoomId = roomId;
            KickId = kickId;
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="RoomKickRequestPacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        /// <param name="packet">The deserialized packet.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out RoomKickRequestPacket? packet)
        {
            int contentSize = DEFAULT_CONTENT_SIZE + GUID_SIZE + GUID_SIZE; // roomId, kickId

            if (contents.Length < contentSize || !TryDeserializeDefaults(contents, out var userId, out var sessionId))
            {
                packet = null;
                return false;
            }

            var roomId = new Guid(contents.Slice(ROOM_ID_OFFSET, GUID_SIZE));
            var kickId = new Guid(contents.Slice(KICK_ID_OFFSET, GUID_SIZE));
            packet = new RoomKickRequestPacket(userId.Value, sessionId.Value, roomId, kickId);
            return true;
        }

        /// <inheritdoc/>
        public override bool TrySerialize([NotNullWhen(true)] out byte[]? packetBytes)
        {
            int contentSize = DEFAULT_CONTENT_SIZE + GUID_SIZE + GUID_SIZE;
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

            int contentSize = DEFAULT_CONTENT_SIZE + GUID_SIZE + GUID_SIZE;

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
            if (TrySerializeHeader(buffer) && TrySerializeDefaults(buffer = buffer[SphynxPacketHeader.Size..]))
            {
                RoomId.TryWriteBytes(buffer.Slice(ROOM_ID_OFFSET, GUID_SIZE));
                KickId.TryWriteBytes(buffer.Slice(KICK_ID_OFFSET, GUID_SIZE));
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public bool Equals(RoomKickRequestPacket? other) => base.Equals(other) && RoomId == other?.RoomId && KickId == other?.KickId;
    }
}