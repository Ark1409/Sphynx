using System.Diagnostics.CodeAnalysis;
using Sphynx.Core;
using Sphynx.Model.User;

namespace Sphynx.Network.PacketV2.Response
{
    /// <inheritdoc cref="SphynxPacketType.REGISTER_RES"/>
    public sealed class RegisterResponsePacket : SphynxResponsePacket, IEquatable<RegisterResponsePacket>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.LOGIN_RES;

        /// <summary>
        /// Holds the authenticated user's information.
        /// </summary>
        public SphynxUserInfo? UserInfo { get; set; }

        /// <summary>
        /// The session ID for the client.
        /// </summary>
        public Guid? SessionId { get; set; }

        private const int SESSION_ID_OFFSET = DEFAULT_CONTENT_SIZE;
        private static readonly int USER_ID_OFFSET = SESSION_ID_OFFSET + GUID_SIZE;
        private static readonly int USER_STATUS_OFFSET = USER_ID_OFFSET + GUID_SIZE;
        private static readonly int USERNAME_SIZE_OFFSET = USER_STATUS_OFFSET + sizeof(SphynxUserStatus);
        private static readonly int USERNAME_OFFSET = USERNAME_SIZE_OFFSET + sizeof(int);

        /// <summary>
        /// Creates a new <see cref="LoginResponsePacket"/> with <see cref="SphynxErrorCode.SUCCESS"/>.
        /// </summary>
        public RegisterResponsePacket() : this(SphynxErrorCode.SUCCESS)
        {
        }

        /// <summary>
        /// Creates a new <see cref="LoginResponsePacket"/>.
        /// </summary>
        /// <param name="errorCode">Error code for login attempt.</param>
        public RegisterResponsePacket(SphynxErrorCode errorCode) : base(errorCode)
        {
        }

        /// <summary>
        /// Creates a new <see cref="LoginResponsePacket"/>.
        /// </summary>
        /// <param name="userInfo">Holds the authenticated user's information.</param>
        /// <param name="sessionId">The session ID for the client.</param>
        public RegisterResponsePacket(SphynxUserInfo userInfo, Guid sessionId) : this(SphynxErrorCode.SUCCESS)
        {
            UserInfo = userInfo;
            SessionId = sessionId;
        }

        public override bool TrySerialize([NotNullWhen(true)] out byte[]? packetBytes)
        {
            throw new NotImplementedException();
        }

        public override Task<bool> TrySerializeAsync(Stream stream)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public bool Equals(RegisterResponsePacket? other) =>
            base.Equals(other) && SessionId == other?.SessionId && (UserInfo?.Equals(other?.UserInfo) ?? true);
    }
}
