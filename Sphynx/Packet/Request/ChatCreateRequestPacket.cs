using System.Runtime.InteropServices;

using Sphynx.Utils;

namespace Sphynx.Packet.Request
{
    /// <inheritdoc cref="SphynxPacketType.CHAT_CREATE_REQ"/>
    public sealed class ChatCreateRequestPacket : SphynxRequestPacket, IEquatable<ChatCreateRequestPacket>
    {
        /// <summary>
        /// The name of the chat room.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The password for the chat room, or null if the room is not guarded by a password.
        /// </summary>
        public string? Password { get; set; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.CHAT_CREATE_REQ;

        private const int NAME_SIZE_OFFSET = 0;
        private const int NAME_OFFSET = NAME_SIZE_OFFSET + sizeof(int);
        private const int PASSWORD_SIZE = 256;

        /// <summary>
        /// Creates a <see cref="ChatCreateRequestPacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        public ChatCreateRequestPacket(ReadOnlySpan<byte> contents)
        {
            int nameSize = contents.Slice(NAME_SIZE_OFFSET, sizeof(int)).ReadInt32();
            Name = TEXT_ENCODING.GetString(contents.Slice(NAME_OFFSET, nameSize));

            // ---------------------------- //
            // TODO: Read password bytes    //
            // ---------------------------- //
        }

        /// <summary>
        /// Creates a new <see cref="ChatCreateRequestPacket"/>.
        /// </summary>
        /// <param name="name">The name for the chat room.</param>
        /// <param name="password">The password for the chat room, or null if the room is not guarded by a password.</param>
        public ChatCreateRequestPacket(string name, string? password = null)
        {
            Name = name;
            Password = password;
        }

        /// <inheritdoc/>
        public override byte[] Serialize()
        {
            int nameSize = TEXT_ENCODING.GetByteCount(Name);
            const int PASSWORD_SIZE = 256;
            int contentSize = sizeof(int) + nameSize + PASSWORD_SIZE;

            byte[] serializedBytes = new byte[SphynxRequestHeader.HEADER_SIZE + contentSize];
            var serializationSpan = new Span<byte>(serializedBytes);

            SerializeHeader(serializationSpan.Slice(0, SphynxRequestHeader.HEADER_SIZE), contentSize);
            SerializeContents(serializationSpan.Slice(SphynxRequestHeader.HEADER_SIZE), nameSize);

            return serializedBytes;
        }

        private void SerializeContents(Span<byte> buffer, int nameSize)
        {
            nameSize.WriteBytes(buffer.Slice(NAME_SIZE_OFFSET, sizeof(int)));
            TEXT_ENCODING.GetBytes(Name, buffer.Slice(NAME_OFFSET, nameSize));

            int PASSWORD_OFFSET = NAME_OFFSET + nameSize;
            // -------------------------------- //
            // TODO: Serialize hashed password  //
            // -------------------------------- //
        }

        /// <inheritdoc/>
        public bool Equals(ChatCreateRequestPacket? other) => Name == other?.Name && Password == other?.Password;
    }
}
