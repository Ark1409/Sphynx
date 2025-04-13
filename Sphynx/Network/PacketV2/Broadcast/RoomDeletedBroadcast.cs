using Sphynx.Core;

namespace Sphynx.Network.PacketV2.Broadcast
{
    /// <inheritdoc cref="SphynxPacketType.CHAT_DEL_BCAST"/>
    public sealed class RoomDeletedBroadcast : SphynxPacket, IEquatable<RoomDeletedBroadcast>
    {
        /// <summary>
        /// Room ID of the deleted room.
        /// </summary>
        public SnowflakeId RoomId { get; init; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.CHAT_DEL_BCAST;

        /// <summary>
        /// Creates a new <see cref="RoomDeletedBroadcast"/>.
        /// </summary>
        /// <param name="roomId">Room ID of the deleted room.</param>
        public RoomDeletedBroadcast(SnowflakeId roomId)
        {
            RoomId = roomId;
        }

        /// <inheritdoc/>
        public bool Equals(RoomDeletedBroadcast? other) => base.Equals(other) && RoomId == other?.RoomId;
    }
}
