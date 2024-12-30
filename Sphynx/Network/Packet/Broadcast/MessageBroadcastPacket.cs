using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Sphynx.Network.Packet.Request;

namespace Sphynx.Network.Packet.Broadcast
{
    /// <inheritdoc cref="SphynxPacketType.MSG_BCAST"/>
    public class MessageBroadcastPacket : SphynxPacket, IEquatable<MessageBroadcastPacket>
    {
        /// <summary>
        /// The ID of the room to which the message was sent.
        /// </summary>
        public Guid RoomId { get; set; }

        /// <summary>
        /// The message ID of the message that was sent.
        /// </summary>
        public Guid MessageID { get; set; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.MSG_BCAST;

        protected const int ROOM_ID_OFFSET = 0;
        protected static readonly int MESSAGE_ID_OFFSET = GUID_SIZE;

        /// <summary>
        /// Creates a new <see cref="MessageRequestPacket"/>, assuming the message is for a user.
        /// </summary>
        /// <param name="roomId">The ID of the room to which the message was sent.</param>
        /// <param name="messageId">The message ID of the message that was sent.</param>
        public MessageBroadcastPacket(Guid roomId, Guid messageId)
        {
            RoomId = roomId;
            MessageID = messageId;
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="MessageBroadcastPacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        /// <param name="packet">The deserialized packet.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out MessageBroadcastPacket? packet)
        {
            int contentSize = GUID_SIZE + GUID_SIZE; // RoomId, MessageId

            if (contents.Length < contentSize)
            {
                packet = null;
                return false;
            }

            var roomId = new Guid(contents.Slice(ROOM_ID_OFFSET, GUID_SIZE));
            var messageId = new Guid(contents.Slice(MESSAGE_ID_OFFSET, GUID_SIZE));

            packet = new MessageBroadcastPacket(roomId, messageId);
            return true;
        }

        /// <inheritdoc/>
        public override bool TrySerialize([NotNullWhen(true)] out byte[]? packetBytes)
        {
            int contentSize = GUID_SIZE + GUID_SIZE; // RoomId, MessageId
            int bufferSize = SphynxPacketHeader.HEADER_SIZE + contentSize;

            if (!TrySerialize(packetBytes = new byte[bufferSize]))
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

            int contentSize = GUID_SIZE + GUID_SIZE; // RoomId, MessageId

            int bufferSize = SphynxPacketHeader.HEADER_SIZE + contentSize;
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
            finally
            {
                ArrayPool<byte>.Shared.Return(rawBuffer);
            }

            return false;
        }

        private bool TrySerialize(Span<byte> buffer)
        {
            if (TrySerializeHeader(buffer))
            {
                buffer = buffer[SphynxPacketHeader.HEADER_SIZE..];
                RoomId.TryWriteBytes(buffer.Slice(ROOM_ID_OFFSET, GUID_SIZE));
                MessageID.TryWriteBytes(buffer.Slice(MESSAGE_ID_OFFSET, GUID_SIZE));
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public bool Equals(MessageBroadcastPacket? other) => base.Equals(other) && RoomId == other?.RoomId && MessageID == other?.MessageID;
    }
}