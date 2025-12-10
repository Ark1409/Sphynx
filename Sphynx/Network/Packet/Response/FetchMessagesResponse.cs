using Sphynx.Core;
using Sphynx.Model;
using Sphynx.Utils;

namespace Sphynx.Network.Packet.Response
{
    /// <inheritdoc cref="SphynxPacketType.MSG_INFO_RES"/>
    public sealed class FetchMessagesResponse : SphynxResponse, IEquatable<FetchMessagesResponse>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.MSG_INFO_RES;

        /// <summary>
        /// The resolved messages' information. The array is in decreasing order of message creation time.
        /// </summary>
        public ChatMessage[]? Messages { get; set; }

        public FetchMessagesResponse()
        {
        }

        /// <summary>
        /// Creates a new <see cref="FetchMessagesResponse"/>.
        /// </summary>
        /// <param name="errorInfo">Error code for logout attempt.</param>
        public FetchMessagesResponse(SphynxErrorInfo errorInfo) : base(errorInfo)
        {
        }

        /// <summary>
        /// Creates a new <see cref="FetchMessagesResponse"/> with <see cref="SphynxErrorCode.SUCCESS"/>.
        /// </summary>
        /// <param name="messages">The resolved messages' information.</param>
        public FetchMessagesResponse(params ChatMessage[] messages) : this(SphynxErrorCode.SUCCESS)
        {
            Messages = messages;
        }

        /// <inheritdoc/>
        public bool Equals(FetchMessagesResponse? other)
        {
            if (other is null || !base.Equals(other)) return false;
            if (Messages is null && other.Messages is null) return true;
            if (Messages is null || other.Messages is null) return false;

            return MemoryUtils.SequenceEqual(Messages, other.Messages);
        }
    }
}
