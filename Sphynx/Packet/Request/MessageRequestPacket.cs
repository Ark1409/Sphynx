﻿using System.Diagnostics.CodeAnalysis;

using Sphynx.ChatRoom;
using Sphynx.Utils;

namespace Sphynx.Packet.Request
{
    /// <inheritdoc cref="SphynxPacketType.MSG_REQ"/>.
    public sealed class MessageRequestPacket : SphynxRequestPacket, IEquatable<MessageRequestPacket>
    {
        /// <summary>
        /// The type of recipient for the message.
        /// </summary>
        public ChatRoomType RecipientType { get; set; }

        /// <summary>
        /// ID of the message recipient.
        /// </summary>
        public Guid RecipientId { get; set; }

        /// <summary>
        /// The contents of the chat message.
        /// </summary>
        public string Message { get; set; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.MSG_REQ;

        private const int RECIPIENT_TYPE_SIZE = sizeof(ChatRoomType);

        private const int RECIPIENT_TYPE_OFFSET = DEFAULT_CONTENT_SIZE;
        private const int RECIPIENT_ID_OFFSET = RECIPIENT_TYPE_OFFSET + RECIPIENT_TYPE_SIZE;
        private const int MESSAGE_LENGTH_OFFSET = RECIPIENT_ID_OFFSET + GUID_SIZE;
        private const int MESSAGE_OFFSET = MESSAGE_LENGTH_OFFSET + sizeof(int);

        /// <summary>
        /// Creates a new <see cref="MessageRequestPacket"/>.
        /// </summary>
        /// <param name="recipientType">The type of recipient for the message.</param>
        /// <param name="recipientId">ID of recipient (whether it be a room or user).</param>
        /// <param name="message">The contents of the chat message.</param>
        public MessageRequestPacket(ChatRoomType recipientType, Guid recipientId, string message) : this(Guid.Empty, Guid.Empty, recipientType, recipientId, message)
        {

        }

        /// <summary>
        /// Creates a new <see cref="MessageRequestPacket"/>.
        /// </summary>
        /// <param name="userId">The user ID of the requesting user.</param>
        /// <param name="sessionId">The session ID for the requesting user.</param>
        /// <param name="recipientType">The type of recipient for the message.</param>
        /// <param name="recipientId">ID of recipient (whether it be a room or user).</param>
        /// <param name="message">The contents of the chat message.</param>
        public MessageRequestPacket(Guid userId, Guid sessionId, ChatRoomType recipientType, Guid recipientId, string message) : base(userId, sessionId)
        {
            RecipientType = recipientType;
            RecipientId = recipientId;
            Message = message ?? throw new ArgumentNullException(nameof(message)); // Exceptions OK on client
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="MessageRequestPacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        /// <param name="packet">The deserialized packet.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out MessageRequestPacket? packet)
        {
            if (!TryDeserialize(contents[..DEFAULT_CONTENT_SIZE], out var userId, out var sessionId))
            {
                packet = null;
                return false;
            }

            try
            {
                var recipientType = (ChatRoomType)contents[RECIPIENT_TYPE_OFFSET];
                var recipientId = new Guid(contents.Slice(RECIPIENT_ID_OFFSET, GUID_SIZE));

                int messageLength = contents.Slice(MESSAGE_LENGTH_OFFSET, sizeof(int)).ReadInt32();
                string message = TEXT_ENCODING.GetString(contents.Slice(MESSAGE_OFFSET, messageLength));

                packet = new MessageRequestPacket(userId.Value, sessionId.Value, recipientType, recipientId, message);
                return true;
            }
            catch
            {
                packet = null;
                return false;
            }
        }

        /// <inheritdoc/>
        public override bool TrySerialize(out byte[]? packetBytes)
        {
            int messageSize = TEXT_ENCODING.GetByteCount(Message);
            int contentSize = DEFAULT_CONTENT_SIZE + RECIPIENT_TYPE_SIZE + GUID_SIZE + sizeof(int) + messageSize;

            packetBytes = new byte[SphynxPacketHeader.HEADER_SIZE + contentSize];
            var packetSpan = new Span<byte>(packetBytes);

            if (TrySerializeHeader(packetSpan[..SphynxPacketHeader.HEADER_SIZE], contentSize) &&
                TrySerialize(packetSpan = packetSpan[SphynxPacketHeader.HEADER_SIZE..]))
            {
                packetSpan[RECIPIENT_TYPE_OFFSET] = (byte)RecipientType;

                RecipientId.TryWriteBytes(packetSpan.Slice(RECIPIENT_ID_OFFSET, GUID_SIZE));

                messageSize.WriteBytes(packetSpan, MESSAGE_LENGTH_OFFSET);
                TEXT_ENCODING.GetBytes(Message, packetSpan.Slice(MESSAGE_OFFSET, messageSize));
                return true;
            }

            packetBytes = null;
            return false;
        }

        /// <inheritdoc/>
        public bool Equals(MessageRequestPacket? other) => base.Equals(other) &&
            RecipientType == other?.RecipientType && RecipientId == other?.RecipientId && Message == other?.Message;
    }
}
