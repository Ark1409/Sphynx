namespace Sphynx.Network.Packet.Broadcast
{
    public sealed class LeftRoomBroadcast : SphynxBroadcast, IEquatable<LeftRoomBroadcast>
    {
        /// <inheritdoc/>
        public override SphynxBroadcastType BroadcastType => SphynxBroadcastType.LEAVE_ROOM_BCAST;

        /// <summary>
        /// Room ID of the room the user has left.
        /// </summary>
        public Guid RoomId { get; set; }

        /// <summary>
        /// The user ID of the user who left the room.
        /// </summary>
        public Guid LeaverId { get; set; }

        public LeftRoomBroadcast()
        {
        }

        /// <summary>
        /// Creates a new <see cref="LeftRoomBroadcast"/>.
        /// </summary>
        /// <param name="roomId">Room ID of the room the user has left.</param>
        /// <param name="leaverId">The user ID of the user who left the room.</param>
        public LeftRoomBroadcast(Guid roomId, Guid leaverId)
        {
            RoomId = roomId;
            LeaverId = leaverId;
        }

        /// <inheritdoc/>
        public bool Equals(LeftRoomBroadcast? other) => base.Equals(other) && RoomId == other?.RoomId && LeaverId == other?.LeaverId;
    }
}
