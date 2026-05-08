using Sphynx.Core;
using Sphynx.Network.Packet.Response;

namespace Sphynx.Network.Packet.Request
{
    public class DeleteRoomRequest : SphynxRequest<RoomDeleteResponse>, IEquatable<DeleteRoomRequest>
    {
        /// <summary>
        /// The ID of the room to delete.
        /// </summary>
        /// <remarks>Must be a room ID for a group chat room.</remarks>
        public Guid RoomId { get; set; }

        /// <summary>
        /// The password for the room to delete, if the room was guarded with a password.
        /// This acts as a sort of confirmation to ensure the user understands the action they are about to perform.
        /// </summary>
        public string? Password { get; set; }

        /// <inheritdoc/>
        public override SphynxRequestType RequestType => SphynxRequestType.DEL_ROOM_REQ;

        public DeleteRoomRequest()
        {
        }

        public DeleteRoomRequest(Guid roomId, string? password)
        {
            RoomId = roomId;
            Password = password;
        }

        /// <inheritdoc/>
        public bool Equals(DeleteRoomRequest? other) => base.Equals(other) && RoomId == other?.RoomId && Password == other?.Password;

        public override RoomDeleteResponse CreateResponse(SphynxErrorInfo errorInfo) => new RoomDeleteResponse(errorInfo);
    }
}
