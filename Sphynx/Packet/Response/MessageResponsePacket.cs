using System.Diagnostics.CodeAnalysis;

namespace Sphynx.Packet.Response
{
    /// <inheritdoc cref="SphynxPacketType.MSG_RES"/>
    public sealed class MessageResponsePacket : SphynxResponsePacket, IEquatable<MessageResponsePacket>
    {
        /// <inheritdoc/>
        public override SphynxPacketType PacketType => SphynxPacketType.MSG_RES;

        /// <summary>
        /// Creates a new <see cref="MessageResponsePacket"/> with <see cref="SphynxErrorCode.SUCCESS"/>.
        /// </summary>
        public MessageResponsePacket() : this(SphynxErrorCode.SUCCESS)
        {
        }

        /// <summary>
        /// Creates a new <see cref="MessageResponsePacket"/>.
        /// </summary>
        /// <param name="errorCode">Error code for login attempt.</param>
        public MessageResponsePacket(SphynxErrorCode errorCode) : base(errorCode)
        {
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="LoginResponsePacket"/>.
        /// </summary>
        /// <param name="contents">Packet contents, excluding the header.</param>
        /// <param name="packet">The deserialized packet.</param>
        public static bool TryDeserialize(ReadOnlySpan<byte> contents, [NotNullWhen(true)] out MessageResponsePacket? packet)
        {
            if (TryDeserialize(contents, out SphynxErrorCode? errorCode))
            {
                packet = new MessageResponsePacket(errorCode.Value);
                return true;
            }

            packet = null;
            return false;
        }

        /// <inheritdoc/>
        public override bool TrySerialize([NotNullWhen(true)] out byte[]? packetBytes)
        {
            int contentSize = sizeof(SphynxErrorCode);

            packetBytes = new byte[contentSize];
            var packetSpan = new Span<byte>(packetBytes);

            if (TrySerializeHeader(packetSpan, contentSize) && TrySerialize(packetSpan[SphynxPacketHeader.HEADER_SIZE..]))
            {
                return true;
            }

            packetBytes = null;
            return false;
        }

        /// <inheritdoc/>
        public bool Equals(MessageResponsePacket? other) => base.Equals(other);
    }
}
