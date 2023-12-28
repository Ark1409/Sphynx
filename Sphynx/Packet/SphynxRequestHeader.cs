using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Sphynx.Packet
{
    /// <summary>
    /// The packet header for a request sent from a client to the server.
    /// </summary>
    public sealed class SphynxRequestHeader : SphynxPacketHeader
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

            if (!VerifySignature(packet.Slice(SIGNATURE_OFFSET, sizeof(ushort))))
                throw new ArgumentException("Packet unidentifiable", nameof(packet));

            PacketType = (SphynxPacketType)MemoryMarshal.Cast<byte, uint>(packet.Slice(PACKET_TYPE_OFFSET, sizeof(SphynxPacketType)))[0];

            if (((int)PacketType) < 0)
                throw new ArgumentException($"Raw packet ({PacketType}) type must be request packet", nameof(PacketType));

            UserId = new Guid(packet.Slice(USER_ID_OFFSET, GUID_SIZE));
            SessionId = new Guid(packet.Slice(SESSION_ID_OFFSET, GUID_SIZE));
            ContentSize = MemoryMarshal.Cast<byte, int>(packet.Slice(CONTENT_SIZE_OFFSET, sizeof(int)))[0];
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

            SerializeSignature(buffer.Slice(SIGNATURE_OFFSET, sizeof(ushort)));
            SerializePacketType(buffer.Slice(PACKET_TYPE_OFFSET, sizeof(SphynxPacketType)), PacketType);

            // Prepare NOP packet on failure
            if (!UserId.TryWriteBytes(buffer.Slice(USER_ID_OFFSET, GUID_SIZE)) || !SessionId.TryWriteBytes(buffer.Slice(SESSION_ID_OFFSET, GUID_SIZE)))
            {
                SerializePacketType(buffer.Slice(PACKET_TYPE_OFFSET, sizeof(SphynxPacketType)), SphynxPacketType.NOP);
            }

            SerializeContentSize(buffer.Slice(CONTENT_SIZE_OFFSET, sizeof(int)));
        }
    }
}
