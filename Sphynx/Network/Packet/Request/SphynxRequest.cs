using Sphynx.Core;
using Sphynx.Network.Packet.Response;

namespace Sphynx.Network.Packet.Request
{
    public struct SphynxRequestHeader
    {
        public SphynxRequestType RequestType { get; init; }
        public Guid SessionId { get; set; }
        public Guid RequestId { get; set; }
    }

    /// <summary>
    /// Represents a request message.
    /// </summary>
    /// <seealso cref="SphynxMessageType.Request"/>
    public abstract class SphynxRequest : SphynxMessage, IEquatable<SphynxRequest>
    {
        /// <inheritdoc/>
        public sealed override SphynxMessageType MessageType => SphynxMessageType.Request;

        /// <summary>
        /// Holds header information for this request.
        /// </summary>
        public SphynxRequestHeader Header;

        public abstract SphynxRequestType RequestType { get; }

        public SphynxRequest()
        {
            Header = new SphynxRequestHeader { RequestType = RequestType };
        }

        /// <inheritdoc/>
        public virtual bool Equals(SphynxRequest? other) => base.Equals(other) && RequestType == other?.RequestType;

        public abstract SphynxResponse CreateResponse(SphynxErrorInfo errorInfo);

        public override string ToString() => $"{MessageType}, {RequestType}";
    }

    public abstract class SphynxRequest<TResponse> : SphynxRequest where TResponse : SphynxResponse
    {
        public abstract override TResponse CreateResponse(SphynxErrorInfo errorInfo);
    }
}
