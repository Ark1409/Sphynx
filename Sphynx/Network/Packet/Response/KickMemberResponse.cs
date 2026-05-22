using Sphynx.Core;

namespace Sphynx.Network.Packet.Response
{
    public class KickMemberResponse : SphynxResponse, IEquatable<KickMemberResponse>
    {
        /// <inheritdoc/>
        public override SphynxRequestType ResponseType => SphynxRequestType.KICK_MEMBER_REQ;

        public KickMemberResponse()
        {
        }

        /// <summary>
        /// Creates a new <see cref="KickMemberResponse"/>.
        /// </summary>
        /// <param name="errorInfo">Error code for kick attempt.</param>
        public KickMemberResponse(SphynxErrorCode errorInfo = SphynxErrorCode.SUCCESS) : base(errorInfo)
        {
        }

        /// <summary>
        /// Creates a new <see cref="KickMemberResponse"/>.
        /// </summary>
        /// <param name="errorInfo">Error code for kick attempt.</param>
        public KickMemberResponse(SphynxErrorInfo errorInfo) : base(errorInfo)
        {
        }

        /// <inheritdoc/>
        public bool Equals(KickMemberResponse? other) => base.Equals(other);
    }
}
