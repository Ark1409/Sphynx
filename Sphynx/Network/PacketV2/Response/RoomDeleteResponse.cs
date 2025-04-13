using Sphynx.Core;

namespace Sphynx.Network.PacketV2.Response
{
    /// <inheritdoc cref="SphynxPacketType.ROOM_DEL_RES"/>
    public sealed class RoomDeleteResponse : SphynxResponse, IEquatable<RoomDeleteResponse>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.ROOM_DEL_RES;

        /// <summary>
        /// Creates a new <see cref="RoomDeleteResponse"/>.
        /// </summary>
        /// <param name="errorCode">Error code for delete attempt.</param>
        public RoomDeleteResponse(SphynxErrorCode errorCode = SphynxErrorCode.SUCCESS) : base(errorCode)
        {
        }

        /// <inheritdoc/>
        public bool Equals(RoomDeleteResponse? other) => base.Equals(other);
    }
}
