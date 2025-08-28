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
        public SphynxSelfInfo? UserInfo { get; set; }

        public Guid? SessionId { get; set; }

        public RegisterResponse()
        {
        }

        /// <summary>
        /// Creates a new <see cref="LoginResponse"/>.
        /// </summary>
        /// <param name="errorInfo">Error code for login attempt.</param>
        public RegisterResponse(SphynxErrorInfo errorInfo) : base(errorInfo)
        {
        }

        /// <summary>
        /// Creates a new <see cref="LoginResponse"/>.
        /// </summary>
        /// <param name="userInfo">Holds the authenticated user's information.</param>
        /// <param name="sessionId">The identifier the user's session.</param>
        public RegisterResponse(SphynxSelfInfo userInfo, Guid sessionId) : this(SphynxErrorCode.SUCCESS)
        {
            UserInfo = userInfo;
            SessionId = sessionId;
        }

        /// <inheritdoc/>
        public bool Equals(RegisterResponse? other)
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
