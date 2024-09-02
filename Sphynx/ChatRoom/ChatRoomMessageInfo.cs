using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Sphynx.Packet;
using Sphynx.Utils;

namespace Sphynx.ChatRoom
{
    /// <summary>
    /// Represents a single message within a chat room.
    /// </summary>
    public class ChatRoomMessageInfo : IEquatable<ChatRoomMessageInfo>
    {
        #region Model

        /// <summary>
        /// The timestamp for this message.
        /// </summary>
        public virtual DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// The timestamp at which this message was edited.
        /// </summary>
        public virtual DateTimeOffset EditTimestamp { get; set; }

        /// <summary>
        /// The chat room to which this message was sent.
        /// </summary>
        public virtual Guid RoomId { get; set; }

        /// <summary>
        /// The user ID of the message sender.
        /// </summary>
        public virtual Guid SenderId { get; set; }

        /// <summary>
        /// An ID for this specific message.
        /// </summary>
        public virtual Guid MessageId { get; set; }

        /// <summary>
        /// The message content.
        /// </summary>
        public virtual string Content { get; set; }

        #endregion

        #region Serialization

        private static readonly unsafe int GUID_SIZE = sizeof(Guid);
        private static readonly int ROOM_ID_OFFSET = 0;
        private static readonly int MESSAGE_ID_OFFSET = ROOM_ID_OFFSET + GUID_SIZE;
        private static readonly int SENDER_ID_OFFSET = ROOM_ID_OFFSET + GUID_SIZE;
        private static readonly int TIMESTAMP_OFFSET = SENDER_ID_OFFSET + GUID_SIZE;
        private static readonly int EDIT_TIMESTAMP_OFFSET = TIMESTAMP_OFFSET + 2 * sizeof(long);
        private static readonly int CONTENT_SIZE_OFFSET = EDIT_TIMESTAMP_OFFSET + 2 * sizeof(long);
        private static readonly int CONTENT_OFFSET = CONTENT_SIZE_OFFSET + sizeof(int);

        private static readonly int MIN_CONTENT_SIZE = GUID_SIZE + GUID_SIZE + GUID_SIZE // MessageId, RoomId, SenderId 
                                                       + 2 * sizeof(long) + 2 * sizeof(long) // Timestamp, EditTimestamp
                                                       + sizeof(int); // Content size;

        #endregion

        /// <summary>
        /// Creates a new <see cref="ChatRoomMessageInfo"/>.
        /// </summary>
        /// <param name="roomId">The room that this message was sent to.</param>
        /// <param name="senderId">The user ID of the message sender.</param>
        /// <param name="content">The message content.</param>
        public ChatRoomMessageInfo(Guid roomId, Guid senderId, string content)
            : this(roomId, Guid.NewGuid(), senderId, DateTimeOffset.UtcNow, content)
        {
        }

        /// <summary>
        /// Creates a new <see cref="ChatRoomMessageInfo"/>.
        /// </summary>
        /// <param name="roomId">The room that this message was sent to.</param>
        /// <param name="senderId">The user ID of the message sender.</param>
        /// <param name="content">The message content.</param>
        /// <param name="timestamp">The timestamp for this message.</param>
        public ChatRoomMessageInfo(Guid roomId, Guid senderId, DateTimeOffset timestamp, string content)
            : this(roomId, Guid.NewGuid(), senderId, timestamp, content)
        {
        }

        /// <summary>
        /// Creates a new <see cref="ChatRoomMessageInfo"/>.
        /// </summary>
        /// <param name="roomId">The room that this message was sent to.</param>
        /// <param name="messageId">An ID for this specific message.</param>
        /// <param name="senderId">The user ID of the message sender.</param>
        /// <param name="content">The message content.</param>
        public ChatRoomMessageInfo(Guid roomId, Guid messageId, Guid senderId, string content)
            : this(roomId, messageId, senderId, DateTimeOffset.UtcNow, content)
        {
        }

        /// <inheritdoc cref="ChatRoomMessageInfo(System.Guid,System.Guid,string)"/>
        /// <param name="messageId">An ID for this specific message.</param>
        /// <param name="timestamp">The timestamp for this message.</param>
        public ChatRoomMessageInfo(Guid roomId, Guid messageId, Guid senderId, DateTimeOffset timestamp, string content)
            : this(roomId, messageId, senderId, timestamp, DateTimeOffset.MinValue, content)
        {
        }

        /// <inheritdoc cref="ChatRoomMessageInfo(System.Guid,System.Guid,System.Guid,System.DateTimeOffset,string)"/>
        /// <param name="editTimestamp">The timestamp at which this message was edited.</param>
        public ChatRoomMessageInfo(Guid roomId, Guid messageId, Guid senderId, DateTimeOffset timestamp, DateTimeOffset editTimestamp, string content)
        {
            RoomId = roomId;
            MessageId = messageId;
            Timestamp = timestamp;
            EditTimestamp = editTimestamp;
            SenderId = senderId;
            Content = content ?? string.Empty;
        }

        #region Serialization

