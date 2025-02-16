using Sphynx.Core;

namespace Sphynx.Network.PacketV2.Response
{
    /// <inheritdoc cref="SphynxPacketType.ROOM_LEAVE_RES"/>
    public sealed class LeaveRoomResponsePacket : SphynxResponsePacket, IEquatable<LeaveRoomResponsePacket>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.ROOM_LEAVE_RES;

        /// <summary>
        /// Creates a new <see cref="LeaveRoomResponsePacket"/>.
        /// </summary>
        /// <param name="errorCode">Error code for leave attempt.</param>
        public LeaveRoomResponsePacket(SphynxErrorCode errorCode = SphynxErrorCode.SUCCESS) : base(errorCode)
        {
        }

        /// <inheritdoc/>
        public bool Equals(LeaveRoomResponsePacket? other) => base.Equals(other);
    }
}
