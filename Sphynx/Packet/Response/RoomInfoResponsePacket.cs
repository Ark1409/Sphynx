using System.Diagnostics.CodeAnalysis;

namespace Sphynx.Packet.Response
{
    /// <inheritdoc cref="SphynxPacketType.ROOM_INFO_RES"/>
    public sealed class RoomInfoResponsePacket : SphynxResponsePacket, IEquatable<RoomInfoResponsePacket>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.ROOM_INFO_RES;

        /// <summary>
        /// Creates a new <see cref="RoomInfoResponsePacket"/>.
        /// </summary>
        /// <param name="errorCode">The error code for the response packet.</param>
        public RoomInfoResponsePacket(SphynxErrorCode errorCode) : base(errorCode)
        {
        }

        /// <inheritdoc/>
        public override bool TrySerialize([NotNullWhen(true)] out byte[]? packetBytes)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public bool Equals(RoomInfoResponsePacket? other) => base.Equals(other);
    }
}
