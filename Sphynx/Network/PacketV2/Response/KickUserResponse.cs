using Sphynx.Core;

namespace Sphynx.Network.PacketV2.Response
{
    /// <inheritdoc cref="SphynxPacketType.ROOM_KICK_RES"/>
    public sealed class KickUserResponse : SphynxResponse, IEquatable<KickUserResponse>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.ROOM_KICK_RES;

        /// <summary>
        /// Creates a new <see cref="KickUserResponse"/>.
        /// </summary>
        /// <param name="errorCode">Error code for kick attempt.</param>
        public KickUserResponse(SphynxErrorCode errorCode = SphynxErrorCode.SUCCESS) : base(errorCode)
        {
        }

        /// <inheritdoc/>
        public bool Equals(KickUserResponse? other) => base.Equals(other);
    }
}
