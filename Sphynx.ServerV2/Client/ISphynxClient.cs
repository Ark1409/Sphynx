// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Net;
using Sphynx.Network.PacketV2;

namespace Sphynx.ServerV2.Client
{
    /// <summary>
    /// Represents a remote Sphynx client endpoint to which data can be sent.
    /// </summary>
    public interface ISphynxClient
    {
        /// <summary>
        /// The unique ID for this client.
        /// </summary>
        Guid ClientId { get; }

        /// <summary>
        /// The endpoint for this client.
        /// </summary>
        IPEndPoint EndPoint { get; }

        /// <summary>
        /// Sends a packet to this specific client over the network.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        /// <param name="cancellationToken">A cancellation token for the send request.</param>
        /// <returns>A task representing the send operation.</returns>
        ValueTask SendAsync(SphynxPacket packet, CancellationToken cancellationToken = default);
    }
}
