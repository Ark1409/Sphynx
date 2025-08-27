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
        public Guid[] UserIds
        {
            get => _userIds;
            set
            {
                if (value.Length > MAX_USER_COUNT)
                {
                    if (_userIds.Length != MAX_USER_COUNT)
                        _userIds = new Guid[MAX_USER_COUNT];

                    Array.Copy(value, 0, _userIds, 0, MAX_USER_COUNT);
                    return;
                }

                _userIds = value;
            }
        }

        private Guid[] _userIds = Array.Empty<Guid>();

        public FetchUsersRequest()
        {
        }

        /// <summary>
        /// Creates a new <see cref="FetchUsersRequest"/>.
        /// </summary>
        public FetchUsersRequest(Guid sessionId) : base(sessionId)
        {
        }

        /// <summary>
        /// Creates a new <see cref="FetchUsersRequest"/>.
        /// </summary>
        /// <param name="sessionId">The JWT access token for this request.</param>
        /// <param name="userIds">The user IDs of the users for which to retrieve information.</param>
        public FetchUsersRequest(Guid sessionId, params Guid[] userIds) : base(sessionId)
        {
            UserIds = userIds;
        }

        /// <inheritdoc/>
        public bool Equals(FetchUsersRequest? other) =>
            base.Equals(other) && MemoryUtils.SequenceEqual(UserIds, other?.UserIds);

        public override FetchUsersResponse CreateResponse(SphynxErrorInfo errorInfo) => new FetchUsersResponse(errorInfo);
    }
}
