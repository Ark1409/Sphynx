using Sphynx.Core;

namespace Sphynx.Network.PacketV2.Response
{
    /// <summary>
    /// Represents a response packet.
    /// </summary>
    public abstract class SphynxResponsePacket : SphynxPacket
    {
        /// <summary>
        /// <inheritdoc cref="SphynxErrorCode"/>
        /// </summary>
        public SphynxErrorCode ErrorCode { get; init; }

        /// <summary>
        /// Creates a new <see cref="SphynxResponsePacket"/>.
        /// </summary>
        /// <param name="errorCode">The error code for the response packet.</param>
        public SphynxResponsePacket(SphynxErrorCode errorCode)
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Checks whether this packet is <see cref="SphynxErrorCode.SUCCESS"/>.
        /// </summary>
        /// <param name="packet">The packet to check.</param>
        /// <returns>true if this packet is a <see cref="SphynxErrorCode.SUCCESS"/>, false otherwise.</returns>
        public static implicit operator bool(SphynxResponsePacket packet) =>
            packet.ErrorCode == SphynxErrorCode.SUCCESS;

        /// <summary>
        /// Indicates whether the current packet has the same user and session ID as another request packet.
        /// </summary>
        /// <param name="other">A request packet to compare with this request packet.</param>
        /// <returns>true if the current packet is equal to the other parameter; otherwise, false.</returns>
        protected bool Equals(SphynxResponsePacket? other) => base.Equals(other) && ErrorCode == other?.ErrorCode;
    }
}
