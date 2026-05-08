namespace Sphynx.Network.Packet.Broadcast
{
    public class KickedMemberBroadcast : SphynxBroadcast, IEquatable<KickedMemberBroadcast>
    {
        /// <inheritdoc/>
        public override SphynxBroadcastType BroadcastType => SphynxBroadcastType.KICK_MEMBER_BCAST;

        /// <summary>
        /// Room ID of the room to kick the user from.
        /// </summary>
        public Guid RoomId { get; set; }

        /// <summary>
        /// User ID of the user that was kicked.
        /// </summary>
        public Guid MemberId { get; set; }

        public KickedMemberBroadcast()
        {
        }

        /// <summary>
        /// Creates a new <see cref="KickedMemberBroadcast"/>.
        /// </summary>
        /// <param name="roomId">Room ID of the room to kick the user from.</param>
        /// <param name="memberId">User ID of the user that was kicked.</param>
        public KickedMemberBroadcast(Guid roomId, Guid memberId)
        {
            RoomId = roomId;
            MemberId = memberId;
        }

        /// <inheritdoc/>
        public bool Equals(KickedMemberBroadcast? other) => base.Equals(other) && RoomId == other?.RoomId && MemberId == other?.MemberId;
    }
}