        public static bool TryDeserialize(ReadOnlySpan<byte> msgInfoBytes, [NotNullWhen(true)] out ChatRoomMessageInfo? msgInfo)
        {
            return TryDeserialize(msgInfoBytes, out msgInfo, out _);
        }

        public static bool TryDeserialize(ReadOnlySpan<byte> msgInfoBytes, [NotNullWhen(true)] out ChatRoomMessageInfo? msgInfo, out int bytesRead)
        {
            if (msgInfoBytes.Length < MIN_CONTENT_SIZE)
            {
                bytesRead = 0;
                msgInfo = null;
                return false;
            }

            try
            {
                return (bytesRead = DeserializeContents(msgInfoBytes, out msgInfo)) > 0;
            }
            catch
            {
                bytesRead = 0;
                msgInfo = null;
                return false;
            }
        }

        private static int DeserializeContents(ReadOnlySpan<byte> msgInfoBytes, out ChatRoomMessageInfo? msgInfo)
        {
            var roomId = new Guid(msgInfoBytes.Slice(ROOM_ID_OFFSET, GUID_SIZE));
            var msgId = new Guid(msgInfoBytes.Slice(MESSAGE_ID_OFFSET, GUID_SIZE));
            var senderId = new Guid(msgInfoBytes.Slice(SENDER_ID_OFFSET, GUID_SIZE));

            long timestampTicks = msgInfoBytes[TIMESTAMP_OFFSET..].ReadInt64();
            var timestampOffset = new TimeSpan(msgInfoBytes[(TIMESTAMP_OFFSET + sizeof(long))..].ReadInt64());
            var timestamp = new DateTimeOffset(timestampTicks, timestampOffset);

            long editTimestampTicks = msgInfoBytes[EDIT_TIMESTAMP_OFFSET..].ReadInt64();
            var editTimestampOffset = new TimeSpan(msgInfoBytes[(EDIT_TIMESTAMP_OFFSET + sizeof(long))..].ReadInt64());
            var editTimestamp = new DateTimeOffset(editTimestampTicks, editTimestampOffset);

            int contentSize = msgInfoBytes[CONTENT_SIZE_OFFSET..].ReadInt32();
            string content = SphynxPacket.TEXT_ENCODING.GetString(msgInfoBytes.Slice(CONTENT_OFFSET, contentSize));

            msgInfo = new ChatRoomMessageInfo(roomId, msgId, senderId, timestamp, editTimestamp, content);

            int bytesRead = MIN_CONTENT_SIZE + contentSize;
            return bytesRead;
        }

        /// <inheritdoc/>
        public virtual bool TrySerialize(Span<byte> buffer, out int bytesWritten)
        {
            GetPacketInfo(out int msgContentSize, out int contentSize);

            try
            {
                SerializeContents(buffer, msgContentSize);
                bytesWritten = contentSize;
                return true;
            }
            catch
            {
                bytesWritten = 0;
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> TrySerializeAsync(Stream stream)
        {
            if (!stream.CanWrite) return false;

            GetPacketInfo(out int msgContentSize, out int contentSize);

            byte[] rawBuffer = ArrayPool<byte>.Shared.Rent(contentSize);
            var buffer = rawBuffer.AsMemory()[..contentSize];

            try
            {
                SerializeContents(buffer.Span, msgContentSize);
                await stream.WriteAsync(buffer);
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rawBuffer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void GetPacketInfo(out int msgContentSize, out int contentSize)
        {
            msgContentSize = string.IsNullOrEmpty(Content) ? 0 : SphynxPacket.TEXT_ENCODING.GetByteCount(Content);
            contentSize = CONTENT_OFFSET + msgContentSize;
        }

        private void SerializeContents(Span<byte> buffer, int msgContentSize)
        {
            RoomId.TryWriteBytes(buffer.Slice(ROOM_ID_OFFSET, GUID_SIZE));
            MessageId.TryWriteBytes(buffer.Slice(MESSAGE_ID_OFFSET, GUID_SIZE));
            SenderId.TryWriteBytes(buffer.Slice(SENDER_ID_OFFSET, GUID_SIZE));

            Timestamp.Ticks.WriteBytes(buffer[TIMESTAMP_OFFSET..]);
            Timestamp.Offset.Ticks.WriteBytes(buffer[(TIMESTAMP_OFFSET + sizeof(long))..]);

            EditTimestamp.Ticks.WriteBytes(buffer[EDIT_TIMESTAMP_OFFSET..]);
            EditTimestamp.Offset.Ticks.WriteBytes(buffer[(EDIT_TIMESTAMP_OFFSET + sizeof(long))..]);

            msgContentSize.WriteBytes(buffer[CONTENT_SIZE_OFFSET..]);

            SphynxPacket.TEXT_ENCODING.GetBytes(Content, buffer.Slice(CONTENT_OFFSET, msgContentSize));
        }

        internal static int GetMinimumSize() => MIN_CONTENT_SIZE;

        #endregion

        #region Interfaces

        /// <inheritdoc />
        public bool Equals(ChatRoomMessageInfo? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return MessageId.Equals(other.MessageId);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((ChatRoomMessageInfo)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode() => MessageId.GetHashCode();

        #endregion
    }
}