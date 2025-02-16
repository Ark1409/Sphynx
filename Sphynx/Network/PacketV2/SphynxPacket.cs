namespace Sphynx.Network.PacketV2
{
    /// <summary>
    /// Represents a packet sent between nodes on a Sphynx network.
    /// </summary>
    public abstract class SphynxPacket
    {
        /// <summary>
        /// Packet type for this packet.
        /// </summary>
        public abstract SphynxPacketType PacketType { get; }

        /// <summary>
        /// Indicates whether the current packet has the same packet type as another packet.
        /// </summary>
        /// <param name="other">A packet to compare with this packet.</param>
        /// <returns>true if the current packet is equal to the other parameter; otherwise, false.</returns>
        protected virtual bool Equals(SphynxPacket? other) => PacketType == other?.PacketType;
    }
}
