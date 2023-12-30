using System.Runtime.InteropServices;

using Sphynx.Utils;

namespace Sphynx.Packet.Response
{
    /// <inheritdoc cref="SphynxPacketType.CHAT_DEL_RES"/>
    public sealed class ChatDeleteResponsePacket : SphynxResponsePacket, IEquatable<ChatDeleteResponsePacket>
    {
        /// <summary>
        /// Error code for password authentication.
        /// </summary>
        public SphynxErrorCode ErrorCode { get; set; }

        /// <summary>
        /// The message for the <see cref="ErrorCode"/>.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.LOGIN_RES;

        private const int ERROR_CODE_OFFSET = 0;
        private const int ERROR_MSG_SIZE_OFFSET = ERROR_CODE_OFFSET + sizeof(SphynxErrorCode);
        private const int ERROR_MSG_OFFSET = ERROR_MSG_SIZE_OFFSET + sizeof(int);

        /// <summary>
        /// Creates a <see cref="ChatDeleteResponsePacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        public ChatDeleteResponsePacket(ReadOnlySpan<byte> contents)
        {
            ErrorCode = (SphynxErrorCode)contents[ERROR_CODE_OFFSET];

            int errorMsgSize = contents.Slice(ERROR_MSG_OFFSET, sizeof(int)).ReadInt32();
            ErrorMessage = TEXT_ENCODING.GetString(contents.Slice(ERROR_MSG_OFFSET, errorMsgSize));
        }

        /// <summary>
        /// Creates a new <see cref="ChatDeleteResponsePacket"/>.
        /// </summary>
        /// <param name="errorCode">Error code for room password authentication.</param>
        /// <param name="errorMessage">Error message for <paramref name="errorCode"/>.</param>
        public ChatDeleteResponsePacket(SphynxErrorCode errorCode, string errorMessage)
        {
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
        }

        /// <inheritdoc/>
        public override byte[] Serialize()
        {
            int errorMsgSize = TEXT_ENCODING.GetByteCount(ErrorMessage);
            int contentSize = sizeof(SphynxErrorCode) + sizeof(int) + errorMsgSize;

            byte[] serializedBytes = new byte[SphynxResponseHeader.HEADER_SIZE + contentSize];
            var serialzationSpan = new Span<byte>(serializedBytes);

            SerializeHeader(serialzationSpan.Slice(0, SphynxResponseHeader.HEADER_SIZE), contentSize);
            SerializeContents(serialzationSpan.Slice(SphynxResponseHeader.HEADER_SIZE), errorMsgSize);

            return serializedBytes;
        }

        private void SerializeContents(Span<byte> buffer, int errorMsgSize)
        {
            buffer[ERROR_CODE_OFFSET] = (byte)ErrorCode;

            errorMsgSize.WriteBytes(buffer.Slice(ERROR_MSG_OFFSET, sizeof(int)));
            TEXT_ENCODING.GetBytes(ErrorMessage, buffer.Slice(ERROR_MSG_OFFSET, errorMsgSize));
        }

        /// <inheritdoc/>
        public bool Equals(ChatDeleteResponsePacket? other) => ErrorCode == other?.ErrorCode && ErrorMessage == other?.ErrorMessage;
    }
}
