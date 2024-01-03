using System.Diagnostics.CodeAnalysis;

namespace Sphynx.Packet.Request
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

        protected const int USER_ID_OFFSET = 0;
        protected const int SESSION_ID_OFFSET = USER_ID_OFFSET + GUID_SIZE;
        protected const int DEFAULT_CONTENT_SIZE = SESSION_ID_OFFSET + GUID_SIZE;

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
        /// Attempts to retrieve a <see cref="UserId"/> and <see cref="SessionId"/> from the raw <paramref name="buffer"/> bytes.
        /// </summary>
        /// <param name="buffer">The raw bytes to deserialize the data from.</param>
        /// <param name="userId">The deserialized user ID.</param>
        /// <param name="sessionId">The deserialized session ID.</param>
        /// <returns>true if the data could be deserialized; false otherwise.</returns>
        protected static bool TryDeserialize(ReadOnlySpan<byte> buffer, [NotNullWhen(true)] out Guid? userId, [NotNullWhen(true)] out Guid? sessionId)
        {
            if (buffer.Length >= GUID_SIZE * 2)
            {
                userId = new Guid(buffer.Slice(USER_ID_OFFSET, GUID_SIZE));
                sessionId = new Guid(buffer.Slice(SESSION_ID_OFFSET, GUID_SIZE));
                return true;
            }

            userId = null;
            sessionId = null;
            return false;
        }

        /// <summary>
        /// Attempts to serialize this <see cref="SphynxRequestPacket"/> into the <paramref name="buffer"/>.
        /// </summary>
        /// <param name="buffer">The buffer to serialize this packet into.</param>
        /// <returns>true if this packet could be serialized; false otherwise.</returns>
        protected virtual bool TrySerialize(Span<byte> buffer)
        {
            if (buffer.Length >= GUID_SIZE * 2)
            {
                UserId.TryWriteBytes(buffer[..GUID_SIZE]);
                SessionId.TryWriteBytes(buffer.Slice(SESSION_ID_OFFSET, GUID_SIZE));
                return true;
            }

            return false;
        }

        /// <summary>
        /// Indicates whether the current packet has the same user and session ID as another request packet.
        /// </summary>
        /// <param name="other">A request packet to compare with this request packet.</param>
        /// <returns>true if the current packet is equal to the other parameter; otherwise, false.</returns>
        protected bool Equals(SphynxRequestPacket? other) => base.Equals(other) && UserId == other?.UserId && SessionId == other?.SessionId;
    }
}