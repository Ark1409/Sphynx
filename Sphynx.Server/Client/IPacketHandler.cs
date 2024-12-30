using Sphynx.Network.Packet;

namespace Sphynx.Server.Client
{
    /// <summary>
    /// Represents a type that performs actions according to information from specific packets.
    /// </summary>
    /// <typeparam name="TPacket">The packet type accepted by the handler.</typeparam>
    public interface IPacketHandler<in TPacket> where TPacket : SphynxPacket
    {
        /// <summary>
        /// Asynchronously performs the appropriate actions for the given <paramref name="packet"/> request.
        /// </summary>
        /// <param name="packet">The packet to handle.</param>
        /// <returns>The started handling task, returning a bool representing whether the packet could be sent.</returns>
        public Task<bool> HandlePacketAsync(TPacket packet);
    }
    
    /// <summary>
    /// Represents a type that performs actions according to information from specific packets.
    /// </summary>
    public interface IPacketHandler : IPacketHandler<SphynxPacket>
    {

    }
}