using Sphynx.ChatRoom;

using System.Diagnostics.CodeAnalysis;

namespace Sphynx.Packet.Request
{
    /// <inheritdoc cref="SphynxPacketType.CHAT_SELECT_REQ"/>
    public sealed class ChatSelectRequestPacket : SphynxRequestPacket, IEquatable<ChatSelectRequestPacket>
    {
        /// <summary>
        /// The type of chat room that was selected.
        /// </summary>
        public ChatRoomType ChatType { get; set; }

        /// <summary>
        /// ID of the chat room that was selected.
        /// </summary>
        public Guid ChatId { get; set; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.CHAT_SELECT_REQ;

        private const int RECIPIENT_TYPE_SIZE = sizeof(ChatRoomType);

        private const int CHAT_TYPE_OFFSET = DEFAULT_CONTENT_SIZE;
        private const int CHAT_ID_OFFSET = CHAT_TYPE_OFFSET + RECIPIENT_TYPE_SIZE;

        /// <summary>
        /// Creates a new <see cref="MessageRequestPacket"/>.
        /// </summary>
        /// <param name="chatType">The type of chat room that was selected.</param>
        /// <param name="chatId">ID of the chat room that was selected.</param>
        public ChatSelectRequestPacket(ChatRoomType chatType, Guid chatId) : this(Guid.Empty, Guid.Empty, chatType, chatId)
        {

        }

        /// <summary>
        /// Creates a new <see cref="ChatSelectRequestPacket"/>.
        /// </summary>
        /// <param name="userId">The user ID of the requesting user.</param>
        /// <param name="sessionId">The session ID for the requesting user.</param>
        /// <param name="chatType">The type of chat room that was selected.</param>
        /// <param name="chatId">ID of the chat room that was selected.</param>
        public ChatSelectRequestPacket(Guid userId, Guid sessionId, ChatRoomType chatType, Guid chatId) : base(userId, sessionId)
        {
            ChatType = chatType;
            ChatId = chatId;
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="ChatSelectRequestPacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        /// <param name="packet">The deserialized packet.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out ChatSelectRequestPacket? packet)
        {
            if (contents.Length < CHAT_ID_OFFSET + GUID_SIZE || !TryDeserialize(contents[..DEFAULT_CONTENT_SIZE], out var userId, out var sessionId))
            {
                packet = null;
                return false;
            }

            try
            {
                var chatType = (ChatRoomType)contents[CHAT_TYPE_OFFSET];
                var chatId = new Guid(contents.Slice(CHAT_ID_OFFSET, GUID_SIZE));

                packet = new ChatSelectRequestPacket(userId.Value, sessionId.Value, chatType, chatId);
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
            int contentSize = DEFAULT_CONTENT_SIZE + RECIPIENT_TYPE_SIZE + GUID_SIZE + sizeof(int) + messageSize;

            packetBytes = new byte[SphynxPacketHeader.HEADER_SIZE + contentSize];
            var packetSpan = new Span<byte>(packetBytes);

            if (TrySerializeHeader(packetSpan[..SphynxPacketHeader.HEADER_SIZE], contentSize) &&
                TrySerialize(packetSpan = packetSpan[SphynxPacketHeader.HEADER_SIZE..]))
            {
                packetSpan[CHAT_TYPE_OFFSET] = (byte)ChatType;
                ChatId.TryWriteBytes(packetSpan.Slice(CHAT_ID_OFFSET, GUID_SIZE));
                return true;
            }

            packetBytes = null;
            return false;
        }

        /// <inheritdoc/>
        public bool Equals(ChatSelectRequestPacket? other) => base.Equals(other) &&
            ChatType == other?.ChatType && ChatId == other?.ChatId;
    }
}
