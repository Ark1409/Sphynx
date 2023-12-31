using Sphynx.Utils;

namespace Sphynx.Packet.Request
{
    /// <summary>
    /// The packet header for a request sent from a client to the server.
    /// </summary>
    public sealed class SphynxRequestHeader : SphynxPacketHeader, IEquatable<SphynxRequestHeader>
    {
        /// <summary>
        /// The size of a request header in bytes.
        /// </summary>
        /// <remarks>
        /// Offset of last element + size of last element
        /// </remarks>
        public const int HEADER_SIZE = CONTENT_SIZE_OFFSET + sizeof(int);

        /// <summary>
        /// <see langword="sizeof"/>(<see cref="Guid"/>).
        /// </summary>
        private const int GUID_SIZE = 16;

        /// <summary>
        /// The user ID of the requesting user.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// The session ID for the requesting user.
        /// </summary>
        public Guid SessionId { get; set; }

        /// <inheritdoc/>
        public override int HeaderSize => HEADER_SIZE;

        private const int SIGNATURE_OFFSET = 0;
        private const int PACKET_TYPE_OFFSET = SIGNATURE_OFFSET + sizeof(ushort);
        private const int USER_ID_OFFSET = PACKET_TYPE_OFFSET + sizeof(SphynxPacketType);
        private const int SESSION_ID_OFFSET = USER_ID_OFFSET + GUID_SIZE;
        private const int CONTENT_SIZE_OFFSET = SESSION_ID_OFFSET + GUID_SIZE;

        /// <summary>
        /// Creates a new <see cref="SphynxRequestHeader"/> from raw packet bytes.
        /// </summary>
        /// <param name="packetHeader">The raw packet bytes.</param>
        public SphynxRequestHeader(byte[] packetHeader) : this(new ReadOnlySpan<byte>(packetHeader))
        {

        }

        /// <summary>
        /// Creates a new <see cref="SphynxRequestHeader"/> from raw packet bytes.
        /// </summary>
        /// <param name="packetHeader">The raw packet header bytes.</param>
        public SphynxRequestHeader(ReadOnlySpan<byte> packetHeader) : base(SphynxPacketType.NOP, default)
        {
            // Avoid exception on the server
            if (packetHeader.Length == HEADER_SIZE && SIGNATURE == packetHeader.ReadUInt16(SIGNATURE_OFFSET))
            {
                PacketType = (SphynxPacketType)packetHeader.ReadUInt32(PACKET_TYPE_OFFSET);

                if (IsRequest(PacketType))
                    throw new InvalidDataException($"Raw packet ({PacketType}) type must be request packet");

                UserId = new Guid(packetHeader.Slice(USER_ID_OFFSET, GUID_SIZE));
                SessionId = new Guid(packetHeader.Slice(SESSION_ID_OFFSET, GUID_SIZE));
                ContentSize = packetHeader.ReadInt32(CONTENT_SIZE_OFFSET);
            }
        }

        /// <summary>
        /// Creates a new <see cref="SphynxRequestHeader"/>.
        /// </summary>
        /// <param name="packetType">The type of packet.</param>
        /// <param name="contentSize">The size of the packet's contents.</param>
        public SphynxRequestHeader(SphynxPacketType packetType, int contentSize) : this(packetType, Guid.Empty, Guid.Empty, contentSize)
        {

        }

        /// <summary>
        /// Creates a new <see cref="SphynxRequestHeader"/>.
        /// </summary>
        /// <param name="packetType">The type of packet.</param>
        /// <param name="userId">The user ID of the requesting user.</param>
        /// <param name="sessionId">The session ID for the requesting user.</param>
        /// <param name="contentSize">The size of the packet's contents.</param>
        public SphynxRequestHeader(SphynxPacketType packetType, Guid userId, Guid sessionId, int contentSize) : base(packetType, contentSize)
        {
            // Exception are accptable on the client
            if (IsRequest(packetType))
            {
                UserId = userId;
                SessionId = sessionId;
            }
            else
            {
                PacketType = SphynxPacketType.NOP;
            }
        }

        /// <inheritdoc/>
        public override void Serialize(Span<byte> buffer)
        {
            // Exception are accptable on the client
            if (buffer.Length < HEADER_SIZE)
                throw new ArgumentException($"Cannot serialize response header into {buffer.Length} bytes");

            SIGNATURE.WriteBytes(buffer, SIGNATURE_OFFSET);
            ((uint)PacketType).WriteBytes(buffer, PACKET_TYPE_OFFSET);

            // Assume it writes; already performed length check
            UserId.TryWriteBytes(buffer.Slice(USER_ID_OFFSET, GUID_SIZE));
            SessionId.TryWriteBytes(buffer.Slice(SESSION_ID_OFFSET, GUID_SIZE));

            ContentSize.WriteBytes(buffer, CONTENT_SIZE_OFFSET);
        }

        /// <inheritdoc/>
        public override bool Equals(SphynxPacketHeader? other) => other is SphynxRequestHeader req && Equals(req);

        /// <inheritdoc/>
        public bool Equals(SphynxRequestHeader? other) => base.Equals(other) && UserId == other?.UserId && SessionId == other?.SessionId;
    }
}
