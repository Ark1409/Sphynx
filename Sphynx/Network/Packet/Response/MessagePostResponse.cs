using Sphynx.Core;
using Sphynx.Model;

namespace Sphynx.Network.Packet.Response
{
    /// <inheritdoc cref="SphynxPacketType.MSG_RES"/>
    public sealed class MessagePostResponse : SphynxResponse, IEquatable<MessagePostResponse>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.MSG_RES;

        public SphynxMessageInfo? MessageInfo { get; set; }

        public MessagePostResponse()
        {
        }

        /// <summary>
        /// Creates a new <see cref="MessagePostResponse"/>.
        /// </summary>
        public MessagePostResponse(SphynxMessageInfo msgInfo) : base(SphynxErrorCode.SUCCESS)
        {
            MessageInfo = msgInfo;
        }

        /// <summary>
        /// Creates a new <see cref="MessagePostResponse"/>.
        /// </summary>
        /// <param name="errorInfo">Error code for message attempt.</param>
        public MessagePostResponse(SphynxErrorInfo errorInfo) : base(errorInfo)
        {
        }

        /// <inheritdoc/>
        public bool Equals(MessagePostResponse? other) => base.Equals(other) && (MessageInfo?.Equals(other?.MessageInfo) ?? false);
    }
}
