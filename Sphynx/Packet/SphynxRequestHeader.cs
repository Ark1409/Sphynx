using Sphynx.Utils;

namespace Sphynx.Packet
{
    /// <summary>
    /// The packet header for a request sent from a client to the server.
    /// </summary>
    public sealed class SphynxRequestHeader : SphynxPacketHeader, IEquatable<SphynxRequestHeader>
    {
        /// <summary>
        /// The size of a request header in bytes.
        /// </summary>
        public const int HEADER_SIZE = 42;

        /// <summary>
        /// <see langword="sizeof"/>(<see cref="Guid"/>).
        /// </summary>
        private const int GUID_SIZE = 16;

        private const int SIGNATURE_OFFSET = 0;
        private const int PACKET_TYPE_OFFSET = SIGNATURE_OFFSET + sizeof(ushort);
        private const int USER_ID_OFFSET = PACKET_TYPE_OFFSET + sizeof(SphynxPacketType);
        private const int SESSION_ID_OFFSET = USER_ID_OFFSET + GUID_SIZE;
        private const int CONTENT_SIZE_OFFSET = SESSION_ID_OFFSET + GUID_SIZE;

        /// <summary>
        /// The user ID of the requesting user.
        /// </summary>
        public Guid UserId { get; }

        /// <summary>
        /// The session ID for the requesting user.
        /// </summary>
        public Guid SessionId { get; }

        /// <inheritdoc/>
        public override int HeaderSize => HEADER_SIZE;

        /// <summary>
        /// Creates a new <see cref="SphynxRequestHeader"/> from raw packet bytes.
        /// </summary>
        /// <param name="packet">The raw packet bytes.</param>
        public SphynxRequestHeader(byte[] packet) : this(new ReadOnlySpan<byte>(packet))
        {

        }

        /// <summary>
        /// Creates a new <see cref="SphynxRequestHeader"/> from raw packet bytes.
        /// </summary>
        /// <param name="packet">The raw packet bytes.</param>
        public SphynxRequestHeader(ReadOnlySpan<byte> packet) : base(SphynxPacketType.NOP, packet.Length)
        {
            if (packet.Length != HEADER_SIZE)
                throw new ArgumentException("Raw packet is not of valid size", nameof(packet));

            if (SIGNATURE != packet.Slice(SIGNATURE_OFFSET, sizeof(ushort)).ReadUInt16())
                throw new ArgumentException("Packet unidentifiable", nameof(packet));

            PacketType = (SphynxPacketType)packet.Slice(PACKET_TYPE_OFFSET, sizeof(SphynxPacketType)).ReadUInt32();

            if (((int)PacketType) < 0)
                throw new InvalidDataException($"Raw packet ({PacketType}) type must be request packet");

            UserId = new Guid(packet.Slice(USER_ID_OFFSET, GUID_SIZE));
            SessionId = new Guid(packet.Slice(SESSION_ID_OFFSET, GUID_SIZE));
            ContentSize = packet.Slice(CONTENT_SIZE_OFFSET, sizeof(int)).ReadInt32();
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
            // Check if first bit on
            if (((int)packetType) < 0)
                throw new ArgumentException("Packet type must be request packet", nameof(packetType));

            UserId = userId;
            SessionId = sessionId;
        }

        /// <inheritdoc/>
        public override void Serialize(Span<byte> buffer)
        {
            if (buffer.Length < HEADER_SIZE)
                throw new ArgumentException($"Cannot serialize response header into {buffer.Length} bytes");

            SIGNATURE.WriteBytes(buffer.Slice(SIGNATURE_OFFSET, sizeof(ushort)));
            ((uint)PacketType).WriteBytes(buffer.Slice(PACKET_TYPE_OFFSET, sizeof(SphynxPacketType)));

            // Prepare NOP packet on failure
            if (!UserId.TryWriteBytes(buffer.Slice(USER_ID_OFFSET, GUID_SIZE)) || !SessionId.TryWriteBytes(buffer.Slice(SESSION_ID_OFFSET, GUID_SIZE)))
            {
                ((uint)SphynxPacketType.NOP).WriteBytes(buffer.Slice(PACKET_TYPE_OFFSET, sizeof(SphynxPacketType)));
            }

            ContentSize.WriteBytes(buffer.Slice(CONTENT_SIZE_OFFSET, sizeof(int)));
        }

        /// <inheritdoc/>
        public override bool Equals(SphynxPacketHeader? other) => other is SphynxRequestHeader req &&
            base.Equals(other) && UserId == req.UserId && SessionId == req.SessionId;

        /// <inheritdoc/>
        public bool Equals(SphynxRequestHeader? other) => Equals(other as SphynxPacketHeader);
    }
}
