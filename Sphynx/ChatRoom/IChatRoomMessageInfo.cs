using Sphynx.Packet.Broadcast;
using Sphynx.Packet.Request;

namespace Sphynx.ChatRoom
{
    /// <summary>
    /// Represents a single message within a chat room.
    /// </summary>
    public interface IChatRoomMessageInfo
    {
        /// <summary>
        /// The timestamp for this message.
        /// </summary>
        public DateTimeOffset Timestamp { get; }

        /// <summary>
        /// THe chat room to which this message was sent.
        /// </summary>
        public Guid RoomId { get; }

        /// <summary>
        /// The user ID of the message sender.
        /// </summary>
        public Guid SenderId { get; }

        /// <summary>
        /// The message content.
        /// </summary>
        public string Content { get; }
    }
}