using Sphynx.Core;

namespace Sphynx.Network.PacketV2.Response
{
    /// <inheritdoc cref="SphynxPacketType.MSG_RES"/>
    public sealed class MessagePostResponse : SphynxResponse, IEquatable<MessagePostResponse>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.MSG_RES;

        /// <summary>
        /// Creates a new <see cref="MessagePostResponse"/>.
        /// </summary>
        /// <param name="errorInfo">Error code for message attempt.</param>
        public MessagePostResponse(SphynxErrorCode errorInfo = SphynxErrorCode.SUCCESS) : base(errorInfo)
        {
        }

        /// <summary>
        /// Creates a new <see cref="MessagePostResponse"/>.
        /// </summary>
        /// <param name="errorInfo">Error code for message attempt.</param>
        public MessagePostResponse(SphynxErrorInfo errorInfo) : base(errorInfo)
        {
        }

        /// <inheritdoc/>
        public bool Equals(MessagePostResponse? other) => base.Equals(other);
    }
}
