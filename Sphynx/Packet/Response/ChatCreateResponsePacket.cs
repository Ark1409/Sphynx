using System.Buffers;
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
            if (!TryDeserializeDefaults(contents, out SphynxErrorCode? errorCode) ||
                (errorCode.Value == SphynxErrorCode.SUCCESS && contents.Length < DEFAULT_CONTENT_SIZE + GUID_SIZE))
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
            int contentSize = DEFAULT_CONTENT_SIZE + (RoomId.HasValue ? GUID_SIZE : 0);
            int bufferSize = SphynxPacketHeader.HEADER_SIZE + contentSize;

            if (!TrySerialize(packetBytes = new byte[bufferSize]))
            {
                packetBytes = null;
                return false;
            }

            return true;
        }

        /// <inheritdoc/>
        public override async Task<bool> TrySerializeAsync(Stream stream)
        {
            if (!stream.CanWrite) return false;

            int contentSize = DEFAULT_CONTENT_SIZE + GUID_SIZE;

            int bufferSize = SphynxPacketHeader.HEADER_SIZE + contentSize;
            var rawBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            var buffer = rawBuffer.AsMemory()[..bufferSize];

            try
            {
                if (TrySerialize(buffer.Span))
                {
                    await stream.WriteAsync(buffer);
                    return true;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rawBuffer);
            }

            return false;
        }

        private bool TrySerialize(Span<byte> buffer)
        {
            if (TrySerializeHeader(buffer) && TrySerializeDefaults(buffer = buffer[SphynxPacketHeader.HEADER_SIZE..]))
            {
                RoomId?.TryWriteBytes(buffer.Slice(ROOM_ID_OFFSET, GUID_SIZE));
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public bool Equals(ChatCreateResponsePacket? other) => base.Equals(other) && RoomId == other?.RoomId;
    }
}
