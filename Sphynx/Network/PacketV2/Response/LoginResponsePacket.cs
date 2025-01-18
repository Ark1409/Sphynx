using Sphynx.Core;
using Sphynx.ModelV2.User;

namespace Sphynx.Network.PacketV2.Response
{
    /// <inheritdoc cref="SphynxPacketType.LOGIN_RES"/>
    public sealed class LoginResponsePacket : SphynxResponsePacket, IEquatable<LoginResponsePacket>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.LOGIN_RES;

        /// <summary>
        /// Holds the authenticated user's information.
        /// </summary>
        public ISphynxSelfInfo? UserInfo { get; set; }

        /// <summary>
        /// The session ID for the client.
        /// </summary>
        public Guid? SessionId { get; set; }

        /// <summary>
        /// Creates a new <see cref="LoginResponsePacket"/>.
        /// </summary>
        /// <param name="errorCode">Error code for login attempt. Cannot be <see cref="SphynxErrorCode.SUCCESS"/>.</param>
        public LoginResponsePacket(SphynxErrorCode errorCode) : base(errorCode)
        {
        }

        /// <summary>
        /// Creates a new <see cref="LoginResponsePacket"/>.
        /// </summary>
        /// <param name="userInfo">Holds the authenticated user's information.</param>
        /// <param name="sessionId">The session ID for the client.</param>
        public LoginResponsePacket(ISphynxSelfInfo userInfo, Guid sessionId) : base(SphynxErrorCode.SUCCESS)
        {
            UserInfo = userInfo ?? throw new ArgumentNullException(nameof(userInfo));
            SessionId = sessionId;
        }

        /// <inheritdoc/>
        public bool Equals(LoginResponsePacket? other)
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
