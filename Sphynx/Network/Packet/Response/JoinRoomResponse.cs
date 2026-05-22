using Sphynx.Core;
using Sphynx.Model.Room;

namespace Sphynx.Network.Packet.Response
{
    public class JoinRoomResponse : SphynxResponse, IEquatable<JoinRoomResponse>
    {
        /// <inheritdoc/>
        public override SphynxRequestType ResponseType => SphynxRequestType.JOIN_ROOM_REQ;

        /// <summary>
        /// The information for the chat room which was joined.
        /// </summary>
        public SphynxRoomInfo? RoomInfo { get; set; }

        public JoinRoomResponse()
        {
        }

        /// <summary>
        /// Creates a new <see cref="JoinRoomResponse"/>.
        /// </summary>
        /// <param name="errorCode">Error code for login attempt.</param>
        public JoinRoomResponse(SphynxErrorCode errorCode = SphynxErrorCode.SUCCESS) : base(errorCode)
        {
        }

        /// <summary>
        /// Creates a new <see cref="JoinRoomResponse"/>.
        /// </summary>
        /// <param name="errorInfo">Error code for login attempt.</param>
        public JoinRoomResponse(SphynxErrorInfo errorInfo) : base(errorInfo)
        {
        }

        /// <summary>
        /// Creates a new <see cref="JoinRoomResponse"/>.
        /// </summary>
        /// <param name="roomInfo">The information for the chat room which was joined.</param>
        public JoinRoomResponse(SphynxRoomInfo roomInfo) : this()
        {
            RoomInfo = roomInfo;
        }

        /// <inheritdoc/>
        public bool Equals(JoinRoomResponse? other) => base.Equals(other) && RoomInfo?.Equals(other?.RoomInfo) == true;
    }
}
