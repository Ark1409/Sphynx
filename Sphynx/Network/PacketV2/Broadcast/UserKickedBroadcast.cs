using Sphynx.Core;

namespace Sphynx.Network.PacketV2.Broadcast
{
    /// <inheritdoc cref="SphynxPacketType.CHAT_KICK_BCAST"/>
    public sealed class UserKickedBroadcast : SphynxPacket, IEquatable<UserKickedBroadcast>
    {
        /// <summary>
        /// Room ID of the room to kick the user from.
        /// </summary>
        public SnowflakeId RoomId { get; init; }

        /// <summary>
        /// User ID of the user that was kicked.
        /// </summary>
        public SnowflakeId KickId { get; init; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.CHAT_KICK_BCAST;

        /// <summary>
        /// Creates a new <see cref="UserKickedBroadcast"/>.
        /// </summary>
        /// <param name="roomId">Room ID of the room to kick the user from.</param>
        /// <param name="kickId">User ID of the user that was kicked.</param>
        public UserKickedBroadcast(SnowflakeId roomId, SnowflakeId kickId)
        {
            RoomId = roomId;
            KickId = kickId;
        }

        /// <inheritdoc/>
        public bool Equals(UserKickedBroadcast? other) =>
            base.Equals(other) && RoomId == other?.RoomId && KickId == other?.KickId;
    }
}
