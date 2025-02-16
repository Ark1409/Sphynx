using Sphynx.Core;
using Sphynx.Network.Packet.Request;

namespace Sphynx.Network.PacketV2.Broadcast
{
    /// <inheritdoc cref="SphynxPacketType.MSG_BCAST"/>
    public class SendMessageBroadcastPacket : SphynxPacket, IEquatable<SendMessageBroadcastPacket>
    {
        /// <summary>
        /// The ID of the room to which the message was sent.
        /// </summary>
        public SnowflakeId RoomId { get; init; }

        /// <summary>
        /// The message ID of the message that was sent.
        /// </summary>
        public SnowflakeId MessageId { get; init; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.MSG_BCAST;

        /// <summary>
        /// Creates a new <see cref="MessageRequestPacket"/>, assuming the message is for a user.
        /// </summary>
        /// <param name="roomId">The ID of the room to which the message was sent.</param>
        /// <param name="messageId">The message ID of the message that was sent.</param>
        public SendMessageBroadcastPacket(SnowflakeId roomId, SnowflakeId messageId)
        {
            RoomId = roomId;
            MessageId = messageId;
        }

        /// <inheritdoc/>
        public bool Equals(SendMessageBroadcastPacket? other) =>
            base.Equals(other) && RoomId == other?.RoomId && MessageId == other?.MessageId;
    }
}
