using System.Buffers;
using System.Diagnostics.CodeAnalysis;

using Sphynx.ChatRoom;
using Sphynx.Packet.Request;
using Sphynx.Utils;

namespace Sphynx.Packet.Broadcast
{
    /// <inheritdoc cref="SphynxPacketType.MSG_BCAST"/>
    public abstract class MessageBroadcastPacket : SphynxPacket, IEquatable<MessageBroadcastPacket>
    {
        /// <summary>
        /// User ID of sender.
        /// </summary>
        public Guid SenderId { get; set; }

        /// <summary>
        /// The contents of the chat message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// <inheritdoc cref="ChatRoomType"/>
        /// </summary>
        public abstract ChatRoomType MessageType { get; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.MSG_BCAST;

        protected const int MESSAGE_TYPE_OFFSET = 0;
        protected const int DEFAULT_CONTENT_SIZE = sizeof(ChatRoomType);

        /// <summary>
        /// Creates a new <see cref="MessageRequestPacket"/>, assuming the message is for a user.
        /// </summary>
        /// <param name="senderId">User ID of the sender.</param>
        /// <param name="message">The contents of the chat message.</param>
        public MessageBroadcastPacket(Guid senderId, string message)
        {
            SenderId = senderId;
            Message = message;
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="MessageBroadcastPacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        /// <param name="packet">The deserialized packet.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out MessageBroadcastPacket? packet)
        {
            if (contents.Length > DEFAULT_CONTENT_SIZE && TryDeserializeDefaults(contents[..DEFAULT_CONTENT_SIZE], out var roomType))
            {
                switch (roomType)
                {
                    case ChatRoomType.DIRECT_MSG:
                        if (Direct.TryDeserialize(contents, out var dPacket))
                        {
                            packet = dPacket;
                            return true;
                        }
                        break;

                    case ChatRoomType.GROUP:
                        if (Group.TryDeserialize(contents, out var gPacket))
                        {
                            packet = gPacket;
                            return true;
                        }
                        break;
                }
            }

            packet = null;
            return false;
        }

        protected bool TrySerializeDefaults(Span<byte> buffer)
        {
            if (buffer.Length < DEFAULT_CONTENT_SIZE)
            {
                return false;
            }

            buffer[MESSAGE_TYPE_OFFSET] = (byte)MessageType;
            return true;
        }

        protected static bool TryDeserializeDefaults(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out ChatRoomType? roomType)
        {
            if (contents.Length < DEFAULT_CONTENT_SIZE)
            {
                roomType = null;
                return false;
            }

            roomType = (ChatRoomType)contents[MESSAGE_TYPE_OFFSET];
            return true;
        }

        /// <inheritdoc/>
        public bool Equals(MessageBroadcastPacket? other) => base.Equals(other) &&
            SenderId == other?.SenderId && Message == other?.Message;

        /// <summary>
        /// <see cref="ChatRoomType.DIRECT_MSG"/> message.
        /// </summary>
        public sealed class Direct : MessageBroadcastPacket, IEquatable<Direct>
        {
            private const int SENDER_ID_OFFSET = MESSAGE_TYPE_OFFSET + sizeof(ChatRoomType);
            private static readonly int MESSAGE_SIZE_OFFSET = SENDER_ID_OFFSET + GUID_SIZE;
            private static readonly int MESSAGE_OFFSET = MESSAGE_SIZE_OFFSET + sizeof(int);

            /// <inheritdoc/>
            public override ChatRoomType MessageType => ChatRoomType.DIRECT_MSG;

            /// <summary>
            /// Creates a new <see cref="MessageRequestPacket"/>, assuming the message is for a user.
            /// </summary>
            /// <param name="senderId">User ID of the sender.</param>
            /// <param name="message">The contents of the chat message.</param>
            public Direct(Guid senderId, string message) : base(senderId, message)
            {

            }
            
            /// <summary>
            /// Attempts to deserialize a <see cref="MessageBroadcastPacket.Direct"/>.
            /// </summary>
            /// <param name="contents">Packet contents, excluding the header.</param>
            /// <param name="packet">The deserialized packet.</param>
            public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out Direct? packet)
            {
                int minContentSize = DEFAULT_CONTENT_SIZE + GUID_SIZE + sizeof(int);

                if (contents.Length < minContentSize || !TryDeserializeDefaults(contents, out var roomType) || roomType != ChatRoomType.DIRECT_MSG)
                {
                    packet = null;
                    return false;
                }

                try
                {
                    var senderId = new Guid(contents.Slice(SENDER_ID_OFFSET, GUID_SIZE));

                    int messageSize = contents.ReadInt32(MESSAGE_SIZE_OFFSET);
                    string message = TEXT_ENCODING.GetString(contents.Slice(MESSAGE_OFFSET, messageSize));

                    packet = new Direct(senderId, message);
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
                int contentSize = DEFAULT_CONTENT_SIZE + GUID_SIZE + sizeof(int) + messageSize;

                int bufferSize = SphynxPacketHeader.HEADER_SIZE + contentSize;
                var rawBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
                var buffer = rawBuffer.AsMemory()[..bufferSize];

                try
                {
                    if (TrySerialize(buffer.Span, messageSize))
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

            private bool TrySerialize(Span<byte> buffer, int messageSize)
            {
                if (TrySerializeHeader(buffer) && TrySerializeDefaults(buffer = buffer[SphynxPacketHeader.HEADER_SIZE..]))
                {
                    SenderId.TryWriteBytes(buffer.Slice(SENDER_ID_OFFSET, GUID_SIZE));

                    messageSize.WriteBytes(buffer, MESSAGE_SIZE_OFFSET);
                    TEXT_ENCODING.GetBytes(Message, buffer.Slice(MESSAGE_OFFSET, messageSize));
                    return true;
                }

                return false;
            }

            /// <inheritdoc/>
            public bool Equals(Direct? other) => base.Equals(other);
        }

        /// <summary>
        /// <see cref="ChatRoomType.GROUP"/> message.
        /// </summary>
        public sealed class Group : MessageBroadcastPacket, IEquatable<Group>
        {
            /// <summary>
            /// Room ID to send the message to.
            /// </summary>
            public Guid RoomId { get; set; }

            private const int ROOM_ID_OFFSET = DEFAULT_CONTENT_SIZE;
            private static readonly int SENDER_ID_OFFSET = ROOM_ID_OFFSET + GUID_SIZE;
            private static readonly int MESSAGE_SIZE_OFFSET = SENDER_ID_OFFSET + GUID_SIZE;
            private static readonly int MESSAGE_OFFSET = MESSAGE_SIZE_OFFSET + sizeof(int);

            /// <inheritdoc/>
            public override ChatRoomType MessageType => ChatRoomType.GROUP;

            /// <summary>
            /// Creates a new <see cref="MessageRequestPacket"/>, assuming the message is for a user.
            /// </summary>
            /// <param name="roomId">Room ID to send the message to.</param>
            /// <param name="senderId">User ID of the sender.</param>
            /// <param name="message">The contents of the chat message.</param>
            public Group(Guid roomId, Guid senderId, string message) : base(senderId, message)
            {
                RoomId = roomId;
                SenderId = senderId;
                Message = message;
            }

            /// <summary>
            /// Attempts to deserialize a <see cref="MessageBroadcastPacket.Group"/>.
            /// </summary>
            /// <param name="contents">Packet contents, excluding the header.</param>
            /// <param name="packet">The deserialized packet.</param>
            public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out Group? packet)
            {
                if (contents.Length < MESSAGE_OFFSET || !TryDeserializeDefaults(contents, out var roomType) || roomType != ChatRoomType.GROUP)
                {
                    packet = null;
                    return false;
                }

                try
                {
                    var roomId = new Guid(contents.Slice(ROOM_ID_OFFSET, GUID_SIZE));
                    var senderId = new Guid(contents.Slice(SENDER_ID_OFFSET, GUID_SIZE));

                    int messageSize = contents.ReadInt32(MESSAGE_SIZE_OFFSET);
                    string message = TEXT_ENCODING.GetString(contents.Slice(MESSAGE_OFFSET, messageSize));

                    packet = new Group(roomId, senderId, message);
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
                int contentSize = DEFAULT_CONTENT_SIZE + GUID_SIZE + GUID_SIZE + sizeof(int) + messageSize;
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
                int contentSize = DEFAULT_CONTENT_SIZE + GUID_SIZE + GUID_SIZE + sizeof(int) + messageSize;

                int bufferSize = SphynxPacketHeader.HEADER_SIZE + contentSize;
                var rawBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
                var buffer = rawBuffer.AsMemory()[..bufferSize];

                try
                {
                    if (TrySerialize(buffer.Span, messageSize))
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

            private bool TrySerialize(Span<byte> buffer, int messageSize)
            {
                if (TrySerializeHeader(buffer) && TrySerializeDefaults(buffer = buffer[SphynxPacketHeader.HEADER_SIZE..]))
                {
                    RoomId.TryWriteBytes(buffer.Slice(ROOM_ID_OFFSET, GUID_SIZE));
                    SenderId.TryWriteBytes(buffer.Slice(SENDER_ID_OFFSET, GUID_SIZE));

                    messageSize.WriteBytes(buffer, MESSAGE_SIZE_OFFSET);
                    TEXT_ENCODING.GetBytes(Message, buffer.Slice(MESSAGE_OFFSET, messageSize));
                    return true;
                }

                return false;
            }

            /// <inheritdoc/>
            public bool Equals(Group? other) => base.Equals(other) && RoomId == other?.RoomId;
        }
    }
}