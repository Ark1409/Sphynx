using Sphynx.Core;
using Sphynx.Model.User;
using Sphynx.Utils;

namespace Sphynx.Network.Packet.Response
{
    public class GetUsersResponse : SphynxResponse, IEquatable<GetUsersResponse>
    {
        /// <inheritdoc/>
        public override SphynxRequestType ResponseType => SphynxRequestType.GET_USER_REQ;

        /// <summary>
        /// The resolved users' information.
        /// </summary>
        public SphynxUserInfo[]? Users { get; set; }

        public GetUsersResponse()
        {
        }

        /// <summary>
        /// Creates a new <see cref="GetUsersResponse"/>.
        /// </summary>
        /// <param name="errorInfo">Error code for logout attempt.</param>
        public GetUsersResponse(SphynxErrorInfo errorInfo) : base(errorInfo)
        {
        }

        /// <summary>
        /// Creates a new <see cref="GetUsersResponse"/> with <see cref="SphynxErrorCode.SUCCESS"/>.
        /// </summary>
        /// <param name="users">The resolved users' information.</param>
        public GetUsersResponse(params SphynxUserInfo[] users) : this(SphynxErrorCode.SUCCESS)
        {
            Users = users;
        }

        /// <inheritdoc/>
        public bool Equals(GetUsersResponse? other) => base.Equals(other) && MemoryUtils.SequenceEqual(Users, other?.Users);
    }
}
