using Sphynx.Core;
using Sphynx.ModelV2.User;

namespace Sphynx.Network.PacketV2.Response
{
    /// <inheritdoc cref="SphynxPacketType.REGISTER_RES"/>
    public sealed class RegisterResponse : SphynxResponse, IEquatable<RegisterResponse>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.REGISTER_RES;

        /// <summary>
        /// Holds the authenticated user's information.
        /// </summary>
        public ISphynxSelfInfo? UserInfo { get; init; }

        /// <summary>
        /// The session ID for the client.
        /// </summary>
        public Guid? SessionId { get; init; }

        /// <summary>
        /// Creates a new <see cref="LoginResponse"/>.
        /// </summary>
        /// <param name="errorCode">Error code for login attempt.</param>
        public RegisterResponse(SphynxErrorCode errorCode) : base(errorCode)
        {
        }

        /// <summary>
        /// Creates a new <see cref="LoginResponse"/>.
        /// </summary>
        /// <param name="userInfo">Holds the authenticated user's information.</param>
        /// <param name="sessionId">The session ID for the client.</param>
        public RegisterResponse(ISphynxSelfInfo userInfo, Guid sessionId) : this(SphynxErrorCode.SUCCESS)
        {
            UserInfo = userInfo;
            SessionId = sessionId;
        }

        /// <inheritdoc/>
        public bool Equals(RegisterResponse? other) =>
            base.Equals(other) && SessionId == other?.SessionId && (UserInfo?.Equals(other?.UserInfo) ?? true);
    }
}
