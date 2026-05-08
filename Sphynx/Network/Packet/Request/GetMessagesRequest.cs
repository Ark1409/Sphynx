using Sphynx.Core;
using Sphynx.Network.Packet.Response;

namespace Sphynx.Network.Packet.Request
{
    public class GetMessagesRequest : SphynxRequest<GetMessagesResponse>, IEquatable<GetMessagesRequest>
    {
        /// <inheritdoc/>
        public override SphynxRequestType RequestType => SphynxRequestType.GET_MSG_REQ;

        /// <summary>
        /// The maximum number of messages which can be requested at once.
        /// </summary>
        public const int MAX_MESSAGES_COUNT = 100;

        /// <summary>
        /// The message from which older messages should be retrieved.
        /// </summary>
        public SnowflakeId BeforeId { get; init; }

        /// <summary>
        /// The room ID from which the message belongs.
        /// </summary>
        public Guid RoomId { get; init; }

        /// <summary>
        /// The number of older messages to retrieve, starting from <see cref="BeforeId"/>.
        /// </summary>
        /// <remarks>Maximum value of <see cref="MAX_MESSAGES_COUNT"/>.</remarks>
        public int Count
        {
            get => _count;
            set => _count = value > MAX_MESSAGES_COUNT ? MAX_MESSAGES_COUNT : value;
        }

        private int _count;

        /// <summary>
        /// Whether to include the message with id <see cref="BeforeId"/> (if it exists) in the response.
        /// </summary>
        public bool Inclusive { get; set; }

        public GetMessagesRequest()
        {
        }

        /// <summary>
        /// Creates a new <see cref="GetMessagesRequest"/>.
        /// </summary>
        /// <param name="beforeId">The message from which older messages should be retrieved.</param>
        /// <param name="roomId">The room ID from which the message belongs.</param>
        /// <param name="count">The number of messages to retrieve, starting from <see cref="BeforeId"/>.</param>
        /// <param name="inclusive">Whether to include the message with id <see cref="BeforeId"/> (if it exists) in the
        /// response.</param>
        public GetMessagesRequest(SnowflakeId beforeId, Guid roomId, int count, bool inclusive = false)
        {
            BeforeId = beforeId;
            Count = count;
            RoomId = roomId;
            Inclusive = inclusive;
        }

        /// <inheritdoc/>
        public bool Equals(GetMessagesRequest? other) => base.Equals(other)
                                                           && BeforeId == other?.BeforeId
                                                           && RoomId == other?.RoomId
                                                           && Count == other.Count
                                                           && Inclusive == other.Inclusive;

        public override GetMessagesResponse CreateResponse(SphynxErrorInfo errorInfo) => new GetMessagesResponse(errorInfo);
    }
}
