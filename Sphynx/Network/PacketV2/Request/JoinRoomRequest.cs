using Sphynx.Core;
using Sphynx.Network.Packet.Response;

namespace Sphynx.Network.PacketV2.Request
{
    /// <inheritdoc cref="SphynxPacketType.ROOM_JOIN_REQ"/>
    public sealed class JoinRoomRequest : SphynxRequest, IEquatable<JoinRoomRequest>
    {
        /// <summary>
        /// Room ID of the room to join.
        /// </summary>
        public SnowflakeId RoomId { get; init; }

        /// <summary>
        /// Password for the room, if the room is guarded with a password.
        /// </summary>
        public string? Password { get; set; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.ROOM_JOIN_REQ;

        /// <summary>
        /// Creates a new <see cref="RoomCreateResponsePacket"/>.
        /// </summary>
        /// <param name="roomId">Room ID of the room to join.</param>
        /// <param name="password">Password for the room, if the room is guarded with a password.</param>
        public JoinRoomRequest(SnowflakeId roomId, string? password = null)
            : this(SnowflakeId.Empty, Guid.Empty, roomId, password)
        {
        }

        /// <summary>
        /// Creates new <see cref="JoinRoomRequest"/>.
        /// </summary>
        /// <param name="userId">The user ID of the requesting user.</param>
        /// <param name="sessionId">The session ID for the requesting user.</param>
        public JoinRoomRequest(SnowflakeId userId, Guid sessionId) : base(userId, sessionId)
        {
        }

        /// <summary>
        /// Creates a new <see cref="RoomCreateResponsePacket"/>.
        /// </summary>
        /// <param name="userId">The user ID of the requesting user.</param>
        /// <param name="sessionId">The session ID for the requesting user.</param>
        /// <param name="roomId">Room ID of the room to join.</param>
        /// <param name="password">Password for the room, if the room is guarded with a password.</param>
        public JoinRoomRequest(
            SnowflakeId userId,
            Guid sessionId,
            SnowflakeId roomId,
            string? password = null) : base(userId, sessionId)
        {
            RoomId = roomId;
            Password = password;
        }

        /// <inheritdoc/>
        public bool Equals(JoinRoomRequest? other) =>
            base.Equals(other) && RoomId == other?.RoomId && Password == other?.Password;
    }
}
