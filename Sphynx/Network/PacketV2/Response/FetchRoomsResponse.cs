using Sphynx.Core;
using Sphynx.ModelV2.Room;
using Sphynx.Utils;

namespace Sphynx.Network.PacketV2.Response
{
    /// <inheritdoc cref="SphynxPacketType.ROOM_INFO_RES"/>
    public sealed class FetchRoomsResponse : SphynxResponse, IEquatable<FetchRoomsResponse>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.ROOM_INFO_RES;

        /// <summary>
        /// The resolved rooms' information.
        /// </summary>
        public ChatRoomInfo[]? Rooms { get; init; }

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
        public FetchRoomsResponse(params ChatRoomInfo[] rooms) : this(SphynxErrorCode.SUCCESS)
        {
            Rooms = rooms;
        }

        /// <inheritdoc/>
        public bool Equals(FetchRoomsResponse? other) =>
            base.Equals(other) && MemoryUtils.SequenceEqual(Rooms, other?.Rooms);
    }
}
