// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Net;
using Sphynx.Network.Packet;

namespace Sphynx.Server.Client
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
        /// Sends a message to this specific client over the network.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="cancellationToken">A cancellation token for the send request.</param>
        /// <returns>A task representing the send operation.</returns>
        ValueTask SendAsync(SphynxMessage message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Signals a wish to disconnect the client from the server, with the given exception.
        /// </summary>
        /// <param name="disconnectException">The disconnection exception.</param>
        /// <param name="waitForFinish">Whether to wait for the client to finish execution.</param>
        /// <returns>A task representing the stop operation. If <paramref name="waitForFinish"/> is true, this task will not
        /// complete until the client has been disconnected; else, it will return after sending a stop signal.</returns>
        ValueTask StopAsync(Exception? disconnectException = null, bool waitForFinish = true);
    }
}
