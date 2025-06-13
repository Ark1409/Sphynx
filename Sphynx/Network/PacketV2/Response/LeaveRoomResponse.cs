using Sphynx.Core;

namespace Sphynx.Network.PacketV2.Response
{
    /// <inheritdoc cref="SphynxPacketType.ROOM_LEAVE_RES"/>
    public sealed class LeaveRoomResponse : SphynxResponse, IEquatable<LeaveRoomResponse>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.ROOM_LEAVE_RES;

        /// <summary>
        /// Creates a new <see cref="LeaveRoomResponse"/>.
        /// </summary>
        /// <param name="errorCode">Error code for leave attempt.</param>
        public LeaveRoomResponse(SphynxErrorCode errorCode = SphynxErrorCode.SUCCESS) : base(errorCode)
        {
        }

        /// <inheritdoc/>
        public bool Equals(LeaveRoomResponse? other) => base.Equals(other);
    }
}
