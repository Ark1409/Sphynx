using Sphynx.Core;

namespace Sphynx.Network.PacketV2.Request
{
    /// <inheritdoc cref="SphynxPacketType.ROOM_KICK_REQ"/>
    public sealed class KickUserRequest : SphynxRequest, IEquatable<KickUserRequest>
    {
        /// <summary>
        /// Room ID of the room to kick the user from.
        /// </summary>
        public SnowflakeId RoomId { get; init; }

        /// <summary>
        /// User ID of the user to kick from the room.
        /// </summary>
        public SnowflakeId KickId { get; init; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.ROOM_KICK_REQ;

        /// <summary>
        /// Creates a new <see cref="KickUserRequest"/>.
        /// </summary>
        /// <param name="roomId">Room ID of the room to kick the user from.</param>
        /// <param name="kickId">User ID of the user to kick from the room.</param>
        public KickUserRequest(SnowflakeId roomId, SnowflakeId kickId) : this(null!, roomId, kickId)
        {
        }

        /// <summary>
        /// Creates new <see cref="KickUserRequest"/>.
        /// </summary>
        /// <param name="accessToken">The JWT access token for this request.</param>
        public KickUserRequest(string accessToken) : base(accessToken)
        {
        }

        /// <summary>
        /// Creates a new <see cref="LeaveRoomRequest"/>.
        /// </summary>
        /// <param name="accessToken">The JWT access token for this request.</param>
        /// <param name="sessionId">The session ID for the requesting user.</param>
        /// <param name="roomId">Room ID of the room to leave.</param>
        /// <param name="kickId">User ID of the user to kick from the room.</param>
        public KickUserRequest(string accessToken, SnowflakeId roomId, SnowflakeId kickId) : base(accessToken)
        {
            RoomId = roomId;
            KickId = kickId;
        }

        /// <inheritdoc/>
        public bool Equals(KickUserRequest? other) => base.Equals(other) && RoomId == other?.RoomId && KickId == other?.KickId;
    }
}
