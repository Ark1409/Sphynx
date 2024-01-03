using System.Diagnostics.CodeAnalysis;

namespace Sphynx.Packet.Response
{
    /// <inheritdoc cref="SphynxPacketType.CHAT_JOIN_RES"/>
    public sealed class ChatJoinResponsePacket : SphynxResponsePacket, IEquatable<ChatJoinResponsePacket>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.CHAT_JOIN_RES;

        /// <summary>
        /// Creates a new <see cref="ChatJoinResponsePacket"/> with <see cref="SphynxErrorCode.SUCCESS"/>.
        /// </summary>
        public ChatJoinResponsePacket() : this(SphynxErrorCode.SUCCESS)
        {
        }

        /// <summary>
        /// Creates a new <see cref="ChatJoinResponsePacket"/>.
        /// </summary>
        /// <param name="errorCode">Error code for login attempt.</param>
        public ChatJoinResponsePacket(SphynxErrorCode errorCode) : base(errorCode)
        {
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="ChatJoinResponsePacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        /// <param name="packet">The deserialized packet.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out ChatJoinResponsePacket? packet)
        {
            if (TryDeserialize(contents, out SphynxErrorCode? errorCode))
            {
                packet = new ChatJoinResponsePacket(errorCode.Value);
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
        public bool Equals(ChatJoinResponsePacket? other) => base.Equals(other);
    }
}
