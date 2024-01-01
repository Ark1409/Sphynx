using Sphynx.Packet.Request;

namespace Sphynx.Packet
{
    /// <summary>
    /// Represents a request packet.
    /// </summary>
    public abstract class SphynxRequestPacket : SphynxPacket
    {
        /// <summary>
        /// The user ID of the requesting user.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// The session ID for the requesting user.
        /// </summary>s
        public Guid SessionId { get; set; }

        /// <summary>
        /// Creates a new <see cref="SphynxRequestPacket"/>.
        /// </summary>
        public SphynxRequestPacket() : this(Guid.Empty, Guid.Empty)
        {

        }

        /// <summary>
        /// Creates a new <see cref="SphynxRequestPacket"/>.
        /// </summary>
        /// <param name="userId">The user ID of the requesting user.</param>
        /// <param name="sessionId">The session ID for the requesting user.</param>
        public SphynxRequestPacket(Guid userId, Guid sessionId)
        {
            UserId = userId;
            SessionId = sessionId;
        }

        /// <summary>
        /// Serializes a packet header into the specified <paramref name="buffer"/>.
        /// </summary>
        /// <param name="buffer">The buffer to serialize this header into.</param>
        /// <param name="contentSize">The <see cref="SphynxPacketHeader.ContentSize"/>.</param>
        protected override SphynxRequestHeader SerializeHeader(Span<byte> buffer, int contentSize)
        {
            var header = new SphynxRequestHeader(PacketType, UserId, SessionId, contentSize);
            header.Serialize(buffer.Slice(0, SphynxRequestHeader.HEADER_SIZE));
            return header;
        }
    }
}