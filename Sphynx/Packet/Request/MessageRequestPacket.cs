﻿using System.Buffers;
using System.Diagnostics.CodeAnalysis;

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

        private static readonly int RECIPIENT_TYPE_OFFSET = DEFAULT_CONTENT_SIZE;
        private static readonly int RECIPIENT_ID_OFFSET = RECIPIENT_TYPE_OFFSET + sizeof(ChatRoomType);
        private static readonly int MESSAGE_SIZE_OFFSET = RECIPIENT_ID_OFFSET + GUID_SIZE;
        private static readonly int MESSAGE_OFFSET = MESSAGE_SIZE_OFFSET + sizeof(int);

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
            int minContentSize = DEFAULT_CONTENT_SIZE + sizeof(ChatRoomType) + GUID_SIZE + sizeof(int);

            if (contents.Length < minContentSize || !TryDeserializeDefaults(contents[..DEFAULT_CONTENT_SIZE], out var userId, out var sessionId))
            {
                packet = null;
                return false;
            }

            try
            {
                var recipientType = (ChatRoomType)contents[RECIPIENT_TYPE_OFFSET];
                var recipientId = new Guid(contents.Slice(RECIPIENT_ID_OFFSET, GUID_SIZE));

                int messageSize = contents.ReadInt32(MESSAGE_SIZE_OFFSET);
                string message = TEXT_ENCODING.GetString(contents.Slice(MESSAGE_OFFSET, messageSize));

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
        public override bool TrySerialize([NotNullWhen(true)] out byte[]? packetBytes)
        {
            int messageSize = TEXT_ENCODING.GetByteCount(Message);
            int contentSize = DEFAULT_CONTENT_SIZE + sizeof(ChatRoomType) + GUID_SIZE + sizeof(int) + messageSize;
            int bufferSize = SphynxPacketHeader.HEADER_SIZE + contentSize;

            if (!TrySerialize(packetBytes = new byte[bufferSize], messageSize))
            {
                packetBytes = null;
                return false;
            }

            return true;
        }

        /// <inheritdoc/>
        public override bool TrySerialize(Stream stream)
        {
            if (!stream.CanWrite) return false;

            int messageSize = TEXT_ENCODING.GetByteCount(Message);
            int contentSize = DEFAULT_CONTENT_SIZE + sizeof(ChatRoomType) + GUID_SIZE + sizeof(int) + messageSize;

            int bufferSize = SphynxPacketHeader.HEADER_SIZE + contentSize;
            var rawBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            var buffer = rawBuffer.AsSpan()[..bufferSize];

            try
            {
                if (TrySerialize(buffer, messageSize))
                {
                    stream.Write(buffer);
                    return true;
                }
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
                buffer[RECIPIENT_TYPE_OFFSET] = (byte)RecipientType;

                RecipientId.TryWriteBytes(buffer.Slice(RECIPIENT_ID_OFFSET, GUID_SIZE));

                messageSize.WriteBytes(buffer, MESSAGE_SIZE_OFFSET);
                TEXT_ENCODING.GetBytes(Message, buffer.Slice(MESSAGE_OFFSET, messageSize));
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public bool Equals(MessageRequestPacket? other) => base.Equals(other) &&
            RecipientType == other?.RecipientType && RecipientId == other?.RecipientId && Message == other?.Message;
    }
}
