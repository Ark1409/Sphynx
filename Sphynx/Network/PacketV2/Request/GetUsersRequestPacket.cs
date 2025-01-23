using Sphynx.Core;
using Sphynx.Utils;

namespace Sphynx.Network.PacketV2.Request
{
    /// <inheritdoc cref="SphynxPacketType.USER_INFO_REQ"/>
    public sealed class GetUsersRequestPacket : SphynxRequestPacket, IEquatable<GetUsersRequestPacket>
    {
        /// <summary>
        /// The maximum number of users which can be requested at once.
        /// </summary>
        public const int MAX_USER_COUNT = 50;

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.USER_INFO_REQ;

        /// <summary>
        /// The user IDs of the users for which to retrieve information.
        /// </summary>
        /// <remarks>If the provided value has a length greater than <see cref="MAX_USER_COUNT"/>, the first
        /// <see cref="MAX_USER_COUNT"/> IDs will be taken.</remarks>
        public SnowflakeId[] UserIds
        {
            get => _userIds;
            set
            {
                if (value.Length > MAX_USER_COUNT)
                {
                    if (_userIds.Length != MAX_USER_COUNT)
                        _userIds = new SnowflakeId[MAX_USER_COUNT];

                    Array.Copy(value, 0, _userIds, 0, MAX_USER_COUNT);
                    return;
                }

                _userIds = value;
            }
        }

        private SnowflakeId[] _userIds = Array.Empty<SnowflakeId>();

        /// <summary>
        /// Creates a new <see cref="GetUsersRequestPacket"/>.
        /// </summary>
        /// <param name="userId">The user ID of the requesting user.</param>
        /// <param name="sessionId">The session ID for the requesting user.</param>
        public GetUsersRequestPacket(SnowflakeId userId, Guid sessionId) : base(userId, sessionId)
        {
        }

        /// <summary>
        /// Creates a new <see cref="GetUsersRequestPacket"/>.
        /// </summary>
        /// <param name="userId">The user ID of the requesting user.</param>
        /// <param name="sessionId">The session ID for the requesting user.</param>
        /// <param name="userIds">The user IDs of the users for which to retrieve information.</param>
        public GetUsersRequestPacket(SnowflakeId userId, Guid sessionId, params SnowflakeId[] userIds) : base(userId,
            sessionId)
        {
            UserIds = userIds;
        }

        /// <inheritdoc/>
        public bool Equals(GetUsersRequestPacket? other) =>
            base.Equals(other) && MemoryUtils.SequenceEqual(UserIds, other?.UserIds);
    }
}
