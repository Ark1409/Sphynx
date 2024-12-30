using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Sphynx.Model.ChatRoom;
using Sphynx.Utils;

namespace Sphynx.Network.Packet.Response
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

        private int ContentSize
        {
            get
            {
                int contentSize = DEFAULT_CONTENT_SIZE;

                // We must switch to an error state since nothing will be serialized
                if (RecentMessages is null || ErrorCode != SphynxErrorCode.SUCCESS)
                {
                    if (ErrorCode == SphynxErrorCode.SUCCESS) ErrorCode = SphynxErrorCode.INVALID_ROOM;
                    return contentSize;
                }

                contentSize += sizeof(int); // msgCount

                for (int i = 0; i < RecentMessages.Length; i++)
                {
                    RecentMessages[i].GetPacketInfo(out _, out int msgInfoSize);
                    contentSize += msgInfoSize;
                }

                return contentSize;
            }
        }

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
                    if (!ChatRoomMessageInfo.TryDeserialize(contents[cursorOffset..], out var msgInfo, out int bytesRead))
                    {
                        packet = null;
                        return false;
                    }

                    recentMsgs[i] = msgInfo;
                    cursorOffset += bytesRead;
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
            int bufferSize = SphynxPacketHeader.HEADER_SIZE + ContentSize;

            try
            {
                if (!TrySerialize(packetBytes = new byte[bufferSize]))
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

            return true;
        }

        /// <inheritdoc/>
        public override async Task<bool> TrySerializeAsync(Stream stream)
        {
            if (!stream.CanWrite) return false;

            int bufferSize = SphynxPacketHeader.HEADER_SIZE + ContentSize;
            byte[] rawBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            var buffer = rawBuffer.AsMemory()[..bufferSize];

            try
            {
                if (TrySerialize(buffer.Span))
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

        private bool TrySerialize(Span<byte> buffer)
        {
            if (!TrySerializeHeader(buffer) || !TrySerializeDefaults(buffer = buffer[SphynxPacketHeader.HEADER_SIZE..]))
            {
                return false;
            }

            // We only serialize recent msgs on success
            if (ErrorCode != SphynxErrorCode.SUCCESS) return true;

            int recentMsgsCount = RecentMessages?.Length ?? 0;
            recentMsgsCount.WriteBytes(buffer[RECENT_MSGS_COUNT_OFFSET..]);

            for (int i = 0, cursorOffset = RECENT_MSGS_OFFSET; i < recentMsgsCount; i++)
            {
                if (!RecentMessages![i].TrySerialize(buffer, out int bytesWritten))
                {
                    return false;
                }

                cursorOffset += bytesWritten;
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