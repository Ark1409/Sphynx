using Sphynx.Core;

namespace Sphynx.Network.PacketV2.Response
{
    /// <summary>
    /// Represents a response packet.
    /// </summary>
    public abstract class SphynxResponse : SphynxPacket
    {
        /// <summary>
        /// <inheritdoc cref="SphynxErrorInfo"/>
        /// </summary>
        public SphynxErrorInfo ErrorInfo { get; init; }

        public SphynxResponse() : this(SphynxErrorCode.SERVER_ERROR)
        {
        }

        /// <summary>
        /// Creates a new <see cref="SphynxResponse"/>.
        /// </summary>
        /// <param name="errorInfo">The error code for the response packet.</param>
        public SphynxResponse(SphynxErrorInfo errorInfo)
        {
            ErrorInfo = errorInfo;
        }

        /// <summary>
        /// Checks whether this packet is <see cref="SphynxErrorCode.SUCCESS"/>.
        /// </summary>
        /// <param name="packet">The packet to check.</param>
        /// <returns>true if this packet is a <see cref="SphynxErrorCode.SUCCESS"/>, false otherwise.</returns>
        public static implicit operator bool(SphynxResponse packet) =>
            packet.ErrorInfo == SphynxErrorCode.SUCCESS;

        /// <summary>
        /// Indicates whether the current packet has the same user and session ID as another request packet.
        /// </summary>
        /// <param name="other">A request packet to compare with this request packet.</param>
        /// <returns>true if the current packet is equal to the other parameter; otherwise, false.</returns>
        protected bool Equals(SphynxResponse? other) => base.Equals(other) && ErrorInfo == other?.ErrorInfo;
    }
}
