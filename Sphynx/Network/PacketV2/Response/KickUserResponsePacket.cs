using Sphynx.Core;

namespace Sphynx.Network.PacketV2.Response
{
    /// <inheritdoc cref="SphynxPacketType.ROOM_KICK_RES"/>
    public sealed class KickUserResponsePacket : SphynxResponsePacket, IEquatable<KickUserResponsePacket>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.ROOM_KICK_RES;

        /// <summary>
        /// Creates a new <see cref="KickUserResponsePacket"/>.
        /// </summary>
        /// <param name="errorCode">Error code for kick attempt.</param>
        public KickUserResponsePacket(SphynxErrorCode errorCode = SphynxErrorCode.SUCCESS) : base(errorCode)
        {
        }

        /// <inheritdoc/>
        public bool Equals(KickUserResponsePacket? other) => base.Equals(other);
    }
}
