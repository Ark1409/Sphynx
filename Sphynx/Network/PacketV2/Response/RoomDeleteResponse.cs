using Sphynx.Core;

namespace Sphynx.Network.PacketV2.Response
{
    /// <inheritdoc cref="SphynxPacketType.ROOM_DEL_RES"/>
    public sealed class RoomDeleteResponse : SphynxResponse, IEquatable<RoomDeleteResponse>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.ROOM_DEL_RES;

        public RoomDeleteResponse()
        {
        }

        /// <summary>
        /// Creates a new <see cref="RoomDeleteResponse"/>.
        /// </summary>
        /// <param name="errorInfo">Error code for delete attempt.</param>
        public RoomDeleteResponse(SphynxErrorCode errorInfo = SphynxErrorCode.SUCCESS) : base(errorInfo)
        {
        }

        /// <summary>
        /// Creates a new <see cref="RoomDeleteResponse"/>.
        /// </summary>
        /// <param name="errorInfo">Error code for delete attempt.</param>
        public RoomDeleteResponse(SphynxErrorInfo errorInfo) : base(errorInfo)
        {
        }

        /// <inheritdoc/>
        public bool Equals(RoomDeleteResponse? other) => base.Equals(other);
    }
}
