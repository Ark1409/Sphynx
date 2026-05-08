using Sphynx.Core;
using Sphynx.Network.Packet.Response;

namespace Sphynx.Network.Packet.Request
{
    public class LeaveRoomRequest : SphynxRequest<LeaveRoomResponse>, IEquatable<LeaveRoomRequest>
    {
        /// <summary>
        /// Room ID of the room to leave.
        /// </summary>
        public Guid RoomId { get; set; }

        /// <inheritdoc/>
        public override SphynxRequestType RequestType => SphynxRequestType.LEAVE_ROOM_REQ;

        public LeaveRoomRequest()
        {
        }

        public LeaveRoomRequest(Guid roomId)
        {
            RoomId = roomId;
        }

        /// <inheritdoc/>
        public bool Equals(LeaveRoomRequest? other) => base.Equals(other) && RoomId == other?.RoomId;

        public override LeaveRoomResponse CreateResponse(SphynxErrorInfo errorInfo) => new LeaveRoomResponse(errorInfo);
    }
}
