using Sphynx.Core;
using Sphynx.ModelV2.User;

namespace Sphynx.Network.PacketV2.Response
{
    /// <inheritdoc cref="SphynxPacketType.LOGIN_RES"/>
    public sealed class LoginResponse : SphynxResponse, IEquatable<LoginResponse>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.LOGIN_RES;

        /// <summary>
        /// Holds the authenticated user's information.
        /// </summary>
        public SphynxSelfInfo? UserInfo { get; init; }

        /// <summary>
        /// The session ID for the client.
        /// </summary>
        public Guid? SessionId { get; init; }

        /// <summary>
        /// Creates a new <see cref="LoginResponse"/>.
        /// </summary>
        /// <param name="errorCode">Error code for login attempt. Cannot be <see cref="SphynxErrorCode.SUCCESS"/>.</param>
        public LoginResponse(SphynxErrorCode errorCode) : base(errorCode)
        {
        }

        /// <summary>
        /// Creates a new <see cref="LoginResponse"/>.
        /// </summary>
        /// <param name="userInfo">Holds the authenticated user's information.</param>
        /// <param name="sessionId">The session ID for the client.</param>
        public LoginResponse(SphynxSelfInfo userInfo, Guid sessionId) : base(SphynxErrorCode.SUCCESS)
        {
            UserInfo = userInfo ?? throw new ArgumentNullException(nameof(userInfo));
            SessionId = sessionId;
        }

        /// <inheritdoc/>
        public bool Equals(LoginResponse? other)
        {
            if (other is null || !base.Equals(other) || SessionId != other.SessionId)
                return false;

            if (UserInfo is null && other.UserInfo is null)
                return true;

            if (UserInfo is null || other.UserInfo is null)
                return false;

            return UserInfo.Equals(other.UserInfo);
        }
    }
}
