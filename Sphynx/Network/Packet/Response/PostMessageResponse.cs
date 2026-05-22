using Sphynx.Core;
using Sphynx.Model;

namespace Sphynx.Network.Packet.Response
{
    public class PostMessageResponse : SphynxResponse, IEquatable<PostMessageResponse>
    {
        /// <inheritdoc/>
        public override SphynxRequestType ResponseType => SphynxRequestType.SEND_MSG_REQ;

        public SphynxMessageInfo? MessageInfo { get; set; }

        public PostMessageResponse()
        {
        }

        /// <summary>
        /// Creates a new <see cref="PostMessageResponse"/>.
        /// </summary>
        public PostMessageResponse(SphynxMessageInfo msgInfo) : base(SphynxErrorCode.SUCCESS)
        {
            MessageInfo = msgInfo;
        }

        /// <summary>
        /// Creates a new <see cref="PostMessageResponse"/>.
        /// </summary>
        /// <param name="errorInfo">Error code for message attempt.</param>
        public PostMessageResponse(SphynxErrorInfo errorInfo) : base(errorInfo)
        {
        }

        /// <inheritdoc/>
        public bool Equals(PostMessageResponse? other) => base.Equals(other) && (MessageInfo?.Equals(other?.MessageInfo) ?? false);
    }
}
