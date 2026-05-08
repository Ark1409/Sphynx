using Sphynx.Core;
using Sphynx.Model.Room;
using Sphynx.Utils;

namespace Sphynx.Network.Packet.Response
{
    public class GetRoomsResponse : SphynxResponse, IEquatable<GetRoomsResponse>
    {
        /// <inheritdoc/>
        public override SphynxRequestType ResponseType => SphynxRequestType.GET_ROOM_REQ;

        /// <summary>
        /// The resolved rooms' information.
        /// </summary>
        public SphynxRoomInfo[]? Rooms { get; set; }

        public GetRoomsResponse()
        {
        }

        /// <summary>
        /// Creates a new <see cref="GetRoomsResponse"/>.
        /// </summary>
        /// <param name="errorInfo">The error code for the response packet.</param>
        public GetRoomsResponse(SphynxErrorInfo errorInfo) : base(errorInfo)
        {
        }

        /// <summary>
        /// Creates a new <see cref="GetRoomsResponse"/>.
        /// </summary>
        /// <param name="rooms">The error code for the response packet.</param>
        public GetRoomsResponse(params SphynxRoomInfo[] rooms) : this(SphynxErrorCode.SUCCESS)
        {
            Rooms = rooms;
        }

        /// <inheritdoc/>
        public bool Equals(GetRoomsResponse? other) => base.Equals(other) && MemoryUtils.SequenceEqual(Rooms, other?.Rooms);
    }
}
