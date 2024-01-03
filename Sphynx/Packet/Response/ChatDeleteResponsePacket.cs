﻿using System.Diagnostics.CodeAnalysis;

namespace Sphynx.Packet.Response
{
    /// <inheritdoc cref="SphynxPacketType.CHAT_DEL_RES"/>
    public sealed class ChatDeleteResponsePacket : SphynxResponsePacket, IEquatable<ChatDeleteResponsePacket>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.CHAT_DEL_RES;

        /// <summary>
        /// Creates a new <see cref="ChatDeleteResponsePacket"/> with <see cref="SphynxErrorCode.SUCCESS"/>.
        /// </summary>
        public ChatDeleteResponsePacket() : this(SphynxErrorCode.SUCCESS)
        {

        }

        /// <summary>
        /// Creates a new <see cref="ChatDeleteResponsePacket"/>.
        /// </summary>
        /// <param name="errorCode">Error code for login attempt.</param>
        public ChatDeleteResponsePacket(SphynxErrorCode errorCode) : base(errorCode)
        {

        }

        /// <summary>
        /// Attempts to deserialize a <see cref="ChatDeleteResponsePacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        /// <param name="packet">The deserialized packet.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out ChatDeleteResponsePacket? packet)
        {
            if (TryDeserialize(contents, out SphynxErrorCode? errorCode))
            {
                packet = new ChatDeleteResponsePacket(errorCode.Value);
                return true;
            }

            packet = null;
            return false;
        }

        /// <inheritdoc/>
        public override bool TrySerialize([NotNullWhen(true)] out byte[]? packetBytes)
        {
            int contentSize = sizeof(SphynxErrorCode);

            packetBytes = new byte[contentSize];
            var packetSpan = new Span<byte>(packetBytes);

            if (TrySerializeHeader(packetSpan, contentSize) && TrySerialize(packetSpan[SphynxPacketHeader.HEADER_SIZE..]))
            {
                return true;
            }

            packetBytes = null;
            return false;
        }

        /// <inheritdoc/>
        public bool Equals(ChatDeleteResponsePacket? other) => base.Equals(other);
    }
}
