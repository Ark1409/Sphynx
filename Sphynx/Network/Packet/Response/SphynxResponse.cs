using Sphynx.Core;

namespace Sphynx.Network.Packet.Response
{
    public record struct SphynxResponseHeader
    {
        public SphynxRequestType ResponseType { get; init; }
        public Guid RequestId { get; set; }
        public SphynxErrorInfo ErrorInfo { get; set; }
    }

    /// <summary>
    /// Represents a response packet.
    /// </summary>
    /// <seealso cref="SphynxMessageType.Response"/>
    public abstract class SphynxResponse : SphynxMessage, IEquatable<SphynxResponse>
    {
        /// <summary>
        /// The request type for which this response was made.
        /// </summary>
        public abstract SphynxRequestType ResponseType { get; }

        /// <inheritdoc/>
        public sealed override SphynxMessageType MessageType => SphynxMessageType.Response;

        public SphynxResponseHeader Header;

        public SphynxResponse() : this(SphynxErrorCode.SUCCESS)
        {
        }

        public SphynxResponse(SphynxErrorInfo errorInfo)
        {
            Header = new SphynxResponseHeader { ResponseType = ResponseType, ErrorInfo = errorInfo };
        }

        /// <summary>
        /// Checks whether this packet is <see cref="SphynxErrorCode.SUCCESS"/>.
        /// </summary>
        /// <param name="packet">The packet to check.</param>
        /// <returns>true if this packet is a <see cref="SphynxErrorCode.SUCCESS"/>, false otherwise.</returns>
        public static explicit operator bool(SphynxResponse packet) => packet.Header.ErrorInfo == SphynxErrorCode.SUCCESS;

        /// <inheritdoc/>
        public bool Equals(SphynxResponse? other) =>
            base.Equals(other) && ResponseType == other?.ResponseType && Header.ErrorInfo == other?.Header.ErrorInfo;
    }
}
