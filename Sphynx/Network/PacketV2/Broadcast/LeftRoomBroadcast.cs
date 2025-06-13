using Sphynx.Core;

namespace Sphynx.Network.PacketV2.Broadcast
{
    /// <inheritdoc cref="SphynxPacketType.CHAT_JOIN_BCAST"/>
    public sealed class LeftRoomBroadcast : SphynxPacket, IEquatable<LeftRoomBroadcast>
    {
        /// <summary>
        /// Room ID of the room the user has left.
        /// </summary>
        public SnowflakeId RoomId { get; init; }

        /// <summary>
        /// The user ID of the user who left the room.
        /// </summary>
        public SnowflakeId LeaverId { get; init; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.CHAT_JOIN_BCAST;

        /// <summary>
        /// Creates a new <see cref="LeftRoomBroadcast"/>.
        /// </summary>
        /// <param name="roomId">Room ID of the room the user has left.</param>
        /// <param name="leaverId">The user ID of the user who left the room.</param>
        public LeftRoomBroadcast(SnowflakeId roomId, SnowflakeId leaverId)
        {
            RoomId = roomId;
            LeaverId = leaverId;
        }

        /// <inheritdoc/>
        public bool Equals(LeftRoomBroadcast? other) =>
            base.Equals(other) && RoomId == other?.RoomId && LeaverId == other?.LeaverId;
    }
}
