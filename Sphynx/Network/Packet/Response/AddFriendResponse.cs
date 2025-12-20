using Sphynx.Core;
using Sphynx.Model.User;

namespace Sphynx.Network.Packet.Response
{
    /// <inheritdoc cref="SphynxPacketType.ADD_FRIEND_RES"/>
    public sealed class AddFriendResponse : SphynxResponse, IEquatable<AddFriendResponse>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.ADD_FRIEND_RES;

        public SphynxFriendRequest? FriendRequest { get; set; }

        public AddFriendResponse(SphynxFriendRequest friendRequest)
        {
            FriendRequest = friendRequest;
        }

        /// <summary>
        /// Creates a new <see cref="AddFriendResponse"/>.
        /// </summary>
        /// <param name="errorInfo">Error code for kick attempt.</param>
        public AddFriendResponse(SphynxErrorCode errorInfo = SphynxErrorCode.SUCCESS) : base(errorInfo)
        {
        }

        /// <summary>
        /// Creates a new <see cref="AddFriendResponse"/>.
        /// </summary>
        /// <param name="errorInfo">Error code for kick attempt.</param>
        public AddFriendResponse(SphynxErrorInfo errorInfo) : base(errorInfo)
        {
        }

        /// <inheritdoc/>
        public bool Equals(AddFriendResponse? other) => base.Equals(other);
    }
}
