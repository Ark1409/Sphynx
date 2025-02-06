using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Sphynx.Core;

namespace Sphynx.Network.PacketV2.Response
{
    /// <inheritdoc cref="SphynxPacketType.ROOM_DEL_RES"/>
    public sealed class DeleteRoomResponsePacket : SphynxResponsePacket, IEquatable<DeleteRoomResponsePacket>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.ROOM_DEL_RES;

        /// <summary>
        /// Creates a new <see cref="DeleteRoomResponsePacket"/>.
        /// </summary>
        /// <param name="errorCode">Error code for delete attempt.</param>
        public DeleteRoomResponsePacket(SphynxErrorCode errorCode = SphynxErrorCode.SUCCESS) : base(errorCode)
        {
        }

        /// <inheritdoc/>
        public bool Equals(DeleteRoomResponsePacket? other) => base.Equals(other);
    }
}
