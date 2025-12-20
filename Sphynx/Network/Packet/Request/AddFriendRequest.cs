using Sphynx.Core;
using Sphynx.Network.Packet.Response;
using Sphynx.Utils;

namespace Sphynx.Network.Packet.Request
{
    /// <inheritdoc cref="SphynxPacketType.ADD_FRIEND_REQ"/>
    public sealed class AddFriendRequest : SphynxRequest<AddFriendResponse>, IEquatable<AddFriendRequest>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.ADD_FRIEND_REQ;

        /// <summary>
        /// The user IDs of the users for which to retrieve information.
        /// </summary>
        public Guid OtherId { get; set; }

        public AddFriendRequest()
        {
        }

        /// <summary>
        /// Creates a new <see cref="FetchUsersRequest"/>.
        /// </summary>
        public AddFriendRequest(Guid sessionId) : base(sessionId)
        {
        }

        /// <summary>
        /// Creates a new <see cref="FetchUsersRequest"/>.
        /// </summary>
        /// <param name="sessionId">The JWT access token for this request.</param>
        public AddFriendRequest(Guid sessionId, Guid otherId) : base(sessionId)
        {
            OtherId = otherId;
        }

        /// <inheritdoc/>
        public bool Equals(AddFriendRequest? other) => base.Equals(other) && OtherId == other?.OtherId;

        public override AddFriendResponse CreateResponse(SphynxErrorInfo errorInfo) => new AddFriendResponse(errorInfo)
        {
            RequestTag = RequestTag
        };
    }
}
