using System.Diagnostics.CodeAnalysis;

using Sphynx.ChatRoom;
using Sphynx.Utils;

namespace Sphynx.Packet.Request
{
    /// <inheritdoc cref="SphynxPacketType.CHAT_CREATE_REQ"/>
    public abstract class ChatCreateRequestPacket : SphynxRequestPacket, IEquatable<ChatCreateRequestPacket>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.CHAT_CREATE_REQ;

        /// <summary>
        /// <inheritdoc cref="ChatRoomType"/>
        /// </summary>
        public abstract ChatRoomType RoomType { get; }

        protected const int ROOM_TYPE_OFFSET = DEFAULT_CONTENT_SIZE;

        /// <summary>
        /// Creates a new <see cref="ChatCreateRequestPacket"/>.
        /// </summary>
        /// <param name="userId">The user ID of the requesting user.</param>
        /// <param name="sessionId">The session ID for the requesting user.</param>
        public ChatCreateRequestPacket(Guid userId, Guid sessionId) : base(userId, sessionId)
        {

        }

        /// <summary>
        /// Attempts to deserialize a <see cref="ChatCreateRequestPacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        /// <param name="packet">The deserialized packet.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out ChatCreateRequestPacket? packet)
        {
            if (contents.Length > ROOM_TYPE_OFFSET + sizeof(ChatRoomType))
            {
                switch ((ChatRoomType)contents[ROOM_TYPE_OFFSET])
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

        /// <inheritdoc/>
        public bool Equals(ChatCreateRequestPacket? other) => base.Equals(other) && RoomType == other?.RoomType;

        /// <summary>
        /// <see cref="ChatRoomType.DIRECT_MSG"/> room creation request.
        /// </summary>
        public sealed class Direct : ChatCreateRequestPacket, IEquatable<Direct>
        {
            /// <inheritdoc/>
            public override ChatRoomType RoomType => ChatRoomType.DIRECT_MSG;

            /// <summary>
            /// The user ID of the other user to create the DM with.
            /// </summary>
            public Guid OtherId { get; set; }

            private const int OTHER_ID_OFFSET = 0;

            /// <summary>
            /// Creates a new <see cref="ChatCreateRequestPacket"/>.
            /// </summary>
            /// <param name="otherId">The user ID of the other user to create the DM with.</param>
            public Direct(Guid otherId) : this(Guid.Empty, Guid.Empty, otherId)
            {
            }

            /// <summary>
            /// Creates a new <see cref="ChatCreateRequestPacket"/>.
            /// </summary>
            /// <param name="userId">The user ID of the requesting user.</param>
            /// <param name="sessionId">The session ID for the requesting user.</param>
            /// <param name="otherId">The user ID of the other user to create the DM with.</param>
            public Direct(Guid userId, Guid sessionId, Guid otherId) : base(userId, sessionId)
            {
                OtherId = otherId;
            }

            /// <summary>
            /// Attempts to deserialize a <see cref="Direct"/>.
            /// </summary>
            /// <param name="contents">Packet contents, excluding the header.</param>
            /// <param name="packet">The deserialized packet.</param>
            public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out Direct? packet)
            {
                if (contents.Length < OTHER_ID_OFFSET + GUID_SIZE || !TryDeserialize(contents, out var userId, out var sessionId) ||
                    (ChatRoomType)contents[ROOM_TYPE_OFFSET] != ChatRoomType.GROUP)
                {
                    packet = null;
                    return false;
                }

                packet = new Direct(userId.Value, sessionId.Value, new Guid(contents.Slice(OTHER_ID_OFFSET, GUID_SIZE)));
                return true;
            }

            /// <inheritdoc/>
            public override bool TrySerialize([NotNullWhen(true)] out byte[]? packetBytes)
            {
                int contentSize = OTHER_ID_OFFSET + GUID_SIZE;

                packetBytes = new byte[SphynxPacketHeader.HEADER_SIZE + contentSize];
                var packetSpan = new Span<byte>(packetBytes);

                if (TrySerializeHeader(packetSpan[..SphynxPacketHeader.HEADER_SIZE], contentSize) &&
                    TrySerialize(packetSpan = packetSpan[SphynxPacketHeader.HEADER_SIZE..]))
                {
                    packetSpan[ROOM_TYPE_OFFSET] = (byte)RoomType;
                    OtherId.TryWriteBytes(packetSpan.Slice(OTHER_ID_OFFSET, GUID_SIZE));
                    return true;
                }

                packetBytes = null;
                return false;
            }

            /// <inheritdoc/>
            public bool Equals(Direct? other) => base.Equals(other) && OtherId == other?.OtherId;
        }

        /// <summary>
        /// <see cref="ChatRoomType.GROUP"/> room creation request.
        /// </summary>
        public sealed class Group : ChatCreateRequestPacket, IEquatable<Group>
        {
            /// <summary>
            /// The name of the chat room.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// The password for the chat room.
            /// </summary>
            public string? Password { get; set; }

            /// <inheritdoc/>
            public override ChatRoomType RoomType => ChatRoomType.GROUP;

            private const int NAME_SIZE_OFFSET = ROOM_TYPE_OFFSET + sizeof(ChatRoomType);
            private const int NAME_OFFSET = NAME_SIZE_OFFSET + sizeof(int);

            /// <summary>
            /// Creates a new <see cref="ChatCreateRequestPacket"/>.
            /// </summary>
            /// <param name="name">The name for the chat room.</param>
            /// <param name="password">The password for the chat room, or null if the room is not guarded by a password.</param>
            public Group(string name, string? password = null) : this(Guid.Empty, Guid.Empty, name, password)
            {
            }

            /// <summary>
            /// Creates a new <see cref="ChatCreateRequestPacket"/>.
            /// </summary>
            /// <param name="userId">The user ID of the requesting user.</param>
            /// <param name="sessionId">The session ID for the requesting user.</param>
            /// <param name="name">The name for the chat room.</param>
            /// <param name="password">The password for the chat room, or null if the room is not guarded by a password.</param>
            public Group(Guid userId, Guid sessionId, string name, string? password = null) : base(userId, sessionId)
            {
                Name = name;
                Password = password;
            }

            /// <summary>
            /// Attempts to deserialize a <see cref="Group"/>.
            /// </summary>
            /// <param name="contents">Packet contents, excluding the header.</param>
            /// <param name="packet">The deserialized packet.</param>
            public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out Group? packet)
            {
                if (contents.Length <= NAME_OFFSET || !TryDeserialize(contents, out var userId, out var sessionId) ||
                    (ChatRoomType)contents[ROOM_TYPE_OFFSET] != ChatRoomType.GROUP)
                {
                    packet = null;
                    return false;
                }

                try
                {
                    int nameSize = contents.ReadInt32(NAME_SIZE_OFFSET);
                    string name = TEXT_ENCODING.GetString(contents.Slice(NAME_OFFSET, nameSize));

                    // TODO: Read hashed password bytes
                    int PASSWORD_SIZE_OFFSET = NAME_OFFSET + nameSize;
                    int passwordSize = contents.ReadInt32(PASSWORD_SIZE_OFFSET);

                    int PASSWORD_OFFSET = PASSWORD_SIZE_OFFSET + sizeof(int);
                    string password = TEXT_ENCODING.GetString(contents.Slice(PASSWORD_OFFSET, passwordSize));

                    packet = new Group(userId.Value, sessionId.Value, name, passwordSize > 0 ? password : default);
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
                int nameSize = TEXT_ENCODING.GetByteCount(Name);
                int passwordSize = TEXT_ENCODING.GetByteCount(Password ?? string.Empty);
                int contentSize = sizeof(ChatRoomType) + sizeof(int) + nameSize + sizeof(int) + passwordSize;

                packetBytes = new byte[SphynxPacketHeader.HEADER_SIZE + contentSize];
                var packetSpan = new Span<byte>(packetBytes);

                if (TrySerializeHeader(packetSpan[..SphynxPacketHeader.HEADER_SIZE], contentSize) &&
                    TrySerialize(packetSpan = packetSpan[SphynxPacketHeader.HEADER_SIZE..]))
                {
                    packetSpan[ROOM_TYPE_OFFSET] = (byte)RoomType;

                    nameSize.WriteBytes(packetSpan, NAME_SIZE_OFFSET);
                    TEXT_ENCODING.GetBytes(Name, packetSpan.Slice(NAME_OFFSET, nameSize));

                    // TODO: Serialize hashed password
                    int PASSWORD_SIZE_OFFSET = NAME_OFFSET + nameSize;
                    passwordSize.WriteBytes(packetSpan, PASSWORD_SIZE_OFFSET);

                    int PASSWORD_OFFSET = PASSWORD_SIZE_OFFSET + sizeof(int);
                    TEXT_ENCODING.GetBytes(Password, packetSpan.Slice(PASSWORD_OFFSET, passwordSize));
                    return true;
                }

                packetBytes = null;
                return false;
            }

            /// <inheritdoc/>
            public bool Equals(Group? other) => base.Equals(other) && Name == other?.Name && Password == other?.Password;
        }
    }
}
