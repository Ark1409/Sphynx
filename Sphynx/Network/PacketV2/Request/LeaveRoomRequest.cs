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
        public LeaveRoomRequest(SnowflakeId roomId) : this(SnowflakeId.Empty, Guid.Empty, roomId)
        {
        }

        /// <summary>
        /// Creates new <see cref="LeaveRoomRequest"/>.
        /// </summary>
        /// <param name="userId">The user ID of the requesting user.</param>
        /// <param name="sessionId">The session ID for the requesting user.</param>
        public LeaveRoomRequest(SnowflakeId userId, Guid sessionId) : base(userId, sessionId)
        {
        }

        /// <summary>
        /// Creates a new <see cref="LeaveRoomRequest"/>.
        /// </summary>
        /// <param name="userId">The user ID of the requesting user.</param>
        /// <param name="sessionId">The session ID for the requesting user.</param>
        /// <param name="roomId">Room ID of the room to leave.</param>
        public LeaveRoomRequest(SnowflakeId userId, Guid sessionId, SnowflakeId roomId) : base(userId, sessionId)
        {
            RoomId = roomId;
        }

        /// <inheritdoc/>
        public bool Equals(LeaveRoomRequest? other) => base.Equals(other) && RoomId == other?.RoomId;
    }
}
