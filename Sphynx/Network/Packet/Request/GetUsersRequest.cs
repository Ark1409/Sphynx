using Sphynx.Core;
using Sphynx.Network.Packet.Response;
using Sphynx.Utils;

namespace Sphynx.Network.Packet.Request
{
    public class GetUsersRequest : SphynxRequest<GetUsersResponse>, IEquatable<GetUsersRequest>
    {
        /// <summary>
        /// The maximum number of users which can be requested at once.
        /// </summary>
        public const int MAX_USER_COUNT = 50;

        /// <inheritdoc/>
        public override SphynxRequestType RequestType => SphynxRequestType.GET_USER_REQ;

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

        public GetUsersRequest()
        {
        }

        /// <summary>
        /// Creates a new <see cref="GetUsersRequest"/>.
        /// </summary>
        /// <param name="userIds">The user IDs of the users for which to retrieve information.</param>
        public GetUsersRequest(params Guid[] userIds)
        {
            UserIds = userIds;
        }

        /// <inheritdoc/>
        public bool Equals(GetUsersRequest? other) => base.Equals(other) && MemoryUtils.SequenceEqual(UserIds, other?.UserIds);

        public override GetUsersResponse CreateResponse(SphynxErrorInfo errorInfo) => new GetUsersResponse(errorInfo);
    }
}
