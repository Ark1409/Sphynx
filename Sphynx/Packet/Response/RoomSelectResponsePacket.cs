using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Sphynx.ChatRoom;
using Sphynx.Utils;

namespace Sphynx.Packet.Response
{
    /// <inheritdoc cref="SphynxPacketType.ROOM_SELECT_RES"/>
    public sealed class RoomSelectResponsePacket : SphynxResponsePacket, IEquatable<RoomSelectResponsePacket>
    {
        /// <summary>
        /// Holds the selected chat room's recent information, with the last element representing the most recent message,
        /// or is null if the requesting user does not have access to the room. 
        /// </summary>
        /// <remarks>Message Infos in the array cannot be null.</remarks>
        public ChatRoomMessageInfo[]? RecentMessages { get; set; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.ROOM_SELECT_RES;

        private const int RECENT_MSGS_COUNT_OFFSET = DEFAULT_CONTENT_SIZE;
        private const int RECENT_MSGS_OFFSET = RECENT_MSGS_COUNT_OFFSET + sizeof(int);

        /// <summary>
        /// Creates a new <see cref="RoomSelectResponsePacket"/>.
        /// </summary>
        /// <param name="errorCode">The error code for the response packet.</param>
        public RoomSelectResponsePacket(SphynxErrorCode errorCode) : base(errorCode)
        {
        }

        /// <summary>
        /// Creates a new <see cref="RoomSelectResponsePacket"/>.
        /// </summary>
        /// <param name="recentMessages">Holds the selected chat room's recent information, with the last element representing the most recent message,
        /// or is null if the requesting user does not have access to the room.</param>
        public RoomSelectResponsePacket(params ChatRoomMessageInfo[] recentMessages) : base(SphynxErrorCode.SUCCESS)
        {
            RecentMessages = recentMessages;
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="RoomSelectResponsePacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        /// <param name="packet">The deserialized packet.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out RoomSelectResponsePacket? packet)
        {
            int minContentSize = DEFAULT_CONTENT_SIZE + sizeof(int); // recentMsgsCount

            if (!TryDeserializeDefaults(contents, out SphynxErrorCode? errorCode) ||
                (errorCode.Value == SphynxErrorCode.SUCCESS && contents.Length < minContentSize))
            {
                packet = null;
                return false;
            }

            // We only provide recent msg info on success
            if (errorCode != SphynxErrorCode.SUCCESS)
            {
                packet = new RoomSelectResponsePacket(errorCode.Value);
                return true;
            }

            try
            {
                int recentMsgsCount = contents[RECENT_MSGS_COUNT_OFFSET..].ReadInt32();
                var recentMsgs = new ChatRoomMessageInfo[recentMsgsCount];

                for (int i = 0, cursorOffset = RECENT_MSGS_OFFSET; i < recentMsgsCount; i++)
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

                    // Room ID was provided in request; sending it in response (for deserialization) is not necessary
                    recentMsgs[i] = new ChatRoomMessageInfo(roomId, msgId, senderId, timestamp, editTimestamp, content);
                }

                packet = new RoomSelectResponsePacket(recentMsgs);
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
            int[] msgContentSizes = ArrayPool<int>.Shared.Rent(RecentMessages?.Length ?? 0);
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

            int[] msgContentSizes = ArrayPool<int>.Shared.Rent(RecentMessages?.Length ?? 0);
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
            if (RecentMessages is null || ErrorCode != SphynxErrorCode.SUCCESS)
            {
                if (ErrorCode == SphynxErrorCode.SUCCESS)
                    ErrorCode = SphynxErrorCode.INVALID_ROOM;
                return;
            }

            int partialMsgLength = GUID_SIZE + GUID_SIZE + 2 * sizeof(long) + 2 * sizeof(long) + GUID_SIZE +
                                   sizeof(int); // msgId, senderId, timestamp, editTimestamp, roomId, contentSize

            contentSize += sizeof(int) + RecentMessages.Length * partialMsgLength; // msgCount

            for (int i = 0; i < RecentMessages.Length; i++)
            {
                string content = RecentMessages[i].Content;
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

            (RecentMessages?.Length ?? 0).WriteBytes(buffer[RECENT_MSGS_COUNT_OFFSET..]);

            for (int i = 0, cursorOffset = RECENT_MSGS_OFFSET; i < (RecentMessages?.Length ?? 0); i++)
            {
                var recentMsg = RecentMessages![i];

                recentMsg.MessageId.TryWriteBytes(buffer.Slice(cursorOffset, GUID_SIZE));
                cursorOffset += GUID_SIZE;
                recentMsg.SenderId.TryWriteBytes(buffer.Slice(cursorOffset, GUID_SIZE));
                cursorOffset += GUID_SIZE;

                recentMsg.Timestamp.Ticks.WriteBytes(buffer[cursorOffset..]);
                cursorOffset += sizeof(long);
                recentMsg.Timestamp.Offset.Ticks.WriteBytes(buffer[cursorOffset..]);
                cursorOffset += sizeof(long);

                recentMsg.EditTimestamp.Ticks.WriteBytes(buffer[cursorOffset..]);
                cursorOffset += sizeof(long);
                recentMsg.EditTimestamp.Offset.Ticks.WriteBytes(buffer[cursorOffset..]);
                cursorOffset += sizeof(long);

                recentMsg.RoomId.TryWriteBytes(buffer.Slice(cursorOffset, GUID_SIZE));
                cursorOffset += GUID_SIZE;

                msgContentSizes[i].WriteBytes(buffer[cursorOffset..]);
                cursorOffset += sizeof(int);

                TEXT_ENCODING.GetBytes(recentMsg.Content, buffer.Slice(cursorOffset, msgContentSizes[i]));
                cursorOffset += msgContentSizes[i];
            }

            return true;
        }

        /// <inheritdoc/>
        public bool Equals(RoomSelectResponsePacket? other)
        {
            if (other is null || !base.Equals(other)) return false;
            if (RecentMessages is null && other.RecentMessages is null) return true;
            if (RecentMessages is null || other.RecentMessages is null) return false;

            return MemoryUtils.SequenceEqual(RecentMessages, other.RecentMessages);
        }
    }
}