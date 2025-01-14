using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Sphynx.Model.ChatRoom;
using Sphynx.Utils;

namespace Sphynx.Network.PacketV2.Request
{
    /// <inheritdoc cref="SphynxPacketType.MSG_REQ"/>.
    public sealed class MessageRequestPacket : SphynxRequestPacket, IEquatable<MessageRequestPacket>
    {
        /// <summary>
        /// The ID of the room to which the message was sent.
        /// </summary>
        public Guid RoomId { get; set; }

        /// <summary>
        /// The contents of the chat message.
        /// </summary>
        public string Message { get; set; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.MSG_REQ;

        private static readonly int ROOM_ID_OFFSET = DEFAULT_CONTENT_SIZE;
        private static readonly int MESSAGE_SIZE_OFFSET = ROOM_ID_OFFSET + GUID_SIZE;
        private static readonly int MESSAGE_OFFSET = MESSAGE_SIZE_OFFSET + sizeof(int);

        /// <summary>
        /// Creates a new <see cref="MessageRequestPacket"/>.
        /// </summary>
        /// <param name="roomId">The ID of the room to which the message was sent.</param>
        /// <param name="message">The contents of the chat message.</param>
        public MessageRequestPacket(Guid roomId, string message) : this(Guid.Empty, Guid.Empty, roomId, message)
        {
        }

        /// <summary>
        /// Creates a new <see cref="MessageRequestPacket"/>.
        /// </summary>
        /// <param name="userId">The user ID of the requesting user.</param>
        /// <param name="sessionId">The session ID for the requesting user.</param>
        /// <param name="roomId">The ID of the room to which the message was sent.</param>
        /// <param name="message">The contents of the chat message.</param>
        public MessageRequestPacket(Guid userId, Guid sessionId, Guid roomId, string message) : base(userId, sessionId)
        {
            RoomId = roomId;
            Message = message ?? throw new ArgumentNullException(nameof(message)); // Exceptions OK on client
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="MessageRequestPacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        /// <param name="packet">The deserialized packet.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out MessageRequestPacket? packet)
        {
            int minContentSize = DEFAULT_CONTENT_SIZE + GUID_SIZE + sizeof(int); // RoomId, MessageSize

            if (contents.Length < minContentSize || !TryDeserializeDefaults(contents[..DEFAULT_CONTENT_SIZE], out var userId, out var sessionId))
            {
                packet = null;
                return false;
            }

            try
            {
                var roomId = new Guid(contents.Slice(ROOM_ID_OFFSET, GUID_SIZE));
                int messageSize = contents[MESSAGE_SIZE_OFFSET..].ReadInt32();
                string message = TEXT_ENCODING.GetString(contents.Slice(MESSAGE_OFFSET, messageSize));

                packet = new MessageRequestPacket(userId.Value, sessionId.Value, roomId, message);
                return true;
            }
            catch
            {
                packet = null;
                return false;
            }
        }

        /// <inheritdoc/>
        public override bool TrySerialize([NotNullWhen(true)] out byte[]? packetBytes)
        {
            int messageSize = TEXT_ENCODING.GetByteCount(Message);
            int contentSize = DEFAULT_CONTENT_SIZE + GUID_SIZE + sizeof(int) + messageSize;
            int bufferSize = SphynxPacketHeader.HEADER_SIZE + contentSize;

            if (!TrySerialize(packetBytes = new byte[bufferSize], messageSize))
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

            int messageSize = TEXT_ENCODING.GetByteCount(Message);
            int contentSize = DEFAULT_CONTENT_SIZE + sizeof(ChatRoomType) + GUID_SIZE + sizeof(int) + messageSize;

            int bufferSize = SphynxPacketHeader.HEADER_SIZE + contentSize;
            byte[] rawBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            var buffer = rawBuffer.AsMemory()[..bufferSize];

            try
            {
                if (TrySerialize(buffer.Span, messageSize))
                {
                    await stream.WriteAsync(buffer);
                    return true;
                }
            }
            catch
            {
                return false;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rawBuffer);
            }

            return false;
        }

        private bool TrySerialize(Span<byte> buffer, int messageSize)
        {
            if (TrySerializeHeader(buffer) && TrySerializeDefaults(buffer = buffer[SphynxPacketHeader.HEADER_SIZE..]))
            {
                RoomId.TryWriteBytes(buffer.Slice(ROOM_ID_OFFSET, GUID_SIZE));
                messageSize.WriteBytes(buffer[MESSAGE_SIZE_OFFSET..]);
                TEXT_ENCODING.GetBytes(Message, buffer.Slice(MESSAGE_OFFSET, messageSize));
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public bool Equals(MessageRequestPacket? other) => base.Equals(other) && RoomId == other?.RoomId && Message == other?.Message;
    }
}
