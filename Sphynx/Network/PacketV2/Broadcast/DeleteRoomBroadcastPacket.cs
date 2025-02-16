using Sphynx.Core;

namespace Sphynx.Network.PacketV2.Broadcast
{
    /// <inheritdoc cref="SphynxPacketType.CHAT_DEL_BCAST"/>
    public sealed class DeleteRoomBroadcastPacket : SphynxPacket, IEquatable<DeleteRoomBroadcastPacket>
    {
        /// <summary>
        /// Room ID of the deleted room.
        /// </summary>
        public SnowflakeId RoomId { get; init; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.CHAT_DEL_BCAST;

        /// <summary>
        /// Creates a new <see cref="DeleteRoomBroadcastPacket"/>.
        /// </summary>
        /// <param name="roomId">Room ID of the deleted room.</param>
        public DeleteRoomBroadcastPacket(SnowflakeId roomId)
        {
            RoomId = roomId;
        }

        /// <inheritdoc/>
        public bool Equals(DeleteRoomBroadcastPacket? other) => base.Equals(other) && RoomId == other?.RoomId;
    }
}
