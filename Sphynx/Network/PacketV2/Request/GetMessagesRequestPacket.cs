using Sphynx.Core;

namespace Sphynx.Network.PacketV2.Request
{
    /// <inheritdoc cref="SphynxPacketType.MSG_INFO_REQ"/>
    public sealed class GetMessagesRequestPacket : SphynxRequestPacket, IEquatable<GetMessagesRequestPacket>
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
        public SnowflakeId SinceId { get; set; }

        /// <summary>
        /// The room ID from which the message belongs.
        /// </summary>
        public SnowflakeId RoomId { get; set; }

        /// <summary>
        /// The number of older messages to retrieve, starting from <see cref="SinceId"/>.
        /// </summary>
        /// <remarks>Maximum value of <see cref="MAX_MESSAGES_COUNT"/>.</remarks>
        public int Count { get; set; }

        /// <summary>
        /// Whether to include the message with id <see cref="SinceId"/> (if it exists) in the response.
        /// </summary>
        public bool Inclusive { get; set; } = false;

        /// <summary>
        /// Creates a new <see cref="GetMessagesRequestPacket"/>.
        /// </summary>
        /// <param name="userId">The user ID of the requesting user.</param>
        /// <param name="sessionId">The session ID for the requesting user.</param>
        public GetMessagesRequestPacket(SnowflakeId userId, Guid sessionId) : base(userId, sessionId)
        {
        }

        /// <summary>
        /// Creates a new <see cref="GetMessagesRequestPacket"/>.
        /// </summary>
        /// <param name="userId">The user ID of the requesting user.</param>
        /// <param name="sessionId">The session ID for the requesting user.</param>
        /// <param name="sinceId">The message from which older messages should be retrieved.</param>
        /// <param name="roomId">The room ID from which the message belongs.</param>
        /// <param name="count">The number of messages to retrieve, starting from <see cref="SinceId"/>.</param>
        /// <param name="inclusive">Whether to include the message with id <see cref="SinceId"/> (if it exists) in the
        /// response.</param>
        public GetMessagesRequestPacket(
            SnowflakeId userId,
            Guid sessionId,
            SnowflakeId sinceId,
            SnowflakeId roomId,
            int count,
            bool inclusive = false)
            : base(userId, sessionId)
        {
            SinceId = sinceId;
            Count = count;
            RoomId = roomId;
            Inclusive = inclusive;
        }

        /// <inheritdoc/>
        public bool Equals(GetMessagesRequestPacket? other) => base.Equals(other)
                                                               && SinceId == other?.SinceId
                                                               && RoomId == other?.RoomId
                                                               && Count == other.Count
                                                               && Inclusive == other.Inclusive;
    }
}
