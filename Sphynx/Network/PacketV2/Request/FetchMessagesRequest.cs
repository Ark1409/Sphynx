using Sphynx.Core;
using Sphynx.Network.PacketV2.Response;

namespace Sphynx.Network.PacketV2.Request
{
    /// <inheritdoc cref="SphynxPacketType.MSG_INFO_REQ"/>
    public sealed class FetchMessagesRequest : SphynxRequest<FetchMessagesResponse>, IEquatable<FetchMessagesRequest>
    {
        /// <summary>
        /// The maximum number of messages which can be requested at once.
        /// </summary>
        public const int MAX_MESSAGES_COUNT = 50;

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.MSG_INFO_REQ;

        /// <summary>
        /// The message from which older messages should be retrieved.
        /// </summary>
        public SnowflakeId BeforeId { get; init; }

        /// <summary>
        /// The room ID from which the message belongs.
        /// </summary>
        public SnowflakeId RoomId { get; init; }

        /// <summary>
        /// The number of older messages to retrieve, starting from <see cref="BeforeId"/>.
        /// </summary>
        /// <remarks>Maximum value of <see cref="MAX_MESSAGES_COUNT"/>.</remarks>
        public int Count
        {
            get => _count;
            init => _count = value > MAX_MESSAGES_COUNT ? MAX_MESSAGES_COUNT : value;
        }

        private int _count;

        /// <summary>
        /// Whether to include the message with id <see cref="BeforeId"/> (if it exists) in the response.
        /// </summary>
        public bool Inclusive { get; set; }

        /// <summary>
        /// Creates a new <see cref="FetchMessagesRequest"/>.
        /// </summary>
        /// <param name="accessToken">The JWT access token for this request.</param>
        public FetchMessagesRequest(string accessToken) : base(accessToken)
        {
        }

        /// <summary>
        /// Creates a new <see cref="FetchMessagesRequest"/>.
        /// </summary>
        /// <param name="accessToken">The JWT access token for this request.</param>
        /// <param name="beforeId">The message from which older messages should be retrieved.</param>
        /// <param name="roomId">The room ID from which the message belongs.</param>
        /// <param name="count">The number of messages to retrieve, starting from <see cref="BeforeId"/>.</param>
        /// <param name="inclusive">Whether to include the message with id <see cref="BeforeId"/> (if it exists) in the
        /// response.</param>
        public FetchMessagesRequest(string accessToken, SnowflakeId beforeId, SnowflakeId roomId, int count, bool inclusive = false)
            : base(accessToken)
        {
            BeforeId = beforeId;
            Count = count;
            RoomId = roomId;
            Inclusive = inclusive;
        }

        /// <inheritdoc/>
        public bool Equals(FetchMessagesRequest? other) => base.Equals(other)
                                                           && BeforeId == other?.BeforeId
                                                           && RoomId == other?.RoomId
                                                           && Count == other.Count
                                                           && Inclusive == other.Inclusive;

        public override FetchMessagesResponse CreateResponse(SphynxErrorInfo errorInfo) => new FetchMessagesResponse(errorInfo);
    }
}
