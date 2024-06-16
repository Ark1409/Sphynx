using System.Buffers;
using System.Diagnostics.CodeAnalysis;

using Sphynx.Utils;

namespace Sphynx.Packet.Request
{
    /// <inheritdoc cref="SphynxPacketType.CHAT_DEL_REQ"/>
    public sealed class ChatDeleteRequestPacket : SphynxRequestPacket, IEquatable<ChatDeleteRequestPacket>
    {
        /// <summary>
        /// The ID of the room to delete.
        /// </summary>
        public Guid RoomId { get; set; }

        /// <summary>
        /// The password for the room to delete, if the room was guarded with a password. 
        /// This is a sort of confirmation to ensure the user understands the action they are about to perform.
        /// </summary>
        public string? Password { get; set; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType { get; }

        private static readonly int ROOM_ID_OFFSET = DEFAULT_CONTENT_SIZE;
        private static readonly int PASSWORD_SIZE_OFFSET = ROOM_ID_OFFSET + GUID_SIZE;
        private static readonly int PASSWORD_OFFSET = PASSWORD_SIZE_OFFSET + sizeof(int);

        /// <summary>
        /// Creates new <see cref="ChatDeleteRequestPacket"/>.
        /// </summary>
        /// <param name="roomId">The ID of the room to delete.</param>
        /// <param name="password">The password for the room to delete, if the room was guarded with a password.</param>
        public ChatDeleteRequestPacket(Guid roomId, string? password) : this(Guid.Empty, Guid.Empty, roomId, password)
        {
        }

        /// <summary>
        /// Creates new <see cref="ChatDeleteRequestPacket"/>.
        /// </summary>
        /// <param name="userId">The user ID of the requesting user.</param>
        /// <param name="sessionId">The session ID for the requesting user.</param>
        /// <param name="roomId">The ID of the room to delete.</param>
        /// <param name="password">The password for the room to delete, if the room was guarded with a password.</param>
        public ChatDeleteRequestPacket(Guid userId, Guid sessionId, Guid roomId, string? password) : base(userId, sessionId)
        {
            RoomId = roomId;
            Password = password;
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="ChatDeleteRequestPacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        /// <param name="packet">The deserialized packet.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out ChatDeleteRequestPacket? packet)
        {
            int minContentSize = DEFAULT_CONTENT_SIZE + GUID_SIZE + sizeof(int);

            if (contents.Length < minContentSize || !TryDeserializeDefaults(contents[..DEFAULT_CONTENT_SIZE], out var userId, out var sessionId))
            {
                packet = null;
                return false;
            }

            try
            {
                var roomId = new Guid(contents.Slice(ROOM_ID_OFFSET, GUID_SIZE));

                // TODO: Read hashed password bytes
                int passwordSize = contents.ReadInt32(PASSWORD_SIZE_OFFSET);
                string password = TEXT_ENCODING.GetString(contents.Slice(PASSWORD_OFFSET, passwordSize));

                packet = new ChatDeleteRequestPacket(userId.Value, sessionId.Value, roomId, passwordSize > 0 ? password : null);
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
            int passwordSize = !string.IsNullOrEmpty(Password) ? TEXT_ENCODING.GetByteCount(Password) : 0;
            int contentSize = DEFAULT_CONTENT_SIZE + GUID_SIZE + sizeof(int) + passwordSize;

            int bufferSize = SphynxPacketHeader.HEADER_SIZE + contentSize;

            if (!TrySerialize(packetBytes = new byte[bufferSize], passwordSize))
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

            int passwordSize = !string.IsNullOrEmpty(Password) ? TEXT_ENCODING.GetByteCount(Password) : 0;
            int contentSize = DEFAULT_CONTENT_SIZE + GUID_SIZE + sizeof(int) + passwordSize;

            int bufferSize = SphynxPacketHeader.HEADER_SIZE + contentSize;
            byte[] rawBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            var buffer = rawBuffer.AsMemory()[..bufferSize];

            try
            {
                if (TrySerialize(buffer.Span, passwordSize))
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

        private bool TrySerialize(Span<byte> buffer, int passwordSize)
        {
            if (TrySerializeHeader(buffer) && TrySerializeDefaults(buffer = buffer[SphynxPacketHeader.HEADER_SIZE..]))
            {
                RoomId.TryWriteBytes(buffer.Slice(ROOM_ID_OFFSET, GUID_SIZE));

                // TODO: Serialize hashed password
                passwordSize.WriteBytes(buffer, PASSWORD_SIZE_OFFSET);
                TEXT_ENCODING.GetBytes(Password, buffer.Slice(PASSWORD_OFFSET, passwordSize));
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public bool Equals(ChatDeleteRequestPacket? other) => base.Equals(other) && RoomId == other?.RoomId && Password == other?.Password;
    }
}
