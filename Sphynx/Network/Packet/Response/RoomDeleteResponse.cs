using Sphynx.Core;

namespace Sphynx.Network.Packet.Response
{
    public class RoomDeleteResponse : SphynxResponse, IEquatable<RoomDeleteResponse>
    {
        /// <inheritdoc/>
        public override SphynxRequestType ResponseType => SphynxRequestType.DEL_ROOM_REQ;

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
