using System.Diagnostics.CodeAnalysis;

namespace Sphynx.Packet.Response
{
    /// <inheritdoc cref="SphynxPacketType.CHAT_SELECT_RES"/>
    public sealed class ChatSelectResponsePacket : SphynxResponsePacket, IEquatable<ChatSelectResponsePacket>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.CHAT_SELECT_RES;

        /// <summary>
        /// Creates a new <see cref="ChatSelectResponsePacket"/>.
        /// </summary>
        /// <param name="errorCode">The error code for the response packet.</param>
        public ChatSelectResponsePacket(SphynxErrorCode errorCode) : base(errorCode)
        {
        }

        /// <inheritdoc/>
        public override bool TrySerialize([NotNullWhen(true)] out byte[]? packetBytes)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public bool Equals(ChatSelectResponsePacket? other) => base.Equals(other);
    }
}
