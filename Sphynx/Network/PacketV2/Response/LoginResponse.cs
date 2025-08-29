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
        public SphynxSelfInfo? UserInfo { get; set; }

        /// <summary>
        /// Identifier for the current session.
        /// </summary>
        public Guid? SessionId { get; set; }

        public LoginResponse()
        {
        }

        /// <summary>
        /// Creates a new <see cref="LoginResponse"/>.
        /// </summary>
        /// <param name="errorInfo">Error code for login attempt. Cannot be <see cref="SphynxErrorCode.SUCCESS"/>.</param>
        public LoginResponse(SphynxErrorInfo errorInfo) : base(errorInfo)
        {
        }

        /// <summary>
        /// Creates a new <see cref="LoginResponse"/>.
        /// </summary>
        /// <param name="userInfo">Holds the authenticated user's information.</param>
        /// <param name="sessionId">The identifier for the user's session.</param>
        public LoginResponse(SphynxSelfInfo userInfo, Guid sessionId)
            : this(SphynxErrorCode.SUCCESS)
        {
            UserInfo = userInfo ?? throw new ArgumentNullException(nameof(userInfo));
            SessionId = sessionId;
        }

        /// <inheritdoc/>
        public bool Equals(LoginResponse? other)
        {
            if (other is null || !base.Equals(other))
                return false;

            if (SessionId != other.SessionId)
                return false;

            if (UserInfo is null && other.UserInfo is null)
                return true;

            if (UserInfo is null || other.UserInfo is null)
                return false;

            return UserInfo.Equals(other.UserInfo);
        }
    }
}
