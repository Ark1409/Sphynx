using Sphynx.Core;
using Sphynx.Network.Packet.Response;

namespace Sphynx.Network.Packet.Request
{
    /// <inheritdoc cref="SphynxPacketType.MSG_REQ"/>
    public sealed class MessagePostRequest : SphynxRequest<MessagePostResponse>, IEquatable<MessagePostRequest>
    {
        /// <summary>
        /// The ID of the room to which the message was sent.
        /// </summary>
        public SnowflakeId RoomId { get; set; }

        /// <summary>
        /// The contents of the chat message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.MSG_REQ;

        /// <summary>
        /// Creates a new <see cref="MessagePostRequest"/>.
        /// </summary>
        public MessagePostRequest()
        {
        }

        /// <summary>
        /// Creates a new <see cref="MessagePostRequest"/>.
        /// </summary>
        public MessagePostRequest(Guid sessionId) : base(sessionId)
        {
        }

        /// <summary>
        /// Creates a new <see cref="MessagePostRequest"/>.
        /// </summary>
        /// <param name="roomId">The ID of the room to which the message was sent.</param>
        /// <param name="message">The contents of the chat message.</param>
        public MessagePostRequest(Guid sessionId, SnowflakeId roomId, string message) : base(sessionId)
        {
            RoomId = roomId;
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        /// <inheritdoc/>
        public bool Equals(MessagePostRequest? other) =>
            base.Equals(other) && RoomId == other?.RoomId && Message == other?.Message;

        public override MessagePostResponse CreateResponse(SphynxErrorInfo errorInfo) => new MessagePostResponse(errorInfo)
        {
            RequestTag = RequestTag
        };
    }
}
