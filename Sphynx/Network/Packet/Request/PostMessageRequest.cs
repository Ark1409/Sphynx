using Sphynx.Core;
using Sphynx.Network.Packet.Response;

namespace Sphynx.Network.Packet.Request
{
    public class PostMessageRequest : SphynxRequest<PostMessageResponse>, IEquatable<PostMessageRequest>
    {
        /// <inheritdoc/>
        public override SphynxRequestType RequestType => SphynxRequestType.SEND_MSG_REQ;

        /// <summary>
        /// The ID of the room to send the message to.
        /// </summary>
        public Guid RoomId { get; set; }

        /// <summary>
        /// The contents of the chat message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        public PostMessageRequest()
        {
        }

        /// <summary>
        /// Creates a new <see cref="PostMessageRequest"/>.
        /// </summary>
        /// <param name="roomId">The ID of the room to which the message was sent.</param>
        /// <param name="message">The contents of the chat message.</param>
        public PostMessageRequest(Guid roomId, string message)
        {
            RoomId = roomId;
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        /// <inheritdoc/>
        public bool Equals(PostMessageRequest? other) =>
            base.Equals(other) && RoomId == other?.RoomId && Message == other?.Message;

        public override PostMessageResponse CreateResponse(SphynxErrorInfo errorInfo) => new PostMessageResponse(errorInfo);
    }
}
