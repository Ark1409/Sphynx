using Sphynx.Core;
using Sphynx.Model.Room;

namespace Sphynx.Network.Packet.Response
{
    /// <inheritdoc cref="SphynxPacketType.ROOM_CREATE_RES"/>
    public sealed class RoomCreateResponse : SphynxResponse, IEquatable<RoomCreateResponse>
    {
        /// <summary>
        /// The newly created room.
        /// </summary>
        public SphynxRoomInfo? RoomInfo { get; set; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.ROOM_CREATE_RES;

        public RoomCreateResponse()
        {
        }

        /// <summary>
        /// Creates a new <see cref="RoomCreateResponse"/>.
        /// </summary>
        /// <param name="errorInfo">Error code for room creation attempt.</param>
        public RoomCreateResponse(SphynxErrorCode errorInfo) : this(new SphynxErrorInfo(errorInfo))
        {
        }

        /// <summary>
        /// Creates a new <see cref="RoomCreateResponse"/>.
        /// </summary>
        /// <param name="errorInfo">Error code for room creation attempt.</param>
        public RoomCreateResponse(SphynxErrorInfo errorInfo) : base(errorInfo)
        {
        }

        /// <summary>
        /// Creates a new <see cref="RoomCreateResponse"/>.
        /// </summary>
        public RoomCreateResponse(SphynxRoomInfo roomInfo) : base(SphynxErrorCode.SUCCESS)
        {
            ArgumentNullException.ThrowIfNull(roomInfo);
            RoomInfo = roomInfo;
        }

        /// <inheritdoc/>
        public bool Equals(RoomCreateResponse? other) => base.Equals(other) && RoomInfo == other?.RoomInfo;
    }
}
