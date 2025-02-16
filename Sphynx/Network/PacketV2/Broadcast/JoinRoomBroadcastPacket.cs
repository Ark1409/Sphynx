using Sphynx.Core;

namespace Sphynx.Network.PacketV2.Broadcast
{
    /// <inheritdoc cref="SphynxPacketType.CHAT_JOIN_BCAST"/>
    public sealed class JoinRoomBroadcastPacket : SphynxPacket, IEquatable<JoinRoomBroadcastPacket>
    {
        /// <summary>
        /// Room ID of the room the user has joined.
        /// </summary>
        public SnowflakeId RoomId { get; set; }

        /// <summary>
        /// The user ID of the user who joined the room.
        /// </summary>
        public SnowflakeId JoinerId { get; set; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.CHAT_JOIN_BCAST;

        /// <summary>
        /// Creates a new <see cref="JoinRoomBroadcastPacket"/>.
        /// </summary>
        /// <param name="roomId">Room ID of the room the user has joined.</param>
        /// <param name="joinerId">The user ID of the user who joined the room.</param>
        public JoinRoomBroadcastPacket(SnowflakeId roomId, SnowflakeId joinerId)
        {
            RoomId = roomId;
            JoinerId = joinerId;
        }

        /// <inheritdoc/>
        public bool Equals(JoinRoomBroadcastPacket? other) =>
            base.Equals(other) && RoomId == other?.RoomId && JoinerId == other?.JoinerId;
    }
}
