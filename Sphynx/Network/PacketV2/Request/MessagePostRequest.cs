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
        /// <param name="accessToken">The JWT access token for this request.</param>
        public MessagePostRequest(string accessToken) : base(accessToken)
        {
        }

        /// <summary>
        /// Creates a new <see cref="MessagePostRequest"/>.
        /// </summary>
        /// <param name="accessToken">The JWT access token for this request.</param>
        /// <param name="roomId">The ID of the room to which the message was sent.</param>
        /// <param name="message">The contents of the chat message.</param>
        public MessagePostRequest(string accessToken, SnowflakeId roomId, string message) : base(accessToken)
        {
            RoomId = roomId;
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        /// <inheritdoc/>
        public bool Equals(MessagePostRequest? other) =>
            base.Equals(other) && RoomId == other?.RoomId && Message == other?.Message;
    }
}
