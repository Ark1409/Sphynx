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

        /// <summary>
        /// Attempts to deserialize a <see cref="RoomInfoResponsePacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        /// <param name="packet">The deserialized packet.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out RoomInfoResponsePacket? packet)
        {
            if (TryDeserializeDefaults(contents, out SphynxErrorCode? errorCode))
            {
                packet = new RoomInfoResponsePacket(errorCode.Value);
                return true;
            }

            packet = null;
            return false;
        }

        /// <inheritdoc/>
        public override bool TrySerialize([NotNullWhen(true)] out byte[]? packetBytes)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override bool TrySerialize(Stream stream)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public bool Equals(RoomInfoResponsePacket? other) => base.Equals(other);
    }
}
