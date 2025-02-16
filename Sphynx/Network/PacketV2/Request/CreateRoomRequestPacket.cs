using Sphynx.Core;
using Sphynx.ModelV2.Room;

namespace Sphynx.Network.PacketV2.Request
{
    /// <inheritdoc cref="SphynxPacketType.ROOM_CREATE_REQ"/>
    public abstract class CreateRoomRequestPacket : SphynxRequestPacket, IEquatable<CreateRoomRequestPacket>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.ROOM_CREATE_REQ;

        /// <summary>
        /// <inheritdoc cref="ChatRoomType"/>
        /// </summary>
        public abstract ChatRoomType RoomType { get; }

        /// <summary>
        /// Creates a new <see cref="CreateRoomRequestPacket"/>.
        /// </summary>
        /// <param name="userId">The user ID of the requesting user.</param>
        /// <param name="sessionId">The session ID for the requesting user.</param>
        public CreateRoomRequestPacket(SnowflakeId userId, Guid sessionId) : base(userId, sessionId)
        {
        }

        /// <inheritdoc/>
        public bool Equals(CreateRoomRequestPacket? other) => base.Equals(other) && RoomType == other?.RoomType;

        /// <summary>
        /// <see cref="ChatRoomType.DIRECT_MSG"/> room creation request.
        /// </summary>
        public sealed class Direct : CreateRoomRequestPacket, IEquatable<Direct>
        {
            /// <inheritdoc/>
            public override ChatRoomType RoomType => ChatRoomType.DIRECT_MSG;

            /// <summary>
            /// The user ID of the other user to create the DM with.
            /// </summary>
            public SnowflakeId OtherId { get; init; }

            /// <summary>
            /// Creates a new <see cref="CreateRoomRequestPacket"/>.
            /// </summary>
            /// <param name="otherId">The user ID of the other user to create the DM with.</param>
            public Direct(SnowflakeId otherId) : this(SnowflakeId.Empty, Guid.Empty, otherId)
            {
            }

            /// <summary>
            /// Creates a new <see cref="CreateRoomRequestPacket"/>.
            /// </summary>
            /// <param name="userId">The user ID of the requesting user.</param>
            /// <param name="sessionId">The session ID for the requesting user.</param>
            public Direct(SnowflakeId userId, Guid sessionId) : base(userId, sessionId)
            {
            }

            /// <summary>
            /// Creates a new <see cref="CreateRoomRequestPacket"/>.
            /// </summary>
            /// <param name="userId">The user ID of the requesting user.</param>
            /// <param name="sessionId">The session ID for the requesting user.</param>
            /// <param name="otherId">The user ID of the other user to create the DM with.</param>
            public Direct(SnowflakeId userId, Guid sessionId, SnowflakeId otherId) : base(userId, sessionId)
            {
                OtherId = otherId;
            }

            /// <inheritdoc/>
            public bool Equals(Direct? other) => base.Equals(other) && OtherId == other?.OtherId;
        }

        /// <summary>
        /// <see cref="ChatRoomType.GROUP"/> room creation request.
        /// </summary>
        public sealed class Group : CreateRoomRequestPacket, IEquatable<Group>
        {
            /// <summary>
            /// The name of the chat room.
            /// </summary>
            public string Name { get; init; }

            /// <summary>
            /// The password for the chat room.
            /// </summary>
            public string? Password { get; set; }

            /// <summary>
            /// Whether this room is public.
            /// </summary>
            public bool Public { get; init; }

            /// <inheritdoc/>
            public override ChatRoomType RoomType => ChatRoomType.GROUP;

            /// <summary>
            /// Creates a new <see cref="CreateRoomRequestPacket"/>.
            /// </summary>
            /// <param name="userId">The user ID of the requesting user.</param>
            /// <param name="sessionId">The session ID for the requesting user.</param>
            public Group(SnowflakeId userId, Guid sessionId) : base(userId, sessionId)
            {
            }

            /// <summary>
            /// Creates a new <see cref="CreateRoomRequestPacket"/>.
            /// </summary>
            /// <param name="name">The name for the chat room.</param>
            /// <param name="password">The password for the chat room, or null if the room is not guarded by a password.</param>
            /// <param name="isPublic">Whether this room is public.</param>
            public Group(string name, string? password = null, bool isPublic = true) : this(SnowflakeId.Empty,
                Guid.Empty, name, password, isPublic)
            {
            }

            /// <summary>
            /// Creates a new <see cref="CreateRoomRequestPacket"/>.
            /// </summary>
            /// <param name="userId">The user ID of the requesting user.</param>
            /// <param name="sessionId">The session ID for the requesting user.</param>
            /// <param name="name">The name for the chat room.</param>
            /// <param name="password">The password for the chat room, or null if the room is not guarded by a password.</param>
            /// <param name="isPublic">Whether this room is public.</param>
            public Group(
                SnowflakeId userId,
                Guid sessionId,
                string name,
                string? password = null,
                bool isPublic = true) : base(userId, sessionId)
            {
                Name = name ?? throw new ArgumentNullException(nameof(name));
                Password = password;
                Public = isPublic;
            }

            /// <inheritdoc/>
            public bool Equals(Group? other) =>
                base.Equals(other) && Name == other?.Name && Password == other?.Password;
        }
    }
}
