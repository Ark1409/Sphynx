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
            : this(null!, roomId, password)
        {
        }

        /// <summary>
        /// Creates new <see cref="JoinRoomRequest"/>.
        /// </summary>
        /// <param name="accessToken">The JWT access token for this request.</param>
        public JoinRoomRequest(string accessToken) : base(accessToken)
        {
        }

        /// <summary>
        /// Creates a new <see cref="RoomCreateResponsePacket"/>.
        /// </summary>
        /// <param name="accessToken">The JWT access token for this request.</param>
        /// <param name="roomId">Room ID of the room to join.</param>
        /// <param name="password">Password for the room, if the room is guarded with a password.</param>
        public JoinRoomRequest(string accessToken, SnowflakeId roomId, string? password = null) : base(accessToken)
        {
            RoomId = roomId;
            Password = password;
        }

        /// <inheritdoc/>
        public bool Equals(JoinRoomRequest? other) => base.Equals(other) && RoomId == other?.RoomId && Password == other?.Password;
    }
}
