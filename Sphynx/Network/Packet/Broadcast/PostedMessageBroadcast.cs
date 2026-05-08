using Sphynx.Core;

namespace Sphynx.Network.Packet.Broadcast
{
    public class PostedMessageBroadcast : SphynxBroadcast, IEquatable<PostedMessageBroadcast>
    {
        /// <inheritdoc/>
        public override SphynxBroadcastType BroadcastType => SphynxBroadcastType.SEND_MSG_BCAST;

        /// <summary>
        /// The ID of the room to which the message was sent.
        /// </summary>
        public Guid RoomId { get; set; }

        /// <summary>
        /// The message ID of the message that was sent.
        /// </summary>
        public SnowflakeId MessageId { get; set; }

        public PostedMessageBroadcast()
        {
        }

        public PostedMessageBroadcast(Guid roomId, SnowflakeId messageId)
        {
            RoomId = roomId;
            MessageId = messageId;
        }

        /// <inheritdoc/>
        public bool Equals(PostedMessageBroadcast? other) => base.Equals(other) && RoomId == other?.RoomId && MessageId == other?.MessageId;
    }
}
