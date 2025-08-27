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
        public SphynxSelfInfo? UserInfo { get; init; }

        public string? AccessToken { get; init; }
        public Guid? RefreshToken { get; init; }
        public DateTimeOffset? AccessTokenExpiry { get; init; }

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
        /// <param name="accessToken">The JWT access token.</param>
        /// <param name="refreshToken">The refresh token for the access token.</param>
        /// <param name="accessTokenExpiry">The expiry time of the access token.</param>
        public RegisterResponse(SphynxSelfInfo userInfo, string accessToken, Guid refreshToken, DateTimeOffset accessTokenExpiry)
            : this(SphynxErrorCode.SUCCESS)
        {
            UserInfo = userInfo;
            AccessToken = string.IsNullOrEmpty(accessToken) ? throw new ArgumentException(accessToken) : accessToken;
            RefreshToken = refreshToken;
            AccessTokenExpiry = accessTokenExpiry;
        }

        /// <inheritdoc/>
        public bool Equals(RegisterResponse? other)
        {
            if (other is null || !base.Equals(other))
                return false;

            if (AccessToken != other.AccessToken || RefreshToken != other.RefreshToken || AccessTokenExpiry != other.AccessTokenExpiry)
                return false;

            if (UserInfo is null && other.UserInfo is null)
                return true;

            if (UserInfo is null || other.UserInfo is null)
                return false;

            return UserInfo.Equals(other.UserInfo);
        }
    }
}
