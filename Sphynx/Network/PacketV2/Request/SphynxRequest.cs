using Sphynx.Core;
using Sphynx.Network.PacketV2.Response;

namespace Sphynx.Network.PacketV2.Request
{
    /// <summary>
    /// Represents a request packet.
    /// </summary>
    public abstract class SphynxRequest : SphynxPacket
    {
        /// <summary>
        /// The JWT access token with which this request should be authorized.
        /// </summary>
        public Guid SessionId { get; set; }

        /// <summary>
        /// Creates a new <see cref="SphynxRequest"/>.
        /// </summary>
        public SphynxRequest() : this(Guid.Empty)
        {
        }

        /// <summary>
        /// Creates a new <see cref="SphynxRequest"/>.
        /// </summary>
        /// <param name="sessionId">The JWT access token for this request.</param>
        public SphynxRequest(Guid sessionId)
        {
            SessionId = sessionId;
        }

        /// <summary>
        /// Indicates whether the current packet has the same user and session ID as another request packet.
        /// </summary>
        /// <param name="other">A request packet to compare with this request packet.</param>
        /// <returns>true if the current packet is equal to the other parameter; otherwise, false.</returns>
        protected bool Equals(SphynxRequest? other) => base.Equals(other) && SessionId == other?.SessionId;

        public abstract SphynxResponse CreateResponse(SphynxErrorInfo errorInfo);
    }

    public abstract class SphynxRequest<TResponse> : SphynxRequest where TResponse : SphynxResponse
    {
        /// <summary>
        /// Creates a new <see cref="SphynxRequest"/>.
        /// </summary>
        public SphynxRequest() : base(default)
        {
        }

        /// <summary>
        /// Creates a new <see cref="SphynxRequest"/>.
        /// </summary>
        /// <param name="sessionId">The JWT access token for this request.</param>
        public SphynxRequest(Guid sessionId) : base(sessionId)
        {
        }

        public abstract override TResponse CreateResponse(SphynxErrorInfo errorInfo);
    }
}
