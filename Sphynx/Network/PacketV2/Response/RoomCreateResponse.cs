using Sphynx.Core;

namespace Sphynx.Network.PacketV2.Response
{
    /// <inheritdoc cref="SphynxPacketType.ROOM_CREATE_RES"/>
    public sealed class RoomCreateResponse : SphynxResponse, IEquatable<RoomCreateResponse>
    {
        /// <summary>
        /// Room ID assigned to the newly created room.
        /// </summary>
        public Guid? RoomId { get; set; }

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
            // Assume the room is to be created
            if (errorInfo == SphynxErrorCode.SUCCESS)
                RoomId = Guid.NewGuid();
        }

        /// <summary>
        /// Creates a new <see cref="RoomCreateResponse"/>.
        /// </summary>
        /// <param name="roomId">Room ID assigned to the newly created room.</param>
        public RoomCreateResponse(Guid roomId) : base(SphynxErrorCode.SUCCESS)
        {
            RoomId = roomId;
        }

        /// <inheritdoc/>
        public bool Equals(RoomCreateResponse? other) => base.Equals(other) && RoomId == other?.RoomId;
    }
}
