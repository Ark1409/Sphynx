using Sphynx.Core;
using Sphynx.Network.Packet.Response;

namespace Sphynx.Network.Packet.Request
{
    /// <inheritdoc cref="SphynxPacketType.ROOM_KICK_REQ"/>
    public sealed class KickUserRequest : SphynxRequest<KickUserResponse>, IEquatable<KickUserRequest>
    {
        /// <summary>
        /// Room ID of the room to kick the user from.
        /// </summary>
        public Guid RoomId { get; set; }

        /// <summary>
        /// User ID of the user to kick from the room.
        /// </summary>
        public Guid KickId { get; set; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.ROOM_KICK_REQ;

        public KickUserRequest()
        {
        }

        /// <summary>
        /// Creates a new <see cref="KickUserRequest"/>.
        /// </summary>
        /// <param name="roomId">Room ID of the room to kick the user from.</param>
        /// <param name="kickId">User ID of the user to kick from the room.</param>
        public KickUserRequest(Guid roomId, Guid kickId) : this(default, roomId, kickId)
        {
        }

        /// <summary>
        /// Creates new <see cref="KickUserRequest"/>.
        /// </summary>
        public KickUserRequest(Guid sessionId) : base(sessionId)
        {
        }

        /// <summary>
        /// Creates a new <see cref="LeaveRoomRequest"/>.
        /// </summary>
        /// <param name="roomId">Room ID of the room to leave.</param>
        /// <param name="kickId">User ID of the user to kick from the room.</param>
        public KickUserRequest(Guid sessionId, Guid roomId, Guid kickId) : base(sessionId)
        {
            RoomId = roomId;
            KickId = kickId;
        }

        /// <inheritdoc/>
        public bool Equals(KickUserRequest? other) => base.Equals(other) && RoomId == other?.RoomId && KickId == other?.KickId;

        public override KickUserResponse CreateResponse(SphynxErrorInfo errorInfo) => new KickUserResponse(errorInfo)
        {
            RequestTag = RequestTag
        };
    }
}
