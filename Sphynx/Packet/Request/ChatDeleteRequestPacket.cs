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

        private const int ROOM_ID_OFFSET = DEFAULT_CONTENT_SIZE;
        private const int PASSWORD_SIZE_OFFSET = ROOM_ID_OFFSET + GUID_SIZE;
        private const int PASSWORD_OFFSET = PASSWORD_SIZE_OFFSET + sizeof(int);

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
            if (contents.Length < PASSWORD_OFFSET || !TryDeserialize(contents[..DEFAULT_CONTENT_SIZE], out var userId, out var sessionId))
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

                packet = new ChatDeleteRequestPacket(userId.Value, sessionId.Value, roomId, passwordSize > 0 ? password : default);
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
            int passwordSize = TEXT_ENCODING.GetByteCount(Password ?? string.Empty);
            int contentSize = DEFAULT_CONTENT_SIZE + GUID_SIZE + sizeof(int) + passwordSize;

            packetBytes = new byte[SphynxPacketHeader.HEADER_SIZE + contentSize];
            var packetSpan = new Span<byte>(packetBytes);

            if (TrySerializeHeader(packetSpan[..SphynxPacketHeader.HEADER_SIZE], contentSize) &&
                TrySerialize(packetSpan = packetSpan[SphynxPacketHeader.HEADER_SIZE..]))
            {
                RoomId.TryWriteBytes(packetSpan.Slice(ROOM_ID_OFFSET, GUID_SIZE));

                // TODO: Serialize hashed password
                passwordSize.WriteBytes(packetSpan, PASSWORD_SIZE_OFFSET);
                TEXT_ENCODING.GetBytes(Password, packetSpan.Slice(PASSWORD_OFFSET, passwordSize));
                return true;
            }

            packetBytes = null;
            return false;
        }

        /// <inheritdoc/>
        public bool Equals(ChatDeleteRequestPacket? other) => base.Equals(other) && RoomId == other?.RoomId && Password == other?.Password;
    }
}
