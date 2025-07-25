﻿using Sphynx.Core;
using Sphynx.ModelV2;
using Sphynx.Utils;

namespace Sphynx.Network.PacketV2.Response
{
    /// <inheritdoc cref="SphynxPacketType.MSG_INFO_RES"/>
    public sealed class FetchMessagesResponse : SphynxResponse, IEquatable<FetchMessagesResponse>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.MSG_INFO_RES;

        /// <summary>
        /// The resolved messages' information. The array is in decreasing order of message creation time.
        /// </summary>
        public ChatMessage[]? Messages { get; init; }

        /// <summary>
        /// Creates a new <see cref="FetchMessagesResponse"/>.
        /// </summary>
        /// <param name="errorCode">Error code for logout attempt.</param>
        public FetchMessagesResponse(SphynxErrorCode errorCode) : base(errorCode)
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
