using Sphynx.Core;
using Sphynx.Model.Room;
using Sphynx.Network.Packet.Response;

namespace Sphynx.Network.Packet.Request
{
    /// <inheritdoc cref="SphynxPacketType.ROOM_CREATE_REQ"/>
    public abstract class RoomCreateRequest : SphynxRequest<RoomCreateResponse>, IEquatable<RoomCreateRequest>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.ROOM_CREATE_REQ;

        /// <summary>
        /// <inheritdoc cref="ChatRoomType"/>
        /// </summary>
        public abstract ChatRoomType RoomType { get; }

        public RoomCreateRequest()
        {
        }

        /// <summary>
        /// Creates a new <see cref="RoomCreateRequest"/>.
        /// </summary>
        public RoomCreateRequest(Guid sessionId) : base(sessionId)
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
            public Guid OtherId { get; set; }

            /// <summary>
            /// Creates a new <see cref="RoomCreateRequest"/>.
            /// </summary>
            public Direct()
            {
            }

            /// <summary>
            /// Creates a new <see cref="RoomCreateRequest"/>.
            /// </summary>
            /// <param name="otherId">The user ID of the other user to create the DM with.</param>
            public Direct(Guid otherId) : this(default, otherId)
            {
            }

            /// <summary>
            /// Creates a new <see cref="RoomCreateRequest"/>.
            /// </summary>
            /// <param name="otherId">The user ID of the other user to create the DM with.</param>
            public Direct(Guid sessionId, Guid otherId) : base(sessionId)
            {
                OtherId = otherId;
            }

            /// <inheritdoc/>
            public bool Equals(Direct? other) => base.Equals(other) && OtherId == other?.OtherId;

            public override RoomCreateResponse CreateResponse(SphynxErrorInfo errorInfo) => new RoomCreateResponse(errorInfo);
        }

        /// <summary>
        /// <see cref="ChatRoomType.GROUP"/> room creation request.
        /// </summary>
        public sealed class Group : RoomCreateRequest, IEquatable<Group>
        {
            /// <summary>
            /// The name of the chat room.
            /// </summary>
            public string Name { get; set; } = null!;

            /// <summary>
            /// The password for the chat room.
            /// </summary>
            public string? Password { get; set; }

            /// <summary>
            /// Whether this room is public.
            /// </summary>
            public bool Public { get; set; }

            /// <inheritdoc/>
            public override ChatRoomType RoomType => ChatRoomType.GROUP;

            /// <summary>
            /// Creates a new <see cref="RoomCreateRequest"/>.
            /// </summary>
            public Group()
            {
            }

            /// <summary>
            /// Creates a new <see cref="RoomCreateRequest"/>.
            /// </summary>
            /// <param name="sessionId">The JWT access token for this request.</param>
            public Group(Guid sessionId) : base(sessionId)
            {
            }

            /// <summary>
            /// Creates a new <see cref="RoomCreateRequest"/>.
            /// </summary>
            /// <param name="name">The name for the chat room.</param>
            /// <param name="password">The password for the chat room, or null if the room is not guarded by a password.</param>
            /// <param name="isPublic">Whether this room is public.</param>
            public Group(string name, string? password = null, bool isPublic = true) : this(default,
                name, password, isPublic)
            {
            }

            /// <summary>
            /// Creates a new <see cref="RoomCreateRequest"/>.
            /// </summary>
            /// <param name="sessionId">The JWT access token for this request.</param>
            /// <param name="name">The name for the chat room.</param>
            /// <param name="password">The password for the chat room, or null if the room is not guarded by a password.</param>
            /// <param name="isPublic">Whether this room is public.</param>
            public Group(Guid sessionId, string name, string? password = null, bool isPublic = true) : base(sessionId)
            {
                Name = name ?? throw new ArgumentNullException(nameof(name));
                Password = password;
                Public = isPublic;
            }

            /// <inheritdoc/>
            public bool Equals(Group? other) => base.Equals(other) && Name == other?.Name && Password == other?.Password;

            public override RoomCreateResponse CreateResponse(SphynxErrorInfo errorInfo) => new RoomCreateResponse(errorInfo)
            {
                RequestTag = RequestTag
            };
        }
    }
}
