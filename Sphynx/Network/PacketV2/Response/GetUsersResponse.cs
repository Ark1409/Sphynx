using Sphynx.Core;
using Sphynx.ModelV2.User;
using Sphynx.Utils;

namespace Sphynx.Network.PacketV2.Response
{
    /// <inheritdoc cref="SphynxPacketType.USER_INFO_RES"/>
    public sealed class GetUsersResponse : SphynxResponse, IEquatable<GetUsersResponse>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.USER_INFO_RES;

        /// <summary>
        /// The resolved users' information.
        /// </summary>
        public ISphynxUserInfo[]? Users { get; init; }

        /// <summary>
        /// Creates a new <see cref="GetUsersResponse"/>.
        /// </summary>
        /// <param name="errorCode">Error code for logout attempt.</param>
        public GetUsersResponse(SphynxErrorCode errorCode) : base(errorCode)
        {
        }

        /// <summary>
        /// Creates a new <see cref="GetUsersResponse"/> with <see cref="SphynxErrorCode.SUCCESS"/>.
        /// </summary>
        /// <param name="users">The resolved users' information.</param>
        public GetUsersResponse(params ISphynxUserInfo[] users) : this(SphynxErrorCode.SUCCESS)
        {
            Users = users;
        }

        /// <inheritdoc/>
        public bool Equals(GetUsersResponse? other)
        {
            return base.Equals(other) && MemoryUtils.SequenceEqual(Users, other?.Users);
        }
    }
}
