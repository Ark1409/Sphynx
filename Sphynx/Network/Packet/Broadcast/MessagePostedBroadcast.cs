using Sphynx.Core;

namespace Sphynx.Network.Packet.Broadcast
{
    /// <inheritdoc cref="SphynxPacketType.MSG_BCAST"/>
    public class MessagePostedBroadcast : SphynxPacket, IEquatable<MessagePostedBroadcast>
    {
        /// <summary>
        /// The ID of the room to which the message was sent.
        /// </summary>
        public Guid RoomId { get; set; }

        /// <summary>
        /// The message ID of the message that was sent.
        /// </summary>
        public SnowflakeId MessageId { get; set; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.MSG_BCAST;

        public MessagePostedBroadcast()
        {
        }

        /// <summary>
        /// Creates a new <see cref="MessageRequestPacket"/>, assuming the message is for a user.
        /// </summary>
        /// <param name="roomId">The ID of the room to which the message was sent.</param>
        /// <param name="messageId">The message ID of the message that was sent.</param>
        public MessagePostedBroadcast(Guid roomId, SnowflakeId messageId)
        {
            RoomId = roomId;
            MessageId = messageId;
        }

        /// <inheritdoc/>
        public bool Equals(MessagePostedBroadcast? other) =>
            base.Equals(other) && RoomId == other?.RoomId && MessageId == other?.MessageId;
    }
}
