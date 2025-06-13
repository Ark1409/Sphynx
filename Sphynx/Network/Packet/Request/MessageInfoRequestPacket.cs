using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Sphynx.Network.Transport;
using Sphynx.Utils;

namespace Sphynx.Network.Packet.Request
{
    /// <inheritdoc cref="SphynxPacketType.MSG_INFO_REQ"/>
    public sealed class MessageInfoRequestPacket : SphynxRequestPacket, IEquatable<MessageInfoRequestPacket>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.MSG_INFO_REQ;

        /// <summary>
        /// The message IDs of the messages for which to retrieve information. Each ID should be distinct.
        /// </summary>
        /// <remarks>Each message ID should be distinct.</remarks>
        public Guid[] MessageIds { get; set; }

        private static readonly int MSG_COUNT_OFFSET = DEFAULT_CONTENT_SIZE;
        private static readonly int MSG_IDS_OFFSET = MSG_COUNT_OFFSET + sizeof(int);

        /// <summary>
        /// Creates a new <see cref="MessageInfoRequestPacket"/>.
        /// </summary>
        /// <param name="userId">The user ID of the requesting user.</param>
        /// <param name="sessionId">The session ID for the requesting user.</param>
        public MessageInfoRequestPacket(Guid userId, Guid sessionId) : base(userId, sessionId)
        {
        }

        /// <summary>
        /// Creates a new <see cref="MessageInfoRequestPacket"/>.
        /// </summary>
        /// <param name="userId">The user ID of the requesting user.</param>
        /// <param name="sessionId">The session ID for the requesting user.</param>
        /// <param name="msgIds">The message IDs of the messages for which to retrieve information.</param>
        public MessageInfoRequestPacket(Guid userId, Guid sessionId, params Guid[] msgIds) : base(userId, sessionId)
        {
            MessageIds = msgIds;
        }

        /// <summary>
        /// Creates a new <see cref="MessageInfoRequestPacket"/>.
        /// </summary>
        /// <param name="userId">The user ID of the requesting user.</param>
        /// <param name="sessionId">The session ID for the requesting user.</param>
        /// <param name="msgIds">The message IDs of the messages for which to retrieve information.</param>
        public MessageInfoRequestPacket(Guid userId, Guid sessionId, IEnumerable<Guid> msgIds) : this(userId, sessionId,
            msgIds as Guid[] ?? msgIds.ToArray())
        {
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="MessageInfoRequestPacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        /// <param name="packet">The deserialized packet.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out MessageInfoRequestPacket? packet)
        {
            if (!TryDeserializeDefaults(contents, out var userId, out var sessionId))
            {
                packet = null;
                return false;
            }

            try
            {
                int msgCount = contents[MSG_COUNT_OFFSET..].ReadInt32();
                var msgIds = new Guid[msgCount];

                for (int i = 0; i < msgCount; i++)
                {
                    msgIds[i] = new Guid(contents.Slice(MSG_IDS_OFFSET + i * GUID_SIZE, GUID_SIZE));
                }

                packet = new MessageInfoRequestPacket(userId.Value, sessionId.Value, msgIds);
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
            int contentSize = DEFAULT_CONTENT_SIZE + sizeof(int) + GUID_SIZE * MessageIds.Length; // msgCount, msgIds
            int bufferSize = SphynxPacketHeader.Size + contentSize;

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

            int contentSize = DEFAULT_CONTENT_SIZE + sizeof(int) + GUID_SIZE * MessageIds.Length; // msgCount, msgIds

            int bufferSize = SphynxPacketHeader.Size + contentSize;
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
            if (!TrySerializeHeader(buffer) || !TrySerializeDefaults(buffer = buffer[SphynxPacketHeader.Size..]))
            {
                return false;
            }

            MessageIds.Length.WriteBytes(buffer[MSG_COUNT_OFFSET..]);
            for (int i = 0; i < MessageIds.Length; i++)
            {
                Debug.Assert(MessageIds[i].TryWriteBytes(buffer.Slice(MSG_IDS_OFFSET + i * GUID_SIZE, GUID_SIZE)));
            }

            return true;
        }

        /// <inheritdoc/>
        public bool Equals(MessageInfoRequestPacket? other) => MemoryUtils.SequenceEqual(MessageIds, other?.MessageIds) && base.Equals(other);
    }
}
