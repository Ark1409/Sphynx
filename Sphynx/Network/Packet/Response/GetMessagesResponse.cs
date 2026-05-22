using Sphynx.Core;
using Sphynx.Model;
using Sphynx.Utils;

namespace Sphynx.Network.Packet.Response
{
    public sealed class GetMessagesResponse : SphynxResponse, IEquatable<GetMessagesResponse>
    {
        /// <inheritdoc/>
        public override SphynxRequestType ResponseType => SphynxRequestType.GET_MSG_REQ;

        /// <summary>
        /// The resolved messages' information. The array is in decreasing order of message creation time.
        /// </summary>
        public SphynxMessageInfo[]? Messages { get; set; }

        public GetMessagesResponse()
        {
        }

        /// <summary>
        /// Creates a new <see cref="GetMessagesResponse"/>.
        /// </summary>
        /// <param name="errorInfo">Error code for logout attempt.</param>
        public GetMessagesResponse(SphynxErrorInfo errorInfo) : base(errorInfo)
        {
        }

        /// <summary>
        /// Creates a new <see cref="GetMessagesResponse"/> with <see cref="SphynxErrorCode.SUCCESS"/>.
        /// </summary>
        /// <param name="messages">The resolved messages' information.</param>
        public GetMessagesResponse(params SphynxMessageInfo[] messages) : this(SphynxErrorCode.SUCCESS)
        {
            Messages = messages;
        }

        /// <inheritdoc/>
        public bool Equals(GetMessagesResponse? other)
        {
            if (other is null || !base.Equals(other)) return false;
            if (Messages is null && other.Messages is null) return true;
            if (Messages is null || other.Messages is null) return false;

            return MemoryUtils.SequenceEqual(Messages, other.Messages);
        }
    }
}
