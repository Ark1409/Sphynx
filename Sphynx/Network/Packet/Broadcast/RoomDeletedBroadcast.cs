using Sphynx.Core;

namespace Sphynx.Network.Packet.Broadcast
{
    /// <inheritdoc cref="SphynxPacketType.CHAT_DEL_BCAST"/>
    public sealed class RoomDeletedBroadcast : SphynxPacket, IEquatable<RoomDeletedBroadcast>
    {
        /// <summary>
        /// Room ID of the deleted room.
        /// </summary>
        public Guid RoomId { get; set; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.CHAT_DEL_BCAST;

        public RoomDeletedBroadcast()
        {
        }

        /// <summary>
        /// Creates a new <see cref="RoomDeletedBroadcast"/>.
        /// </summary>
        /// <param name="roomId">Room ID of the deleted room.</param>
        public RoomDeletedBroadcast(Guid roomId)
        {
            RoomId = roomId;
        }

        /// <inheritdoc/>
        public bool Equals(RoomDeletedBroadcast? other) => base.Equals(other) && RoomId == other?.RoomId;
    }
}
