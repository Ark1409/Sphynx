using Sphynx.Core;
using Sphynx.ModelV2;
using Sphynx.Utils;

namespace Sphynx.Network.PacketV2.Response
{
    /// <inheritdoc cref="SphynxPacketType.MSG_INFO_RES"/>
    public sealed class GetMessagesResponsePacket : SphynxResponsePacket, IEquatable<GetMessagesResponsePacket>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.MSG_INFO_RES;

        /// <summary>
        /// The resolved messages' information. The array is in decreasing order of message creation time.
        /// </summary>
        public IChatMessage[]? Messages { get; init; }

        /// <summary>
        /// Creates a new <see cref="GetMessagesResponsePacket"/>.
        /// </summary>
        /// <param name="errorCode">Error code for logout attempt.</param>
        public GetMessagesResponsePacket(SphynxErrorCode errorCode) : base(errorCode)
        {
        }

        /// <summary>
        /// Creates a new <see cref="GetMessagesResponsePacket"/> with <see cref="SphynxErrorCode.SUCCESS"/>.
        /// </summary>
        /// <param name="messages">The resolved messages' information.</param>
        public GetMessagesResponsePacket(params IChatMessage[] messages) : this(SphynxErrorCode.SUCCESS)
        {
            Messages = messages;
        }

        /// <inheritdoc/>
        public bool Equals(GetMessagesResponsePacket? other)
        {
            if (other is null || !base.Equals(other)) return false;
            if (Messages is null && other.Messages is null) return true;
            if (Messages is null || other.Messages is null) return false;

            return MemoryUtils.SequenceEqual(Messages, other.Messages);
        }
    }
}
