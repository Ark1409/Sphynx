using Sphynx.Core;

namespace Sphynx.Network.PacketV2.Request
{
    /// <summary>
    /// Represents a request packet.
    /// </summary>
    public abstract class SphynxRequest : SphynxPacket
    {
        /// <summary>
        /// The user ID of the requesting user.
        /// </summary>
        public SnowflakeId UserId { get; init; }

        /// <summary>
        /// The session ID for the requesting user.
        /// </summary>s
        public Guid SessionId { get; init; }

        /// <summary>
        /// Creates a new <see cref="SphynxRequest"/>.
        /// </summary>
        public SphynxRequest() : this(SnowflakeId.Empty, Guid.Empty)
        {
        }

        /// <summary>
        /// Creates a new <see cref="SphynxRequest"/>.
        /// </summary>
        /// <param name="userId">The user ID of the requesting user.</param>
        /// <param name="sessionId">The session ID for the requesting user.</param>
        public SphynxRequest(SnowflakeId userId, Guid sessionId)
        {
            UserId = userId;
            SessionId = sessionId;
        }

        /// <summary>
        /// Indicates whether the current packet has the same user and session ID as another request packet.
        /// </summary>
        /// <param name="other">A request packet to compare with this request packet.</param>
        /// <returns>true if the current packet is equal to the other parameter; otherwise, false.</returns>
        protected bool Equals(SphynxRequest? other) =>
            base.Equals(other) && UserId == other?.UserId && SessionId == other?.SessionId;
    }
}
