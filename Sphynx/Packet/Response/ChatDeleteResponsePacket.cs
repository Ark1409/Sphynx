using System.Runtime.InteropServices;

namespace Sphynx.Packet.Response
{
    /// <inheritdoc cref="SphynxPacketType.CHAT_DEL_RES"/>
    public sealed class ChatDeleteResponsePacket : SphynxResponsePacket
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

            int errorMsgSize = MemoryMarshal.Cast<byte, int>(contents.Slice(ERROR_MSG_SIZE_OFFSET, sizeof(int)))[0];
            ErrorMessage = TEXT_ENCODING.GetString(contents.Slice(ERROR_MSG_OFFSET, errorMsgSize));
        }

        /// <summary>
        /// Creates a new <see cref="ChatDeleteResponsePacket"/>.
        /// </summary>
        /// <param name="errorCode">Error code for room password authentication.</param>
        /// <param name="errorMessage">Error code for room password authentication.</param>
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

            Span<byte> errorMsgSizeBytes = MemoryMarshal.Cast<int, byte>(stackalloc int[] { errorMsgSize });
            errorMsgSizeBytes.CopyTo(buffer.Slice(ERROR_MSG_SIZE_OFFSET, sizeof(int)));

            TEXT_ENCODING.GetBytes(ErrorMessage, buffer.Slice(ERROR_MSG_OFFSET, errorMsgSize));
        }
    }
}
