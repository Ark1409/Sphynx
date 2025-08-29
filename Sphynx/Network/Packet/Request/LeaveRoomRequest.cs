using Sphynx.Core;
using Sphynx.Network.Packet.Response;

namespace Sphynx.Network.Packet.Request
{
    /// <inheritdoc cref="SphynxPacketType.ROOM_LEAVE_REQ"/>
    public sealed class LeaveRoomRequest : SphynxRequest<LeaveRoomResponse>, IEquatable<LeaveRoomRequest>
    {
        /// <summary>
        /// Room ID of the room to leave.
        /// </summary>
        public Guid RoomId { get; set; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.ROOM_LEAVE_REQ;

        /// <summary>
        /// Creates a new <see cref="LeaveRoomRequest"/>.
        /// </summary>
        /// <param name="roomId">Room ID of the room to leave.</param>
        public LeaveRoomRequest(Guid roomId) : this(default, roomId)
        {
        }

        /// <summary>
        /// Creates a new <see cref="LeaveRoomRequest"/>.
        /// </summary>
        /// <param name="roomId">Room ID of the room to leave.</param>
        public LeaveRoomRequest(Guid sessionId, Guid roomId) : base(sessionId)
        {
            RoomId = roomId;
        }

        /// <inheritdoc/>
        public bool Equals(LeaveRoomRequest? other) => base.Equals(other) && RoomId == other?.RoomId;

        public override LeaveRoomResponse CreateResponse(SphynxErrorInfo errorInfo) => new LeaveRoomResponse(errorInfo)
        {
            RequestTag = RequestTag
        };
    }
}
