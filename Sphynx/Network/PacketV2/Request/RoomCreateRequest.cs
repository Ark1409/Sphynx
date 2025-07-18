using Sphynx.Core;
using Sphynx.ModelV2.Room;

namespace Sphynx.Network.PacketV2.Request
{
    /// <inheritdoc cref="SphynxPacketType.ROOM_CREATE_REQ"/>
    public abstract class RoomCreateRequest : SphynxRequest, IEquatable<RoomCreateRequest>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.ROOM_CREATE_REQ;

        /// <summary>
        /// <inheritdoc cref="ChatRoomType"/>
        /// </summary>
        public abstract ChatRoomType RoomType { get; }

        /// <summary>
        /// Creates a new <see cref="RoomCreateRequest"/>.
        /// </summary>
        /// <param name="accessToken">The JWT access token for this request.</param>
        public RoomCreateRequest(string accessToken) : base(accessToken)
        {
        }

        /// <inheritdoc/>
        public bool Equals(RoomCreateRequest? other) => base.Equals(other) && RoomType == other?.RoomType;

        /// <summary>
        /// <see cref="ChatRoomType.DIRECT_MSG"/> room creation request.
        /// </summary>
        public sealed class Direct : RoomCreateRequest, IEquatable<Direct>
        {
            /// <inheritdoc/>
            public override ChatRoomType RoomType => ChatRoomType.DIRECT_MSG;

            /// <summary>
            /// The user ID of the other user to create the DM with.
            /// </summary>
            public SnowflakeId OtherId { get; init; }

            /// <summary>
            /// Creates a new <see cref="RoomCreateRequest"/>.
            /// </summary>
            /// <param name="otherId">The user ID of the other user to create the DM with.</param>
            public Direct(SnowflakeId otherId) : this(null!, otherId)
            {
            }

            /// <summary>
            /// Creates a new <see cref="RoomCreateRequest"/>.
            /// </summary>
            /// <param name="accessToken">The JWT access token for this request.</param>
            public Direct(string accessToken) : base(accessToken)
            {
            }

            /// <summary>
            /// Creates a new <see cref="RoomCreateRequest"/>.
            /// </summary>
            /// <param name="accessToken">The JWT access token for this request.</param>
            /// <param name="otherId">The user ID of the other user to create the DM with.</param>
            public Direct(string accessToken, SnowflakeId otherId) : base(accessToken)
            {
                OtherId = otherId;
            }

            /// <inheritdoc/>
            public bool Equals(Direct? other) => base.Equals(other) && OtherId == other?.OtherId;
        }

        /// <summary>
        /// <see cref="ChatRoomType.GROUP"/> room creation request.
        /// </summary>
        public sealed class Group : RoomCreateRequest, IEquatable<Group>
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
            /// Creates a new <see cref="RoomCreateRequest"/>.
            /// </summary>
            /// <param name="accessToken">The JWT access token for this request.</param>
            public Group(string accessToken) : base(accessToken)
            {
            }

            /// <summary>
            /// Creates a new <see cref="RoomCreateRequest"/>.
            /// </summary>
            /// <param name="name">The name for the chat room.</param>
            /// <param name="password">The password for the chat room, or null if the room is not guarded by a password.</param>
            /// <param name="isPublic">Whether this room is public.</param>
            public Group(string name, string? password = null, bool isPublic = true) : this(null!,
                name, password, isPublic)
            {
            }

            /// <summary>
            /// Creates a new <see cref="RoomCreateRequest"/>.
            /// </summary>
            /// <param name="accessToken">The JWT access token for this request.</param>
            /// <param name="name">The name for the chat room.</param>
            /// <param name="password">The password for the chat room, or null if the room is not guarded by a password.</param>
            /// <param name="isPublic">Whether this room is public.</param>
            public Group(string accessToken, string name, string? password = null, bool isPublic = true) : base(accessToken)
            {
                Name = name ?? throw new ArgumentNullException(nameof(name));
                Password = password;
                Public = isPublic;
            }

            /// <inheritdoc/>
            public bool Equals(Group? other) => base.Equals(other) && Name == other?.Name && Password == other?.Password;
        }
    }
}
