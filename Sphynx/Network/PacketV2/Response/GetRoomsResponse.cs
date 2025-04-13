using Sphynx.Core;
using Sphynx.ModelV2.Room;
using Sphynx.Utils;

namespace Sphynx.Network.PacketV2.Response
{
    /// <inheritdoc cref="SphynxPacketType.ROOM_INFO_RES"/>
    public sealed class GetRoomsResponse : SphynxResponse, IEquatable<GetRoomsResponse>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.ROOM_INFO_RES;

        /// <summary>
        /// The resolved rooms' information.
        /// </summary>
        public IChatRoomInfo[]? Rooms { get; init; }

        /// <summary>
        /// Creates a new <see cref="GetRoomsResponse"/>.
        /// </summary>
        /// <param name="errorCode">The error code for the response packet.</param>
        public GetRoomsResponse(SphynxErrorCode errorCode) : base(errorCode)
        {
        }

        /// <summary>
        /// Creates a new <see cref="GetRoomsResponse"/>.
        /// </summary>
        /// <param name="rooms">The error code for the response packet.</param>
        public GetRoomsResponse(params IChatRoomInfo[] rooms) : this(SphynxErrorCode.SUCCESS)
        {
            Rooms = rooms;
        }

        /// <inheritdoc/>
        public bool Equals(GetRoomsResponse? other) =>
            base.Equals(other) && MemoryUtils.SequenceEqual(Rooms, other?.Rooms);
    }
}
