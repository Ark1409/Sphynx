using Sphynx.Core;
using Sphynx.Network.Packet.Response;

namespace Sphynx.Network.Packet.Request
{
    /// <inheritdoc cref="SphynxPacketType.ROOM_JOIN_REQ"/>
    public sealed class JoinRoomRequest : SphynxRequest<JoinRoomResponse>, IEquatable<JoinRoomRequest>
    {
        /// <summary>
        /// Room ID of the room to join.
        /// </summary>
        public Guid RoomId { get; set; }

        /// <summary>
        /// Password for the room, if the room is guarded with a password.
        /// </summary>
        public string? Password { get; set; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.ROOM_JOIN_REQ;

        public JoinRoomRequest()
        {
        }

        /// <summary>
        /// Creates a new <see cref="JoinRoomRequest"/>.
        /// </summary>
        /// <param name="roomId">Room ID of the room to join.</param>
        /// <param name="password">Password for the room, if the room is guarded with a password.</param>
        public JoinRoomRequest(Guid roomId, string? password = null) : this(default, roomId, password)
        {
        }

        /// <summary>
        /// Creates new <see cref="JoinRoomRequest"/>.
        /// </summary>
        public JoinRoomRequest(Guid sessionId) : base(sessionId)
        {
        }

        /// <summary>
        /// Creates a new <see cref="JoinRoomRequest"/>.
        /// </summary>
        /// <param name="roomId">Room ID of the room to join.</param>
        /// <param name="password">Password for the room, if the room is guarded with a password.</param>
        public JoinRoomRequest(Guid sessionId, Guid roomId, string? password = null) : base(sessionId)
        {
            RoomId = roomId;
            Password = password;
        }

        /// <inheritdoc/>
        public bool Equals(JoinRoomRequest? other) => base.Equals(other) && RoomId == other?.RoomId && Password == other?.Password;

        public override JoinRoomResponse CreateResponse(SphynxErrorInfo errorInfo) => new JoinRoomResponse(errorInfo)
        {
            RequestTag = RequestTag
        };
    }
}
