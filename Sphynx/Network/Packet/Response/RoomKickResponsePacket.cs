﻿using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Sphynx.Core;
using Sphynx.Network.Transport;

namespace Sphynx.Network.Packet.Response
{
    /// <inheritdoc cref="SphynxPacketType.ROOM_KICK_RES"/>
    public sealed class RoomKickResponsePacket : SphynxResponsePacket, IEquatable<RoomKickResponsePacket>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.ROOM_KICK_RES;

        /// <summary>
        /// Creates a new <see cref="RoomKickResponsePacket"/> with <see cref="SphynxErrorCode.SUCCESS"/>.
        /// </summary>
        public RoomKickResponsePacket() : this(SphynxErrorCode.SUCCESS)
        {
        }

        /// <summary>
        /// Creates a new <see cref="RoomKickResponsePacket"/>.
        /// </summary>
        /// <param name="errorCode">Error code for kick attempt.</param>
        public RoomKickResponsePacket(SphynxErrorCode errorCode) : base(errorCode)
        {
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="RoomLeaveResponsePacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        /// <param name="packet">The deserialized packet.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out RoomKickResponsePacket? packet)
        {
            if (TryDeserializeDefaults(contents, out SphynxErrorCode? errorCode))
            {
                packet = new RoomKickResponsePacket(errorCode.Value);
                return true;
            }

            packet = null;
            return false;
        }

        /// <inheritdoc/>
        public override bool TrySerialize([NotNullWhen(true)] out byte[]? packetBytes)
        {
            int contentSize = DEFAULT_CONTENT_SIZE;
            int bufferSize = SphynxPacketHeader.Size + contentSize;

            if (!TrySerializeHeader(packetBytes = new byte[bufferSize]) ||
                !TrySerializeDefaults(packetBytes.AsSpan()[SphynxPacketHeader.Size..]))
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
                if (TrySerializeHeader(buffer.Span) && TrySerializeDefaults(buffer.Span[SphynxPacketHeader.Size..]))
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

        /// <inheritdoc/>
        public bool Equals(RoomKickResponsePacket? other) => base.Equals(other);
    }
}