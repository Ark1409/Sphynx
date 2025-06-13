using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Sphynx.Model.ChatRoom;
using Sphynx.Network.Transport;
using Sphynx.Utils;

namespace Sphynx.Network.Packet.Request
{
    /// <inheritdoc cref="SphynxPacketType.ROOM_CREATE_REQ"/>
    public abstract class RoomCreateRequestPacket : SphynxRequestPacket, IEquatable<RoomCreateRequestPacket>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.ROOM_CREATE_REQ;

        /// <summary>
        /// <inheritdoc cref="ChatRoomType"/>
        /// </summary>
        public abstract ChatRoomType RoomType { get; }

        protected static readonly int ROOM_TYPE_OFFSET = DEFAULT_CONTENT_SIZE;
        protected static readonly int DEFAULTS_SIZE = DEFAULT_CONTENT_SIZE + sizeof(ChatRoomType);

        /// <summary>
        /// Creates a new <see cref="RoomCreateRequestPacket"/>.
        /// </summary>
        /// <param name="userId">The user ID of the requesting user.</param>
        /// <param name="sessionId">The session ID for the requesting user.</param>
        public RoomCreateRequestPacket(Guid userId, Guid sessionId) : base(userId, sessionId)
        {
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="RoomCreateRequestPacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        /// <param name="packet">The deserialized packet.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out RoomCreateRequestPacket? packet)
        {
            if (contents.Length > DEFAULTS_SIZE)
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

        protected override bool TrySerializeDefaults(Span<byte> buffer)
        {
            if (buffer.Length < DEFAULTS_SIZE || !base.TrySerializeDefaults(buffer))
            {
                return false;
            }

            buffer[ROOM_TYPE_OFFSET] = (byte)RoomType;
            return true;
        }

        protected static bool TryDeserializeDefaults(ReadOnlySpan<byte> contents,
            [NotNullWhen(true)] out Guid? userId,
            [NotNullWhen(true)] out Guid? sessionId,
            [NotNullWhen(true)] out ChatRoomType? roomType)
        {
            if (contents.Length < DEFAULTS_SIZE || !TryDeserializeDefaults(contents, out userId, out sessionId))
            {
                roomType = null;
                userId = null;
                sessionId = null;
                return false;
            }

            roomType = (ChatRoomType)contents[ROOM_TYPE_OFFSET];
            return true;
        }

        /// <inheritdoc/>
        public bool Equals(RoomCreateRequestPacket? other) => base.Equals(other) && RoomType == other?.RoomType;

        /// <summary>
        /// <see cref="ChatRoomType.DIRECT_MSG"/> room creation request.
        /// </summary>
        public sealed class Direct : RoomCreateRequestPacket, IEquatable<Direct>
        {
            /// <inheritdoc/>
            public override ChatRoomType RoomType => ChatRoomType.DIRECT_MSG;

            /// <summary>
            /// The user ID of the other user to create the DM with.
            /// </summary>
            public Guid OtherId { get; set; }

            private static readonly int OTHER_ID_OFFSET = DEFAULTS_SIZE;

            /// <summary>
            /// Creates a new <see cref="RoomCreateRequestPacket"/>.
            /// </summary>
            /// <param name="otherId">The user ID of the other user to create the DM with.</param>
            public Direct(Guid otherId) : this(Guid.Empty, Guid.Empty, otherId)
            {
            }

            /// <summary>
            /// Creates a new <see cref="RoomCreateRequestPacket"/>.
            /// </summary>
            /// <param name="userId">The user ID of the requesting user.</param>
            /// <param name="sessionId">The session ID for the requesting user.</param>
            /// <param name="otherId">The user ID of the other user to create the DM with.</param>
            public Direct(Guid userId, Guid sessionId, Guid otherId) : base(userId, sessionId)
            {
                OtherId = otherId;
            }

            /// <summary>
            /// Attempts to deserialize a <see cref="RoomCreateRequestPacket.Direct"/>.
            /// </summary>
            /// <param name="contents">Packet contents, excluding the header.</param>
            /// <param name="packet">The deserialized packet.</param>
            public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out Direct? packet)
            {
                int contentSize = DEFAULTS_SIZE + GUID_SIZE;

                if (contents.Length < contentSize || !TryDeserializeDefaults(contents, out var userId, out var sessionId, out var roomType) ||
                    roomType != ChatRoomType.DIRECT_MSG)
                {
                    packet = null;
                    return false;
                }

                var otherId = new Guid(contents.Slice(OTHER_ID_OFFSET, GUID_SIZE));
                packet = new Direct(userId.Value, sessionId.Value, otherId);

                return true;
            }

            /// <inheritdoc/>
            public override bool TrySerialize([NotNullWhen(true)] out byte[]? packetBytes)
            {
                int contentSize = DEFAULTS_SIZE + GUID_SIZE;
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

                int contentSize = DEFAULTS_SIZE + GUID_SIZE;

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
                finally
                {
                    ArrayPool<byte>.Shared.Return(rawBuffer);
                }

                return false;
            }

            private bool TrySerialize(Span<byte> buffer)
            {
                if (TrySerializeHeader(buffer) && TrySerializeDefaults(buffer = buffer[SphynxPacketHeader.Size..]))
                {
                    OtherId.TryWriteBytes(buffer.Slice(OTHER_ID_OFFSET, GUID_SIZE));
                    return true;
                }

                return false;
            }

            /// <inheritdoc/>
            public bool Equals(Direct? other) => base.Equals(other) && OtherId == other?.OtherId;
        }

        /// <summary>
        /// <see cref="ChatRoomType.GROUP"/> room creation request.
        /// </summary>
        public sealed class Group : RoomCreateRequestPacket, IEquatable<Group>
        {
            /// <summary>
            /// The name of the chat room.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// The password for the chat room.
            /// </summary>
            public string? Password { get; set; }

            /// <summary>
            /// Whether this room is public.
            /// </summary>
            public bool Public { get; set; }

            /// <inheritdoc/>
            public override ChatRoomType RoomType => ChatRoomType.GROUP;

            private static readonly int VISIBILITY_OFFSET = DEFAULTS_SIZE;
            private static readonly int NAME_SIZE_OFFSET = VISIBILITY_OFFSET + sizeof(bool);
            private static readonly int NAME_OFFSET = NAME_SIZE_OFFSET + sizeof(int);

            /// <summary>
            /// Creates a new <see cref="RoomCreateRequestPacket"/>.
            /// </summary>
            /// <param name="name">The name for the chat room.</param>
            /// <param name="password">The password for the chat room, or null if the room is not guarded by a password.</param>
            /// <param name="public">Whether this room is public.</param>
            public Group(string name, string? password = null, bool @public = true) : this(Guid.Empty, Guid.Empty, name, password, @public)
            {
            }

            /// <summary>
            /// Creates a new <see cref="RoomCreateRequestPacket"/>.
            /// </summary>
            /// <param name="userId">The user ID of the requesting user.</param>
            /// <param name="sessionId">The session ID for the requesting user.</param>
            /// <param name="name">The name for the chat room.</param>
            /// <param name="password">The password for the chat room, or null if the room is not guarded by a password.</param>
            /// <param name="public">Whether this room is public.</param>
            public Group(Guid userId, Guid sessionId, string name, string? password = null, bool @public = true) : base(userId, sessionId)
            {
                Name = name;
                Password = password;
                Public = @public;
            }

            /// <summary>
            /// Attempts to deserialize a <see cref="RoomCreateRequestPacket.Group"/>.
            /// </summary>
            /// <param name="contents">Packet contents, excluding the header.</param>
            /// <param name="packet">The deserialized packet.</param>
            public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out Group? packet)
            {
                int minContentSize = DEFAULTS_SIZE + sizeof(int) + sizeof(int);

                if (contents.Length < minContentSize || !TryDeserializeDefaults(contents, out var userId, out var sessionId, out var roomType) ||
                    roomType != ChatRoomType.GROUP)
                {
                    packet = null;
                    return false;
                }

                try
                {
                    bool isPublic = contents[VISIBILITY_OFFSET] != 0;
                    int nameSize = contents[NAME_SIZE_OFFSET..].ReadInt32();
                    string name = TEXT_ENCODING.GetString(contents.Slice(NAME_OFFSET, nameSize));

                    int PASSWORD_SIZE_OFFSET = NAME_OFFSET + nameSize;
                    int passwordSize = contents[PASSWORD_SIZE_OFFSET..].ReadInt32();

                    int PASSWORD_OFFSET = PASSWORD_SIZE_OFFSET + sizeof(int);
                    string password = TEXT_ENCODING.GetString(contents.Slice(PASSWORD_OFFSET, passwordSize));

                    packet = new Group(userId.Value, sessionId.Value, name, passwordSize > 0 ? password : null, isPublic);
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
                GetPacketInfo(out int nameSize, out int passwordSize, out int contentSize);

                int bufferSize = SphynxPacketHeader.Size + contentSize;

                if (!TrySerialize(packetBytes = new byte[bufferSize], nameSize, passwordSize))
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

                GetPacketInfo(out int nameSize, out int passwordSize, out int contentSize);

                int bufferSize = SphynxPacketHeader.Size + contentSize;
                byte[] rawBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
                var buffer = rawBuffer.AsMemory()[..bufferSize];

                try
                {
                    if (TrySerialize(buffer.Span, nameSize, passwordSize))
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void GetPacketInfo(out int nameSize, out int passwordSize, out int contentSize)
            {
                nameSize = TEXT_ENCODING.GetByteCount(Name);
                passwordSize = !string.IsNullOrEmpty(Password) ? TEXT_ENCODING.GetByteCount(Password) : 0;
                contentSize = DEFAULTS_SIZE + sizeof(int) + nameSize + sizeof(int) + passwordSize;
            }

            private bool TrySerialize(Span<byte> buffer, int nameSize, int passwordSize)
            {
                if (TrySerializeHeader(buffer) && TrySerializeDefaults(buffer = buffer[SphynxPacketHeader.Size..]))
                {
                    buffer[VISIBILITY_OFFSET] = (byte)(Public ? 0 : 1);

                    nameSize.WriteBytes(buffer[NAME_SIZE_OFFSET..]);
                    TEXT_ENCODING.GetBytes(Name, buffer.Slice(NAME_OFFSET, nameSize));

                    int PASSWORD_SIZE_OFFSET = NAME_OFFSET + nameSize;
                    passwordSize.WriteBytes(buffer[PASSWORD_SIZE_OFFSET..]);

                    int PASSWORD_OFFSET = PASSWORD_SIZE_OFFSET + sizeof(int);
                    TEXT_ENCODING.GetBytes(Password, buffer.Slice(PASSWORD_OFFSET, passwordSize));
                    return true;
                }

                return false;
            }

            /// <inheritdoc/>
            public bool Equals(Group? other) => base.Equals(other) && Name == other?.Name && Password == other?.Password;
        }
    }
}