﻿using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace Sphynx.Packet.Broadcast
{
    /// <inheritdoc cref="SphynxPacketType.CHAT_KICK_BCAST"/>
    public sealed class ChatKickBroadcastPacket : SphynxPacket, IEquatable<ChatKickBroadcastPacket>
    {
        /// <summary>
        /// Room ID of the room to kick the user from.
        /// </summary>
        public Guid RoomId { get; set; }

        /// <summary>
        /// User ID of the user that was kicked.
        /// </summary>
        public Guid KickedId { get; set; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.CHAT_KICK_BCAST;

        private const int ROOM_ID_OFFSET = 0;
        private static readonly int KICKED_ID_OFFSET = ROOM_ID_OFFSET + GUID_SIZE;

        /// <summary>
        /// Creates a new <see cref="ChatKickBroadcastPacket"/>.
        /// </summary>
        /// <param name="roomId">Room ID of the room to kick the user from.</param>
        /// <param name="kickedId">User ID of the user that was kicked.</param>
        public ChatKickBroadcastPacket(Guid roomId, Guid kickedId)
        {
            RoomId = roomId;
            KickedId = kickedId;
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="ChatKickBroadcastPacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        /// <param name="packet">The deserialized packet.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out ChatKickBroadcastPacket? packet)
        {
            int contentSize = KICKED_ID_OFFSET + GUID_SIZE;

            if (contents.Length < contentSize)
            {
                packet = null;
                return false;
            }

            var roomId = new Guid(contents.Slice(ROOM_ID_OFFSET, GUID_SIZE));
            var kickId = new Guid(contents.Slice(KICKED_ID_OFFSET, GUID_SIZE));
            packet = new ChatKickBroadcastPacket(roomId, kickId);
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
        public override bool TrySerialize(Stream stream)
        {
            if (!stream.CanWrite) return false;

            int contentSize = GUID_SIZE;

            int bufferSize = SphynxPacketHeader.HEADER_SIZE + contentSize;
            var rawBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            var buffer = rawBuffer.AsSpan()[..bufferSize];

            try
            {
                if (TrySerialize(buffer))
                {
                    stream.Write(buffer);
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
                KickedId.TryWriteBytes(buffer.Slice(KICKED_ID_OFFSET, GUID_SIZE));
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public bool Equals(ChatKickBroadcastPacket? other) => base.Equals(other) && RoomId == other?.RoomId && KickedId == other?.KickedId;
    }
}