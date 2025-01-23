using Sphynx.Core;
using Sphynx.ModelV2.User;
using Sphynx.Utils;

namespace Sphynx.Network.PacketV2.Response
{
    /// <inheritdoc cref="SphynxPacketType.USER_INFO_RES"/>
    public sealed class GetUsersResponsePacket : SphynxResponsePacket, IEquatable<GetUsersResponsePacket>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.USER_INFO_RES;

        /// <summary>
        /// The resolved users' information.
        /// </summary>
        public ISphynxUserInfo[]? Users { get; set; }

        /// <summary>
        /// Creates a new <see cref="GetUsersResponsePacket"/>.
        /// </summary>
        /// <param name="errorCode">Error code for logout attempt.</param>
        public GetUsersResponsePacket(SphynxErrorCode errorCode) : base(errorCode)
        {
        }

        /// <summary>
        /// Creates a new <see cref="GetUsersResponsePacket"/> with <see cref="SphynxErrorCode.SUCCESS"/>.
        /// </summary>
        /// <param name="users">The resolved users' information.</param>
        public GetUsersResponsePacket(params ISphynxUserInfo[] users) : this(SphynxErrorCode.SUCCESS)
        {
            Users = users;
        }

        /// <inheritdoc/>
        public bool Equals(GetUsersResponsePacket? other)
        {
            if (other is null || !base.Equals(other)) return false;
            if (Users is null && other.Users is null) return true;
            if (Users is null || other.Users is null) return false;

            return MemoryUtils.SequenceEqual(Users, other.Users);
        }
    }
}
