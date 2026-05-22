namespace Sphynx.Network.Packet.Broadcast
{
    public class JoinedRoomBroadcast : SphynxBroadcast, IEquatable<JoinedRoomBroadcast>
    {
        /// <inheritdoc/>
        public override SphynxBroadcastType BroadcastType => SphynxBroadcastType.JOIN_ROOM_BCAST;

        /// <summary>
        /// Room ID of the room the user has joined.
        /// </summary>
        public Guid RoomId { get; set; }

        /// <summary>
        /// The user ID of the user who joined the room.
        /// </summary>
        public Guid JoinerId { get; set; }

        public JoinedRoomBroadcast()
        {
        }

        /// <summary>
        /// Creates a new <see cref="JoinedRoomBroadcast"/>.
        /// </summary>
        /// <param name="roomId">Room ID of the room the user has joined.</param>
        /// <param name="joinerId">The user ID of the user who joined the room.</param>
        public JoinedRoomBroadcast(Guid roomId, Guid joinerId)
        {
            RoomId = roomId;
            JoinerId = joinerId;
        }

        /// <inheritdoc/>
        public bool Equals(JoinedRoomBroadcast? other) => base.Equals(other) && RoomId == other?.RoomId && JoinerId == other?.JoinerId;
    }
}
