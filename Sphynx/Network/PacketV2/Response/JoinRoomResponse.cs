using Sphynx.Core;
using Sphynx.ModelV2.Room;

namespace Sphynx.Network.PacketV2.Response
{
    /// <inheritdoc cref="SphynxPacketType.ROOM_JOIN_RES"/>
    public sealed class JoinRoomResponse : SphynxResponse, IEquatable<JoinRoomResponse>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.ROOM_JOIN_RES;

        /// <summary>
        /// The information for the chat room which was joined.
        /// </summary>
        public ChatRoomInfo? RoomInfo { get; set; }

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
        public JoinRoomResponse(ChatRoomInfo roomInfo) : this()
        {
            RoomInfo = roomInfo;
        }

        /// <inheritdoc/>
        public bool Equals(JoinRoomResponse? other) =>
            base.Equals(other) && RoomInfo?.Equals(other?.RoomInfo) == true;
    }
}
