using Sphynx.Core;
using Sphynx.Model.ChatRoom;

namespace Sphynx.Network.PacketV2.Request
{
    /// <inheritdoc cref="SphynxPacketType.ROOM_DEL_REQ"/>
    /// <remarks>Only rooms of type <see cref="ChatRoomType.GROUP"/> can be deleted.</remarks>
    public sealed class DeleteRoomRequestPacket : SphynxRequestPacket, IEquatable<DeleteRoomRequestPacket>
    {
        /// <summary>
        /// The ID of the room to delete.
        /// </summary>
        /// <remarks>Must be a room ID for a group chat room.</remarks>
        public SnowflakeId RoomId { get; init; }

        /// <summary>
        /// The password for the room to delete, if the room was guarded with a password.
        /// This acts as a sort of confirmation to ensure the user understands the action they are about to perform.
        /// </summary>
        public string? Password { get; set; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.ROOM_DEL_REQ;

        /// <summary>
        /// Creates new <see cref="DeleteRoomRequestPacket"/>.
        /// </summary>
        /// <param name="roomId">The ID of the room to delete.</param>
        /// <param name="password">The password for the room to delete, if the room was guarded with a password.</param>
        public DeleteRoomRequestPacket(SnowflakeId roomId, string? password)
            : this(SnowflakeId.Empty, Guid.Empty, roomId, password)
        {
        }

        /// <summary>
        /// Creates new <see cref="DeleteRoomRequestPacket"/>.
        /// </summary>
        /// <param name="userId">The user ID of the requesting user.</param>
        /// <param name="sessionId">The session ID for the requesting user.</param>
        public DeleteRoomRequestPacket(SnowflakeId userId, Guid sessionId) : base(userId, sessionId)
        {
        }

        /// <summary>
        /// Creates new <see cref="DeleteRoomRequestPacket"/>.
        /// </summary>
        /// <param name="userId">The user ID of the requesting user.</param>
        /// <param name="sessionId">The session ID for the requesting user.</param>
        /// <param name="roomId">The ID of the room to delete. Only rooms of type <see cref="ChatRoomType.GROUP"/> can be deleted.</param>
        /// <param name="password">The password for the room to delete, if the room was guarded with a password.</param>
        public DeleteRoomRequestPacket(SnowflakeId userId, Guid sessionId, SnowflakeId roomId, string? password)
            : base(userId, sessionId)
        {
            RoomId = roomId;
            Password = password;
        }

        /// <inheritdoc/>
        public bool Equals(DeleteRoomRequestPacket? other) =>
            base.Equals(other) && RoomId == other?.RoomId && Password == other?.Password;
    }
}
