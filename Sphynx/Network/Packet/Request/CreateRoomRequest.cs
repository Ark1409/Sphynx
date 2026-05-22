using Sphynx.Core;
using Sphynx.Model.Room;
using Sphynx.Network.Packet.Response;

namespace Sphynx.Network.Packet.Request
{
    public abstract class CreateRoomRequest : SphynxRequest<RoomCreateResponse>, IEquatable<CreateRoomRequest>
    {
        /// <inheritdoc/>
        public override SphynxRequestType RequestType => SphynxRequestType.CREATE_ROOM_REQ;

        /// <summary>
        /// <inheritdoc cref="SphynxRoomType"/>
        /// </summary>
        public abstract SphynxRoomType RoomType { get; }

        public CreateRoomRequest()
        {
        }

        /// <inheritdoc/>
        public bool Equals(CreateRoomRequest? other) => base.Equals(other) && RoomType == other?.RoomType;

        public sealed class Direct : CreateRoomRequest, IEquatable<Direct>
        {
            /// <inheritdoc/>
            public override SphynxRoomType RoomType => SphynxRoomType.DIRECT_MSG;

            /// <summary>
            /// The user ID of the other user to create the DM with.
            /// </summary>
            public Guid OtherId { get; set; }

            public Direct()
            {
            }

            public Direct(Guid otherId)
            {
                OtherId = otherId;
            }

            /// <inheritdoc/>
            public bool Equals(Direct? other) => base.Equals(other) && OtherId == other?.OtherId;

            public override RoomCreateResponse CreateResponse(SphynxErrorInfo errorInfo) => new RoomCreateResponse(errorInfo);
        }

        public sealed class Group : CreateRoomRequest, IEquatable<Group>
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
            public bool IsPublic { get; set; }

            /// <inheritdoc/>
            public override SphynxRoomType RoomType => SphynxRoomType.GROUP;

            public Group()
            {
            }

            /// <summary>
            /// Creates a new <see cref="CreateRoomRequest"/>.
            /// </summary>
            /// <param name="name">The name for the chat room.</param>
            /// <param name="password">The password for the chat room, or null if the room is not guarded by a password.</param>
            /// <param name="isPublic">Whether this room is public.</param>
            public Group(string name, string? password = null, bool isPublic = true)
            {
                Name = name ?? throw new ArgumentNullException(nameof(name));
                Password = password;
                IsPublic = isPublic;
            }

            /// <inheritdoc/>
            public bool Equals(Group? other) => base.Equals(other) && Name == other?.Name && Password == other?.Password;

            public override RoomCreateResponse CreateResponse(SphynxErrorInfo errorInfo) => new RoomCreateResponse(errorInfo);
        }
    }
}
