// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Network.PacketV2;

namespace Sphynx.Server.Auth.Handlers
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
        /// <param name="client">The client for which the packet should be handled. For example, response information will
        /// be forwarded to this client.</param>
        /// <param name="packet">The packet to handle.</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns>The started handling task, returning a bool representing whether the packet could be sent.</returns>
        public ValueTask HandlePacketAsync(SphynxClient client, TPacket packet, CancellationToken token = default);
    }

    /// <summary>
    /// Represents a type that performs actions according to information from specific packets.
    /// </summary>
    public interface IPacketHandler : IPacketHandler<SphynxPacket>
    {
    }
}
