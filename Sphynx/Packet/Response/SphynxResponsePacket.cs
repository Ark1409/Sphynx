using System.Diagnostics.CodeAnalysis;

namespace Sphynx.Packet.Response
{
    /// <summary>
    /// Represents a response packet.
    /// </summary>
    public abstract class SphynxResponsePacket : SphynxPacket
    {
        /// <summary>
        /// <inheritdoc cref="SphynxErrorCode"/>
        /// </summary>
        public SphynxErrorCode ErrorCode { get; set; }

        protected const int ERROR_CODE_OFFSET = 0;
        protected const int DEFAULT_CONTENT_SIZE = sizeof(SphynxErrorCode);

        /// <summary>
        /// Creates a new <see cref="SphynxResponsePacket"/>.
        /// </summary>
        /// <param name="errorCode">The error code for the response packet.</param>
        public SphynxResponsePacket(SphynxErrorCode errorCode)
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Attempts to retrieve a <see cref="ErrorCode"/> from the raw <paramref name="buffer"/> bytes.
        /// </summary>
        /// <param name="buffer">The raw bytes to deserialize the data from.</param>
        /// <param name="errorCode">The deserialized error code.</param>
        /// <returns>true if the data could be deserialized; false otherwise.</returns>
        protected static bool TryDeserializeDefaults(ReadOnlySpan<byte> buffer, [NotNullWhen(true)] out SphynxErrorCode? errorCode)
        {
            if (buffer.Length < DEFAULT_CONTENT_SIZE)
            {
                errorCode = null;
                return false;
            }

            errorCode = (SphynxErrorCode)buffer[ERROR_CODE_OFFSET];
            return true;
        }

        /// <summary>
        /// Attempts to serialize the <see cref="ErrorCode"/> into the <paramref name="buffer"/>.
        /// </summary>
        /// <param name="buffer">The buffer to serialize this packet into.</param>
        /// <returns>true if this packet could be serialized; false otherwise.</returns>
        protected bool TrySerializeDefaults(Span<byte> buffer)
        {
            if (buffer.Length < DEFAULT_CONTENT_SIZE)
            {
                return false;
            }

            buffer[ERROR_CODE_OFFSET] = (byte)ErrorCode;
            return true;
        }

        /// <summary>
        /// Checks whether this packet is <see cref="SphynxErrorCode.SUCCESS"/>.
        /// </summary>
        /// <param name="packet">The packet to check.</param>
        /// <returns>true if this packet is a <see cref="SphynxErrorCode.SUCCESS"/>, false otherwise.</returns>
        public static implicit operator bool(SphynxResponsePacket packet) =>
            packet.ErrorCode == SphynxErrorCode.SUCCESS;

        /// <summary>
        /// Indicates whether the current packet has the same user and session ID as another request packet.
        /// </summary>
        /// <param name="other">A request packet to compare with this request packet.</param>
        /// <returns>true if the current packet is equal to the other parameter; otherwise, false.</returns>
        protected bool Equals(SphynxResponsePacket? other) => base.Equals(other) && ErrorCode == other?.ErrorCode;
    }
}