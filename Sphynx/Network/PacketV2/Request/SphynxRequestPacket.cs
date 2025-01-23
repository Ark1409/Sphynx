using System.Diagnostics.CodeAnalysis;
using Sphynx.Core;

namespace Sphynx.Network.PacketV2.Request
{
    /// <summary>
    /// Represents a request packet.
    /// </summary>
    public abstract class SphynxRequestPacket : SphynxPacket
    {
        /// <summary>
        /// The user ID of the requesting user.
        /// </summary>
        public SnowflakeId UserId { get; set; }

        /// <summary>
        /// The session ID for the requesting user.
        /// </summary>s
        public Guid SessionId { get; set; }

        protected static readonly int USER_ID_OFFSET = 0;
        protected static readonly int SESSION_ID_OFFSET = USER_ID_OFFSET + SnowflakeId.SIZE;
        protected static readonly int DEFAULT_CONTENT_SIZE = SnowflakeId.SIZE + GUID_SIZE;

        /// <summary>
        /// Creates a new <see cref="SphynxRequestPacket"/>.
        /// </summary>
        public SphynxRequestPacket() : this(SnowflakeId.Empty, Guid.Empty)
        {
        }

        /// <summary>
        /// Creates a new <see cref="SphynxRequestPacket"/>.
        /// </summary>
        /// <param name="userId">The user ID of the requesting user.</param>
        /// <param name="sessionId">The session ID for the requesting user.</param>
        public SphynxRequestPacket(SnowflakeId userId, Guid sessionId)
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
        protected static bool TryDeserializeDefaults(
            ReadOnlySpan<byte> buffer,
            [NotNullWhen(true)] out SnowflakeId? userId,
            [NotNullWhen(true)] out Guid? sessionId)
        {
            if (buffer.Length < DEFAULT_CONTENT_SIZE)
            {
                userId = null;
                sessionId = null;
                return false;
            }

            userId = new SnowflakeId(buffer.Slice(USER_ID_OFFSET, SnowflakeId.SIZE));
            sessionId = new Guid(buffer.Slice(SESSION_ID_OFFSET, GUID_SIZE));
            return true;
        }

        /// <summary>
        /// Attempts to serialize the default properties (<see cref="UserId"/> and <see cref="SessionId"/>)
        /// of this <see cref="SphynxRequestPacket"/> into the <paramref name="buffer"/>.
        /// </summary>
        /// <param name="buffer">The buffer to serialize this packet into.</param>
        /// <returns>true if the default properties could be serialized; false otherwise.</returns>
        protected virtual bool TrySerializeDefaults(Span<byte> buffer)
        {
            if (buffer.Length < DEFAULT_CONTENT_SIZE || UserId == SnowflakeId.Empty || SessionId == Guid.Empty)
            {
                return false;
            }

            UserId.TryWriteBytes(buffer.Slice(USER_ID_OFFSET, SnowflakeId.SIZE));
            SessionId.TryWriteBytes(buffer.Slice(SESSION_ID_OFFSET, GUID_SIZE));
            return true;
        }

        /// <summary>
        /// Indicates whether the current packet has the same user and session ID as another request packet.
        /// </summary>
        /// <param name="other">A request packet to compare with this request packet.</param>
        /// <returns>true if the current packet is equal to the other parameter; otherwise, false.</returns>
        protected bool Equals(SphynxRequestPacket? other) =>
            base.Equals(other) && UserId == other?.UserId && SessionId == other?.SessionId;
    }
}
