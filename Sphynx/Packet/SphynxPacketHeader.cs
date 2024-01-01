﻿namespace Sphynx.Packet
{
    /// <summary>
    /// Represents the header of a <see cref="SphynxPacket"/>.
    /// </summary>
    public abstract class SphynxPacketHeader : IEquatable<SphynxPacketHeader>
    {
        /// <summary>
        /// The packet signature to safe-guards against corrupted packets.
        /// </summary>
        public const ushort SIGNATURE = 0x5350;

        /// <summary>
        /// The type of this packet.
        /// </summary>
        public SphynxPacketType PacketType { get; set; }

        /// <summary>
        /// The size of the content in this packet in bytes. 
        /// </summary>
        public int ContentSize { get; set; }

        /// <summary>
        /// Returns the size of this particular header in bytes.
        /// </summary>
        public abstract int HeaderSize { get; }

        /// <summary>
        /// Creates a new <see cref="SphynxPacketHeader"/>.
        /// </summary>
        /// <param name="packetType">The type of packet.</param>
        /// <param name="contentSize">The size of the packet's contents.</param>
        public SphynxPacketHeader(SphynxPacketType packetType, int contentSize)
        {
            PacketType = packetType;
            ContentSize = contentSize;
        }

        /// <summary>
        /// Checks whether the <paramref name="packetType"/> is a <see cref="Request.SphynxRequestHeader"/>.
        /// </summary>
        /// <param name="packetType"></param>
        /// <returns>true if the <paramref name="packetType"/> is meant for a <see cref="Request.SphynxRequestHeader"/>;
        /// false otherwise.</returns>
        public static bool IsRequest(SphynxPacketType packetType) => (int)packetType > 0;

        /// <summary>
        /// Checks whether the <paramref name="packetType"/> is a <see cref="Response.SphynxResponseHeader"/>.
        /// </summary>
        /// <param name="packetType"></param>
        /// <returns>true if the <paramref name="packetType"/> is meant for a <see cref="Response.SphynxResponseHeader"/>;
        /// false otherwise.</returns>
        public static bool IsResponse(SphynxPacketType packetType) => (int)packetType < 0;

        /// <summary>
        /// Serializes this packet header into a tightly-packed byte array.
        /// </summary>
        /// <returns>This packet header serialized as a byte array.</returns>
        public virtual byte[] Serialize()
        {
            byte[] bytes = new byte[HeaderSize];
            Serialize(bytes);
            return bytes;
        }

        /// <summary>
        /// Serializes this header into a buffer of bytes.
        /// </summary>
        /// <param name="buffer">The buffer to serialize this header into.</param>
        public abstract void Serialize(Span<byte> buffer);

        /// <inheritdoc/>
        public virtual bool Equals(SphynxPacketHeader? other) => PacketType == other?.PacketType && ContentSize == other?.ContentSize;
    }
}