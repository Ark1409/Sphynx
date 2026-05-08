using Sphynx.Core;
using Sphynx.Network.Packet.Response;
using Sphynx.Utils;

namespace Sphynx.Network.Packet.Request
{
    public  class AddFriendRequest : SphynxRequest<AddFriendResponse>, IEquatable<AddFriendRequest>
    {
        /// <inheritdoc/>
        public override SphynxRequestType RequestType => SphynxRequestType.ADD_FRIEND_REQ;

        /// <summary>
        /// The user IDs of the users for which to retrieve information.
        /// </summary>
        public Guid OtherId { get; set; }

        public AddFriendRequest()
        {
        }

        /// <summary>
        /// Creates a new <see cref="GetUsersRequest"/>.
        /// </summary>
        public AddFriendRequest(Guid otherId)
        {
            OtherId = otherId;
        }

        /// <inheritdoc/>
        public bool Equals(AddFriendRequest? other) => base.Equals(other) && OtherId == other?.OtherId;

        public override AddFriendResponse CreateResponse(SphynxErrorInfo errorInfo) => new AddFriendResponse(errorInfo);
    }
}
