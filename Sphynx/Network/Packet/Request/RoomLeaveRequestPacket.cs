﻿using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Sphynx.Network.Transport;

namespace Sphynx.Network.Packet.Request
{
    /// <inheritdoc cref="SphynxPacketType.ROOM_LEAVE_REQ"/>
    public sealed class RoomLeaveRequestPacket : SphynxRequestPacket, IEquatable<RoomLeaveRequestPacket>
    {
        /// <summary>
        /// Room ID of the room to leave.
        /// </summary>
        public Guid RoomId { get; set; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.ROOM_LEAVE_REQ;

        private static readonly int ROOM_ID_OFFSET = DEFAULT_CONTENT_SIZE;

        /// <summary>
        /// Creates a new <see cref="RoomLeaveRequestPacket"/>.
        /// </summary>
        /// <param name="roomId">Room ID of the room to leave.</param>
        public RoomLeaveRequestPacket(Guid roomId) : this(Guid.Empty, Guid.Empty, roomId)
        {
        }

        /// <summary>
        /// Creates a new <see cref="RoomLeaveRequestPacket"/>.
        /// </summary>
        /// <param name="userId">The user ID of the requesting user.</param>
        /// <param name="sessionId">The session ID for the requesting user.</param>
        /// <param name="roomId">Room ID of the room to leave.</param>
        public RoomLeaveRequestPacket(Guid userId, Guid sessionId, Guid roomId) : base(userId, sessionId)
        {
            RoomId = roomId;
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="RoomLeaveRequestPacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        /// <param name="packet">The deserialized packet.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out RoomLeaveRequestPacket? packet)
        {
            int contentSize = DEFAULT_CONTENT_SIZE + GUID_SIZE;

            if (contents.Length < contentSize || !TryDeserializeDefaults(contents, out var userId, out var sessionId))
            {
                packet = null;
                return false;
            }

            var roomId = new Guid(contents.Slice(ROOM_ID_OFFSET, GUID_SIZE));
            packet = new RoomLeaveRequestPacket(userId.Value, sessionId.Value, roomId);
            return true;
        }

        /// <inheritdoc/>
        public override bool TrySerialize([NotNullWhen(true)] out byte[]? packetBytes)
        {
            int contentSize = DEFAULT_CONTENT_SIZE + GUID_SIZE;
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

            int contentSize = DEFAULT_CONTENT_SIZE + GUID_SIZE;

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
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public bool Equals(RoomLeaveRequestPacket? other) => base.Equals(other) && RoomId == other?.RoomId;
    }
}