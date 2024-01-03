using System.Diagnostics.CodeAnalysis;

namespace Sphynx.Packet.Response
{
    /// <inheritdoc cref="SphynxPacketType.CHAT_CREATE_RES"/>
    public sealed class ChatCreateResponsePacket : SphynxResponsePacket, IEquatable<ChatCreateResponsePacket>
    {
        /// <summary>
        /// Room ID assigned to the newly created room.
        /// </summary>
        public Guid? RoomId { get; set; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.CHAT_CREATE_RES;

        private const int ROOM_ID_OFFSET = DEFAULT_CONTENT_SIZE;

        /// <summary>
        /// Creates a new <see cref="ChatCreateResponsePacket"/>.
        /// </summary>
        /// <param name="errorCode">Error code for room creation attempt.</param>
        public ChatCreateResponsePacket(SphynxErrorCode errorCode) : base(errorCode)
        {
            // Assume the room is to be created
            if (errorCode == SphynxErrorCode.SUCCESS)
            {
                RoomId = Guid.NewGuid();
            }
        }

        /// <summary>
        /// Creates a new <see cref="ChatCreateResponsePacket"/>.
        /// </summary>
        /// <param name="roomId">Room ID assigned to the newly created room.</param>
        public ChatCreateResponsePacket(Guid roomId) : base(SphynxErrorCode.SUCCESS)
        {
            RoomId = roomId;
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="ChatCreateResponsePacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        /// <param name="packet">The deserialized packet.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out ChatCreateResponsePacket? packet)
        {
            if (!TryDeserialize(contents, out SphynxErrorCode? errorCode) ||
                (errorCode.Value == SphynxErrorCode.SUCCESS && contents.Length < ROOM_ID_OFFSET + GUID_SIZE))
            {
                packet = null;
                return false;
            }

            if (errorCode.Value == SphynxErrorCode.SUCCESS)
            {
                var roomId = new Guid(contents.Slice(ROOM_ID_OFFSET, GUID_SIZE));
                packet = new ChatCreateResponsePacket(roomId);
            }
            else
            {
                packet = new ChatCreateResponsePacket(errorCode.Value);
            }

            return true;
        }

        /// <inheritdoc/>
        public override bool TrySerialize([NotNullWhen(true)] out byte[]? packetBytes)
        {
            int contentSize = RoomId.HasValue ? ROOM_ID_OFFSET + GUID_SIZE : DEFAULT_CONTENT_SIZE;

            packetBytes = new byte[SphynxPacketHeader.HEADER_SIZE + contentSize];
            var packetSpan = new Span<byte>(packetBytes);

            if (TrySerializeHeader(packetSpan[..SphynxPacketHeader.HEADER_SIZE], contentSize) &&
                TrySerialize(packetSpan = packetSpan[SphynxPacketHeader.HEADER_SIZE..]))
            {
                if (RoomId.HasValue)
                {
                    RoomId.Value.TryWriteBytes(packetSpan.Slice(ROOM_ID_OFFSET, GUID_SIZE));
                }
                return true;
            }

            packetBytes = null;
            return false;
        }

        /// <inheritdoc/>
        public bool Equals(ChatCreateResponsePacket? other) => base.Equals(other) && RoomId == other?.RoomId;
    }
}
