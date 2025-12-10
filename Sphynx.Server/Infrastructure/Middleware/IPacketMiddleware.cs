// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Network.Packet;
using Sphynx.Server.Client;
using Sphynx.Server.Infrastructure.Handlers;
using Sphynx.Server.Infrastructure.Routing;

namespace Sphynx.Server.Infrastructure.Middleware
{
    /// <summary>
    /// Represents the next middleware or handler in the packet processing pipeline.
    /// </summary>
    /// <typeparam name="TPacket">The type of packet being processed. This must stay consistent with the packet given to
    /// <see cref="IPacketMiddleware.InvokeAsync"/>.</typeparam>
    /// <param name="client">The client context.</param>
    /// <param name="packet">The packet to process.</param>
    /// <param name="ct">A cancellation token.</param>
    public delegate Task NextDelegate<in TPacket>(ISphynxClient client, TPacket packet, CancellationToken ct) where TPacket : SphynxPacket;

    /// <summary>
    /// A component that can process packets of type <typeparamref name="TPacket"/> before or after the
    /// <see cref="IPacketHandler{TPacket}">handlers</see> in a <see cref="IPacketRouter">packet pipeline</see>.
    /// </summary>
    /// <typeparam name="TPacket">The type of packet to be processed.</typeparam>
    public interface IPacketMiddleware<TPacket> where TPacket : SphynxPacket
    {
        /// <summary>
        /// Processes the specified packet and optionally invokes the <paramref name="next"/> middleware or handler in the pipeline.
        /// </summary>
        /// <param name="client">The client associated with the packet.</param>
        /// <param name="packet">The packet to be processed.</param>
        /// <param name="next">The next middleware or handler in the pipeline</param>
        /// <param name="cancellationToken">A cancellation token for the pipeline.</param>
        Task InvokeAsync(ISphynxClient client, TPacket packet, NextDelegate<TPacket> next, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// A component that can process packets of any type before (or after) the
    /// <see cref="IPacketHandler{TPacket}">handlers</see> in a <see cref="IPacketRouter">packet pipeline</see>.
    /// </summary>
    public interface IPacketMiddleware : IPacketMiddleware<SphynxPacket>
    {
    }
}
