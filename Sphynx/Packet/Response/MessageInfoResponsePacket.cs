using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Sphynx.ChatRoom;
using Sphynx.Core;
using Sphynx.Utils;

namespace Sphynx.Packet.Response
{
    /// <inheritdoc cref="SphynxPacketType.MSG_INFO_RES"/>
    public sealed class MessageInfoResponsePacket : SphynxResponsePacket, IEquatable<MessageInfoResponsePacket>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.LOGOUT_RES;

        /// <summary>
        /// The resolved messages' information.
        /// </summary>
        public ChatRoomMessageInfo[]? Messages { get; set; }

        private const int MSG_COUNT_OFFSET = DEFAULT_CONTENT_SIZE;
        private const int MSGS_OFFSET = MSG_COUNT_OFFSET + sizeof(int);

        /// <summary>
        /// Creates a new <see cref="MessageInfoResponsePacket"/>.
        /// </summary>
        /// <param name="errorCode">Error code for logout attempt.</param>
        public MessageInfoResponsePacket(SphynxErrorCode errorCode) : base(errorCode)
        {
        }

        /// <summary>
        /// Creates a new <see cref="MessageInfoResponsePacket"/> with <see cref="SphynxErrorCode.SUCCESS"/>.
        /// </summary>
        /// <param name="messages">The resolved messages' information.</param>
        public MessageInfoResponsePacket(params ChatRoomMessageInfo[]? messages) : this(SphynxErrorCode.SUCCESS)
        {
            Messages = messages;
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="LogoutResponsePacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        /// <param name="packet">The deserialized packet.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out MessageInfoResponsePacket? packet)
        {
            int minContentSize = DEFAULT_CONTENT_SIZE + sizeof(int);

            if (!TryDeserializeDefaults(contents, out SphynxErrorCode? errorCode) ||
                (errorCode.Value == SphynxErrorCode.SUCCESS && contents.Length < minContentSize))
            {
                packet = null;
                return false;
            }

            // We only provide user info on success
            if (errorCode != SphynxErrorCode.SUCCESS)
            {
                packet = new MessageInfoResponsePacket(errorCode.Value);
                return true;
            }

            try
            {
                int msgCount = contents[MSG_COUNT_OFFSET..].ReadInt32();
                var msgs = new ChatRoomMessageInfo[msgCount];

                for (int i = 0, cursorOffset = MSGS_OFFSET; i < msgCount; i++)
                {
                    var msgId = new Guid(contents.Slice(cursorOffset, GUID_SIZE));
                    cursorOffset += GUID_SIZE;

                    var senderId = new Guid(contents.Slice(cursorOffset, GUID_SIZE));
                    cursorOffset += GUID_SIZE;

                    long timestampTicks = contents[cursorOffset..].ReadInt64();
                    cursorOffset += sizeof(long);
                    var timestampOffset = new TimeSpan(contents[cursorOffset..].ReadInt64());
                    cursorOffset += sizeof(long);
                    var timestamp = new DateTimeOffset(timestampTicks, timestampOffset);

                    long editTimestampTicks = contents[cursorOffset..].ReadInt64();
                    cursorOffset += sizeof(long);
                    var editTimestampOffset = new TimeSpan(contents[cursorOffset..].ReadInt64());
                    cursorOffset += sizeof(long);
                    var editTimestamp = new DateTimeOffset(editTimestampTicks, editTimestampOffset);

                    var roomId = new Guid(contents.Slice(cursorOffset, GUID_SIZE));
                    cursorOffset += GUID_SIZE;

                    int contentSize = contents[cursorOffset..].ReadInt32();
                    cursorOffset += sizeof(int);

                    string content = TEXT_ENCODING.GetString(contents.Slice(cursorOffset, contentSize));
                    cursorOffset += contentSize;

                    // Room ID was provided in request; sending it during deserialization is not necessary
                    msgs[i] = new ChatRoomMessageInfo(roomId, msgId, senderId, timestamp, editTimestamp, content);
                }

                packet = new MessageInfoResponsePacket(msgs);
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
            int[] msgContentSizes = ArrayPool<int>.Shared.Rent(Messages?.Length ?? 0);
            GetPacketInfo(msgContentSizes, out int contentSize);
            int bufferSize = SphynxPacketHeader.HEADER_SIZE + contentSize;

            try
            {
                if (!TrySerialize(packetBytes = new byte[bufferSize], msgContentSizes))
                {
                    packetBytes = null;
                    return false;
                }
            }
            catch
            {
                packetBytes = null;
                return false;
            }
            finally
            {
                ArrayPool<int>.Shared.Return(msgContentSizes);
            }

            return true;
        }

        /// <inheritdoc/>
        public override async Task<bool> TrySerializeAsync(Stream stream)
        {
            if (!stream.CanWrite) return false;

            int[] msgContentSizes = ArrayPool<int>.Shared.Rent(Messages?.Length ?? 0);
            GetPacketInfo(msgContentSizes, out int contentSize);

            int bufferSize = SphynxPacketHeader.HEADER_SIZE + contentSize;
            byte[] rawBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            var buffer = rawBuffer.AsMemory()[..bufferSize];

            try
            {
                if (TrySerialize(buffer.Span, msgContentSizes))
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
                ArrayPool<int>.Shared.Return(msgContentSizes);
                ArrayPool<byte>.Shared.Return(rawBuffer);
            }

            return false;
        }

        private void GetPacketInfo(int[] msgContentSizes, out int contentSize)
        {
            contentSize = DEFAULT_CONTENT_SIZE;

            // We must switch to an error state since nothing will be serialized
            if (Messages is null || ErrorCode != SphynxErrorCode.SUCCESS)
            {
                if (ErrorCode == SphynxErrorCode.SUCCESS)
                    ErrorCode = SphynxErrorCode.INVALID_ROOM;
                return;
            }

            int partialMsgLength = GUID_SIZE + GUID_SIZE + 2 * sizeof(long) + 2 * sizeof(long) + GUID_SIZE +
                                   sizeof(int); // msgId, senderId, timestamp, editTimestamp, roomId, contentSize

            contentSize += sizeof(int) + Messages.Length * partialMsgLength; // msgCount

            for (int i = 0; i < Messages.Length; i++)
            {
                string content = Messages[i].Content;
                contentSize += (msgContentSizes[i] = string.IsNullOrEmpty(content) ? 0 : TEXT_ENCODING.GetByteCount(content));
            }
        }

        private bool TrySerialize(Span<byte> buffer, int[] msgContentSizes)
        {
            if (!TrySerializeHeader(buffer) || !TrySerializeDefaults(buffer = buffer[SphynxPacketHeader.HEADER_SIZE..]))
            {
                return false;
            }

            // We only serialize recent msgs on success
            if (ErrorCode != SphynxErrorCode.SUCCESS) return true;

            (Messages?.Length ?? 0).WriteBytes(buffer[MSG_COUNT_OFFSET..]);

            for (int i = 0, cursorOffset = MSGS_OFFSET; i < (Messages?.Length ?? 0); i++)
            {
                var msg = Messages![i];

                msg.MessageId.TryWriteBytes(buffer.Slice(cursorOffset, GUID_SIZE));
                cursorOffset += GUID_SIZE;
                msg.SenderId.TryWriteBytes(buffer.Slice(cursorOffset, GUID_SIZE));
                cursorOffset += GUID_SIZE;

                msg.Timestamp.Ticks.WriteBytes(buffer[cursorOffset..]);
                cursorOffset += sizeof(long);
                msg.Timestamp.Offset.Ticks.WriteBytes(buffer[cursorOffset..]);
                cursorOffset += sizeof(long);

                msg.EditTimestamp.Ticks.WriteBytes(buffer[cursorOffset..]);
                cursorOffset += sizeof(long);
                msg.EditTimestamp.Offset.Ticks.WriteBytes(buffer[cursorOffset..]);
                cursorOffset += sizeof(long);

                msg.RoomId.TryWriteBytes(buffer.Slice(cursorOffset, GUID_SIZE));
                cursorOffset += GUID_SIZE;

                msgContentSizes[i].WriteBytes(buffer[cursorOffset..]);
                cursorOffset += sizeof(int);

                TEXT_ENCODING.GetBytes(msg.Content, buffer.Slice(cursorOffset, msgContentSizes[i]));
                cursorOffset += msgContentSizes[i];
            }

            return true;
        }

        /// <inheritdoc/>
        public bool Equals(MessageInfoResponsePacket? other)
        {
            if (other is null || !base.Equals(other)) return false;
            if (Messages is null && other.Messages is null) return true;
            if (Messages is null || other.Messages is null) return false;

            return MemoryUtils.SequenceEqual(Messages, other.Messages);
        }
    }
}