// Copyright (c) Ark -α- & Specyy.Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Network.PacketV2;
using Sphynx.ServerV2.Client;
using Sphynx.ServerV2.Infrastructure.Handlers;

namespace Sphynx.ServerV2.Infrastructure.Routing
{
    /// <summary>
    /// Represents a router that dispatches incoming packets to their corresponding handlers.
    /// </summary>
    public interface IPacketRouter
    {
        /// <summary>
        /// Registers a packet handler for the specified packet type.
        /// </summary>
        /// <typeparam name="TPacket">The type of packet the handler processes.</typeparam>
        /// <param name="handler">The handler instance responsible for processing the specified packet type.</param>
        IPacketRouter UseHandler<TPacket>(IPacketHandler<TPacket> handler) where TPacket : SphynxPacket;

        /// <summary>
        /// Executes the appropriate handler for the given packet, if one is registered.
        /// </summary>
        /// <param name="ctx">The client context from which the packet was received.</param>
        /// <param name="packet">The packet to be handled.</param>
        /// <param name="token">A cancellation token for aborting the handling operation.</param>
        /// <returns>A task that completes once the packet has been handled or no handler is found.</returns>
        /// <exception cref="ArgumentException">If no handler was found for the <paramref name="packet"/>.</exception>
        Task ExecuteAsync(ISphynxClient ctx, SphynxPacket packet, CancellationToken token = default);
    }
}
