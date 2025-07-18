using Sphynx.Core;

namespace Sphynx.Network.PacketV2.Request
{
    /// <inheritdoc cref="SphynxPacketType.ROOM_LEAVE_REQ"/>
    public sealed class LeaveRoomRequest : SphynxRequest, IEquatable<LeaveRoomRequest>
    {
        /// <summary>
        /// Room ID of the room to leave.
        /// </summary>
        public SnowflakeId RoomId { get; init; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.ROOM_LEAVE_REQ;

        /// <summary>
        /// Creates a new <see cref="LeaveRoomRequest"/>.
        /// </summary>
        /// <param name="roomId">Room ID of the room to leave.</param>
        public LeaveRoomRequest(SnowflakeId roomId) : this(null!, roomId)
        {
        }

        /// <summary>
        /// Creates new <see cref="LeaveRoomRequest"/>.
        /// </summary>
        /// <param name="accessToken">The JWT access token for this request.</param>
        public LeaveRoomRequest(string accessToken) : base(accessToken)
        {
        }

        /// <summary>
        /// Creates a new <see cref="LeaveRoomRequest"/>.
        /// </summary>
        /// <param name="accessToken">The JWT access token for this request.</param>
        /// <param name="roomId">Room ID of the room to leave.</param>
        public LeaveRoomRequest(string accessToken, SnowflakeId roomId) : base(accessToken)
        {
            RoomId = roomId;
        }

        /// <inheritdoc/>
        public bool Equals(LeaveRoomRequest? other) => base.Equals(other) && RoomId == other?.RoomId;
    }
}
