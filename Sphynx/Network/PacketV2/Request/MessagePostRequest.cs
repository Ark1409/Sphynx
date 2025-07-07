using Sphynx.Core;

namespace Sphynx.Network.PacketV2.Request
{
    /// <inheritdoc cref="SphynxPacketType.MSG_REQ"/>
    public sealed class MessagePostRequest : SphynxRequest, IEquatable<MessagePostRequest>
    {
        /// <summary>
        /// The ID of the room to which the message was sent.
        /// </summary>
        public SnowflakeId RoomId { get; init; }

        /// <summary>
        /// The contents of the chat message.
        /// </summary>
        public string Message { get; init; } = string.Empty;

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.MSG_REQ;

        /// <summary>
        /// Creates a new <see cref="MessagePostRequest"/>.
        /// </summary>
        /// <param name="userId">The user ID of the requesting user.</param>
        /// <param name="sessionId">The session ID for the requesting user.</param>
        public MessagePostRequest(SnowflakeId userId, Guid sessionId) : base(userId, sessionId)
        {
        }

        /// <summary>
        /// Creates a new <see cref="MessagePostRequest"/>.
        /// </summary>
        /// <param name="userId">The user ID of the requesting user.</param>
        /// <param name="sessionId">The session ID for the requesting user.</param>
        /// <param name="roomId">The ID of the room to which the message was sent.</param>
        /// <param name="message">The contents of the chat message.</param>
        public MessagePostRequest(SnowflakeId userId, Guid sessionId, SnowflakeId roomId, string message)
            : base(userId, sessionId)
        {
            RoomId = roomId;
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        /// <inheritdoc/>
        public bool Equals(MessagePostRequest? other) =>
            base.Equals(other) && RoomId == other?.RoomId && Message == other?.Message;
    }
}
