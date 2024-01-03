using System.Diagnostics.CodeAnalysis;

using Sphynx.Packet.Response;
using Sphynx.Utils;

namespace Sphynx.Packet.Request
{
    /// <inheritdoc cref="SphynxPacketType.CHAT_JOIN_REQ"/>
    public sealed class ChatJoinRequestPacket : SphynxRequestPacket, IEquatable<ChatJoinRequestPacket>
    {
        /// <summary>
        /// Room ID of the room to join.
        /// </summary>
        public Guid RoomId { get; set; }

        /// <summary>
        /// Password for the room, if the room is guarded with a password.
        /// </summary>
        public string? Password { get; set; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.CHAT_JOIN_REQ;

        private const int ROOM_ID_OFFSET = DEFAULT_CONTENT_SIZE;
        private const int PASSWORD_SIZE_OFFSET = ROOM_ID_OFFSET + GUID_SIZE;
        private const int PASSWORD_OFFSET = PASSWORD_SIZE_OFFSET + sizeof(int);

        /// <summary>
        /// Creates a new <see cref="ChatCreateResponsePacket"/>.
        /// </summary>
        /// <param name="roomId">Room ID of the room to join.</param>
        /// <param name="password">Password for the room, if the room is guarded with a password.</param>
        public ChatJoinRequestPacket(Guid roomId, string? password = null) : this(Guid.Empty, Guid.Empty, roomId, password)
        {
        }

        /// <summary>
        /// Creates a new <see cref="ChatCreateResponsePacket"/>.
        /// </summary>
        /// <param name="userId">The user ID of the requesting user.</param>
        /// <param name="sessionId">The session ID for the requesting user.</param>
        /// <param name="roomId">Room ID of the room to join.</param>
        /// <param name="password">Password for the room, if the room is guarded with a password.</param>
        public ChatJoinRequestPacket(Guid userId, Guid sessionId, Guid roomId, string? password = null) : base(userId, sessionId)
        {
            RoomId = roomId;
            Password = password;
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="ChatJoinRequestPacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        /// <param name="packet">The deserialized packet.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out ChatJoinRequestPacket? packet)
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

                packet = new ChatJoinRequestPacket(userId.Value, sessionId.Value, roomId, passwordSize > 0 ? password : default);
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
            int contentSize = GUID_SIZE + sizeof(int) + passwordSize;

            packetBytes = new byte[SphynxPacketHeader.HEADER_SIZE + contentSize];
            var packetSpan = new Span<byte>(packetBytes);

            if (TrySerializeHeader(packetSpan.Slice(0, SphynxPacketHeader.HEADER_SIZE), contentSize) &&
                TrySerialize(packetSpan = packetSpan[SphynxPacketHeader.HEADER_SIZE..]))
            {
                RoomId.TryWriteBytes(packetSpan.Slice(ROOM_ID_OFFSET, GUID_SIZE));

                // TOOD: Write hashed password
                passwordSize.WriteBytes(packetSpan, PASSWORD_SIZE_OFFSET);
                TEXT_ENCODING.GetBytes(Password, packetSpan.Slice(PASSWORD_OFFSET, passwordSize));
            }

            packetBytes = null;
            return false;
        }

        /// <inheritdoc/>
        public bool Equals(ChatJoinRequestPacket? other) => base.Equals(other) && RoomId == other?.RoomId && Password == other?.Password;
    }
}