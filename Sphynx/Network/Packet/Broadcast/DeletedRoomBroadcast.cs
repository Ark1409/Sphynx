using Sphynx.Core;

namespace Sphynx.Network.Packet.Broadcast
{
    public class DeletedRoomBroadcast : SphynxBroadcast, IEquatable<DeletedRoomBroadcast>
    {
        /// <summary>
        /// Room ID of the deleted room.
        /// </summary>
        public Guid RoomId { get; set; }

        /// <inheritdoc/>
        public override SphynxBroadcastType BroadcastType => SphynxBroadcastType.DEL_ROOM_BCAST;

        public DeletedRoomBroadcast()
        {
        }

        public DeletedRoomBroadcast(Guid roomId)
        {
            RoomId = roomId;
        }

        /// <inheritdoc/>
        public bool Equals(DeletedRoomBroadcast? other) => base.Equals(other) && RoomId == other?.RoomId;
    }
}
