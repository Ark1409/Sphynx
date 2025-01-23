using Sphynx.Core;

namespace Sphynx.Network.PacketV2.Response
{
    /// <inheritdoc cref="SphynxPacketType.MSG_RES"/>
    public sealed class SendMessageResponsePacket : SphynxResponsePacket, IEquatable<SendMessageResponsePacket>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.MSG_RES;

        /// <summary>
        /// Creates a new <see cref="SendMessageResponsePacket"/> with <see cref="SphynxErrorCode.SUCCESS"/>.
        /// </summary>
        public SendMessageResponsePacket() : this(SphynxErrorCode.SUCCESS)
        {
        }

        /// <summary>
        /// Creates a new <see cref="SendMessageResponsePacket"/>.
        /// </summary>
        /// <param name="errorCode">Error code for message attempt.</param>
        public SendMessageResponsePacket(SphynxErrorCode errorCode) : base(errorCode)
        {
        }

        /// <inheritdoc/>
        public bool Equals(SendMessageResponsePacket? other) => base.Equals(other);
    }
}
