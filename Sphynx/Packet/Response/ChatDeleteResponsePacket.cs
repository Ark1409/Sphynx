namespace Sphynx.Packet.Response
{
    /// <inheritdoc cref="SphynxPacketType.CHAT_DEL_RES"/>
    public sealed class ChatDeleteResponsePacket : SphynxResponsePacket, IEquatable<ChatDeleteResponsePacket>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.CHAT_DEL_RES;

        /// <summary>
        /// Creates a <see cref="ChatDeleteResponsePacket"/> with <see cref="SphynxErrorCode.SUCCESS"/>.
        /// </summary>
        public ChatDeleteResponsePacket() : this(SphynxErrorCode.SUCCESS)
        {

        }

        /// <summary>
        /// Creates a new <see cref="ChatDeleteResponsePacket"/>.
        /// </summary>
        /// <param name="errorCode">Error code for attempted room deletion.</param>
        public ChatDeleteResponsePacket(SphynxErrorCode errorCode) : base(errorCode)
        {

        }

        /// <inheritdoc/>
        public override byte[] Serialize()
        {
            byte[] packetBytes = new byte[SphynxResponseHeader.HEADER_SIZE];
            var packetSpan = new Span<byte>(packetBytes);

            SerializeHeader(packetSpan, 0);

            return packetBytes;
        }

        /// <inheritdoc/>
        public bool Equals(ChatDeleteResponsePacket? other) => base.Equals(other);
    }
}
