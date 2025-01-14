using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Sphynx.Core;
using Sphynx.Model.ChatRoom;
using Sphynx.Utils;

namespace Sphynx.Network.PacketV2.Response
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

        private int ContentSize
        {
            get
            {
                int contentSize = DEFAULT_CONTENT_SIZE;

                // We must switch to an error state since nothing will be serialized
                if (Messages is null || ErrorCode != SphynxErrorCode.SUCCESS)
                {
                    if (ErrorCode == SphynxErrorCode.SUCCESS) ErrorCode = SphynxErrorCode.INVALID_ROOM;
                    return contentSize;
                }

                contentSize += sizeof(int); // msgCount

                for (int i = 0; i < Messages.Length; i++)
                {
                    Messages[i].GetPacketInfo(out _, out int msgInfoSize);
                    contentSize += msgInfoSize;
                }

                return contentSize;
            }
        }

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
                    if (!ChatRoomMessageInfo.TryDeserialize(contents[cursorOffset..], out var msgInfo, out int bytesRead))
                    {
                        packet = null;
                        return false;
                    }

                    msgs[i] = msgInfo;
                    cursorOffset += bytesRead;
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

            int msgCount = Messages?.Length ?? 0;
            msgCount.WriteBytes(buffer[MSG_COUNT_OFFSET..]);

            for (int i = 0, cursorOffset = MSGS_OFFSET; i < msgCount; i++)
            {
                if (!Messages![i].TrySerialize(buffer, out int bytesWritten))
                {
                    return false;
                }

                cursorOffset += bytesWritten;
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
