using Sphynx.Core;
using Sphynx.Model.Room;
using Sphynx.Network.Packet.Response;

namespace Sphynx.Network.Packet.Request
{
    /// <inheritdoc cref="SphynxPacketType.ROOM_DEL_REQ"/>
    /// <remarks>Only rooms of type <see cref="ChatRoomType.GROUP"/> can be deleted.</remarks>
    public sealed class RoomDeleteRequest : SphynxRequest<RoomDeleteResponse>, IEquatable<RoomDeleteRequest>
    {
        /// <summary>
        /// The ID of the room to delete.
        /// </summary>
        /// <remarks>Must be a room ID for a group chat room.</remarks>
        public SnowflakeId RoomId { get; set; }

        /// <summary>
        /// The password for the room to delete, if the room was guarded with a password.
        /// This acts as a sort of confirmation to ensure the user understands the action they are about to perform.
        /// </summary>
        public string? Password { get; set; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.ROOM_DEL_REQ;

        public RoomDeleteRequest()
        {
        }

        /// <summary>
        /// Creates new <see cref="RoomDeleteRequest"/>.
        /// </summary>
        /// <param name="roomId">The ID of the room to delete.</param>
        /// <param name="password">The password for the room to delete, if the room was guarded with a password.</param>
        public RoomDeleteRequest(SnowflakeId roomId, string? password) : this(default, roomId, password)
        {
        }

        /// <summary>
        /// Creates new <see cref="RoomDeleteRequest"/>.
        /// </summary>
        public RoomDeleteRequest(Guid sessionId) : base(sessionId)
        {
        }

        /// <summary>
        /// Creates new <see cref="RoomDeleteRequest"/>.
        /// </summary>
        /// <param name="roomId">The ID of the room to delete. Only rooms of type <see cref="ChatRoomType.GROUP"/> can be deleted.</param>
        /// <param name="password">The password for the room to delete, if the room was guarded with a password.</param>
        public RoomDeleteRequest(Guid sessionId, SnowflakeId roomId, string? password) : base(sessionId)
        {
            RoomId = roomId;
            Password = password;
        }

        /// <inheritdoc/>
        public bool Equals(RoomDeleteRequest? other) => base.Equals(other) && RoomId == other?.RoomId && Password == other?.Password;

        public override RoomDeleteResponse CreateResponse(SphynxErrorInfo errorInfo) => new RoomDeleteResponse(errorInfo);
    }
}
