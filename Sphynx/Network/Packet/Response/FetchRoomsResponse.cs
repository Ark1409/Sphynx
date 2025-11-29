using Sphynx.Core;
using Sphynx.Model.Room;
using Sphynx.Utils;

namespace Sphynx.Network.Packet.Response
{
    /// <inheritdoc cref="SphynxPacketType.ROOM_INFO_RES"/>
    public sealed class FetchRoomsResponse : SphynxResponse, IEquatable<FetchRoomsResponse>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.ROOM_INFO_RES;

        /// <summary>
        /// The resolved rooms' information.
        /// </summary>
        public SphynxRoomInfo[]? Rooms { get; set; }

        public FetchRoomsResponse()
        {
        }

        /// <summary>
        /// Creates a new <see cref="FetchRoomsResponse"/>.
        /// </summary>
        /// <param name="errorInfo">The error code for the response packet.</param>
        public FetchRoomsResponse(SphynxErrorInfo errorInfo) : base(errorInfo)
        {
        }

        /// <summary>
        /// Creates a new <see cref="FetchRoomsResponse"/>.
        /// </summary>
        /// <param name="rooms">The error code for the response packet.</param>
        public FetchRoomsResponse(params SphynxRoomInfo[] rooms) : this(SphynxErrorCode.SUCCESS)
        {
            Rooms = rooms;
        }

        /// <inheritdoc/>
        public bool Equals(FetchRoomsResponse? other) =>
            base.Equals(other) && MemoryUtils.SequenceEqual(Rooms, other?.Rooms);
    }
}
