using Sphynx.Core;
using Sphynx.ModelV2.Room;

namespace Sphynx.Network.PacketV2.Response
{
    /// <inheritdoc cref="SphynxPacketType.ROOM_JOIN_RES"/>
    public sealed class JoinRoomResponsePacket : SphynxResponsePacket, IEquatable<JoinRoomResponsePacket>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.ROOM_JOIN_RES;

        /// <summary>
        /// The information for the chat room which was joined.
        /// </summary>
        public IChatRoomInfo? RoomInfo { get; set; }

        /// <summary>
        /// Creates a new <see cref="JoinRoomResponsePacket"/>.
        /// </summary>
        /// <param name="errorCode">Error code for login attempt.</param>
        public JoinRoomResponsePacket(SphynxErrorCode errorCode = SphynxErrorCode.SUCCESS) : base(errorCode)
        {
        }

        /// <summary>
        /// Creates a new <see cref="JoinRoomResponsePacket"/>.
        /// </summary>
        /// <param name="roomInfo">The information for the chat room which was joined.</param>
        public JoinRoomResponsePacket(IChatRoomInfo roomInfo) : this()
        {
            RoomInfo = roomInfo;
        }

        /// <inheritdoc/>
        public bool Equals(JoinRoomResponsePacket? other) =>
            base.Equals(other) && RoomInfo?.Equals(other?.RoomInfo) == true;
    }
}
