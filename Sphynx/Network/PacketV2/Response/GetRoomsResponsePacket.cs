using Sphynx.Core;
using Sphynx.ModelV2.Room;
using Sphynx.Utils;

namespace Sphynx.Network.PacketV2.Response
{
    /// <inheritdoc cref="SphynxPacketType.ROOM_INFO_RES"/>
    public sealed class GetRoomsResponsePacket : SphynxResponsePacket, IEquatable<GetRoomsResponsePacket>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.ROOM_INFO_RES;

        /// <summary>
        /// The resolved rooms' information.
        /// </summary>
        public IChatRoomInfo[]? Rooms { get; set; }

        /// <summary>
        /// Creates a new <see cref="GetRoomsResponsePacket"/>.
        /// </summary>
        /// <param name="errorCode">The error code for the response packet.</param>
        public GetRoomsResponsePacket(SphynxErrorCode errorCode) : base(errorCode)
        {
        }

        /// <summary>
        /// Creates a new <see cref="GetRoomsResponsePacket"/>.
        /// </summary>
        /// <param name="rooms">The error code for the response packet.</param>
        public GetRoomsResponsePacket(params IChatRoomInfo[] rooms) : this(SphynxErrorCode.SUCCESS)
        {
            Rooms = rooms;
        }

        /// <inheritdoc/>
        public bool Equals(GetRoomsResponsePacket? other) =>
            base.Equals(other) && MemoryUtils.SequenceEqual(Rooms, other?.Rooms);
    }
}
