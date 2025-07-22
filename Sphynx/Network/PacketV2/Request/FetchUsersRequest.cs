using Sphynx.Core;
using Sphynx.Network.PacketV2.Response;
using Sphynx.Utils;

namespace Sphynx.Network.PacketV2.Request
{
    /// <inheritdoc cref="SphynxPacketType.USER_INFO_REQ"/>
    public sealed class FetchUsersRequest : SphynxRequest, IEquatable<FetchUsersRequest>
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
            init
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
        /// Creates a new <see cref="FetchUsersRequest"/>.
        /// </summary>
        /// <param name="accessToken">The JWT access token for this request.</param>
        public FetchUsersRequest(string accessToken) : base(accessToken)
        {
        }

        /// <summary>
        /// Creates a new <see cref="FetchUsersRequest"/>.
        /// </summary>
        /// <param name="accessToken">The JWT access token for this request.</param>
        /// <param name="userIds">The user IDs of the users for which to retrieve information.</param>
        public FetchUsersRequest(string accessToken, params SnowflakeId[] userIds) : base(accessToken)
        {
            UserIds = userIds;
        }

        /// <inheritdoc/>
        public bool Equals(FetchUsersRequest? other) =>
            base.Equals(other) && MemoryUtils.SequenceEqual(UserIds, other?.UserIds);

        public override FetchUsersResponse CreateResponse(SphynxErrorInfo errorInfo) => new FetchUsersResponse(errorInfo);
    }
}
