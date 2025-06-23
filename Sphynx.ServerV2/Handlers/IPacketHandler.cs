// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Network.PacketV2;
using Sphynx.ServerV2.Client;

namespace Sphynx.ServerV2.Handlers
{
    /// <summary>
    /// Represents a type that handles specific packets requests from clients.
    /// </summary>
    /// <typeparam name="TPacket">The packet type accepted by the handler.</typeparam>
    public interface IPacketHandler<in TPacket> where TPacket : SphynxPacket
    {
        /// <summary>
        /// Asynchronously handles the given <paramref name="packet"/> request.
        /// </summary>
        /// <param name="client">The client for which the packet should be handled. For instance, response information will
        /// be forwarded to this client.</param>
        /// <param name="packet">The packet to handle.</param>
        /// <param name="cancellationToken">A cancellation token for the handling request.</param>
        /// <returns>The started handling task, returning a bool representing whether the packet could be sent.</returns>
        public Task HandlePacketAsync(ISphynxClient client, TPacket packet, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents a type that handles generic packets requests from clients.
    /// </summary>
    public interface IPacketHandler : IPacketHandler<SphynxPacket>
    {
    }
}
