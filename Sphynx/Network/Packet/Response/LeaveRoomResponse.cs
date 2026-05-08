using Sphynx.Core;

namespace Sphynx.Network.Packet.Response
{
    public class LeaveRoomResponse : SphynxResponse, IEquatable<LeaveRoomResponse>
    {
        /// <inheritdoc/>
        public override SphynxRequestType ResponseType => SphynxRequestType.LEAVE_ROOM_REQ;

        public LeaveRoomResponse()
        {
        }

        public LeaveRoomResponse(SphynxErrorCode errorInfo = SphynxErrorCode.SUCCESS) : base(errorInfo)
        {
        }

        public LeaveRoomResponse(SphynxErrorInfo errorInfo) : base(errorInfo)
        {
        }

        /// <inheritdoc/>
        public bool Equals(LeaveRoomResponse? other) => base.Equals(other);
    }
}
