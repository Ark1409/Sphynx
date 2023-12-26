using System.Runtime.InteropServices;

namespace Sphynx.Packet.Response
{
    /// <inheritdoc cref="SphynxPacketType.LOGIN_RES"/>
    public sealed class LoginResponsePacket : SphynxPacket
    {
        /// <summary>
        /// <inheritdoc cref="SphynxErrorCode"/>
        /// </summary>
        public SphynxErrorCode ErrorCode { get; set; }

        /// <summary>
        /// The message for the <see cref="ErrorCode"/>.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.LOGIN_RES;

        /// <summary>
        /// Creates a <see cref="LoginResponsePacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        public LoginResponsePacket(ReadOnlySpan<byte> contents)
        {
            ErrorCode = (SphynxErrorCode)contents[0];

            const int ERROR_MSG_SIZE_OFFSET = sizeof(SphynxErrorCode);
            int errorMsgSize = MemoryMarshal.Cast<byte, int>(contents.Slice(ERROR_MSG_SIZE_OFFSET, sizeof(int)))[0];

            const int ERROR_MSG_OFFSET = ERROR_MSG_SIZE_OFFSET + sizeof(int);
            ErrorMessage = TEXT_ENCODING.GetString(contents.Slice(ERROR_MSG_OFFSET, errorMsgSize));
        }

        /// <summary>
        /// Creates a new <see cref="LoginResponsePacket"/>.
        /// </summary>
        /// <param name="errorCode">Error code for login attempt.</param>
        /// <param name="errorMessage">Error code for login attempt.</param>
        public LoginResponsePacket(SphynxErrorCode errorCode, string errorMessage)
        {
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
        }

        /// <inheritdoc/>
        public override byte[] Serialize()
        {
            int errorMsgSize = TEXT_ENCODING.GetByteCount(ErrorMessage);
            int contentSize = sizeof(SphynxErrorCode) + errorMsgSize;

            byte[] serializedBytes = new byte[SphynxRequestHeader.HEADER_SIZE + contentSize];

            SerializeData(new Span<byte>(serializedBytes), contentSize, errorMsgSize);

            return serializedBytes;
        }

        private void SerializeData(Span<byte> stream, int contentSize, int errorMsgSize)
        {
            var header = new SphynxRequestHeader(PacketType, contentSize);
            header.Serialize(stream.Slice(0, SphynxRequestHeader.HEADER_SIZE));

            stream[SphynxRequestHeader.HEADER_SIZE] = (byte)ErrorCode;

            const int ERROR_MSG_SIZE_OFFSET = SphynxRequestHeader.HEADER_SIZE + sizeof(SphynxErrorCode);
            Span<byte> serializedErrorMsgSize = MemoryMarshal.Cast<int, byte>(stackalloc int[] { errorMsgSize });
            serializedErrorMsgSize.CopyTo(stream.Slice(ERROR_MSG_SIZE_OFFSET, serializedErrorMsgSize.Length));

            int ERROR_MSG_OFFSET = ERROR_MSG_SIZE_OFFSET + serializedErrorMsgSize.Length;
            TEXT_ENCODING.GetBytes(ErrorMessage, stream.Slice(ERROR_MSG_OFFSET));
        }
    }
}
