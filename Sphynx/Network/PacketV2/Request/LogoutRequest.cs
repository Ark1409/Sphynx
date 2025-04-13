using Sphynx.Core;

namespace Sphynx.Network.PacketV2.Request
{
    /// <inheritdoc cref="SphynxPacketType.LOGOUT_REQ"/>
    public sealed class LogoutRequest : SphynxRequest, IEquatable<LogoutRequest>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.LOGOUT_REQ;

        /// <summary>
        /// Creates a new <see cref="LogoutRequest"/>.
        /// </summary>
        /// <param name="userId">The user ID of the requesting user.</param>
        /// <param name="sessionId">The session ID for the requesting user.</param>
        public LogoutRequest(SnowflakeId userId, Guid sessionId) : base(userId, sessionId)
        {
        }

        /// <inheritdoc/>
        public bool Equals(LogoutRequest? other) => base.Equals(other);
    }
}
