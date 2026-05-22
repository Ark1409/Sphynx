// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Network.Packet;
using Sphynx.Server.Client;

namespace Sphynx.Server.Infrastructure.Handlers
{
    /// <summary>
    /// Represents a type that handles specific messages requests from clients.
    /// </summary>
    /// <typeparam name="TMessage">The message type accepted by the handler.</typeparam>
    public interface IMessageHandler<in TMessage> where TMessage : SphynxMessage
    {
        /// <summary>
        /// Asynchronously handles the given <paramref name="message"/> request.
        /// </summary>
        /// <param name="client">The client for which the message should be handled. For instance, response information will
        /// be forwarded to this client.</param>
        /// <param name="message">The message to handle.</param>
        /// <param name="cancellationToken">A cancellation token for the handling request.</param>
        /// <returns>The started handling task, returning a bool representing whether the message could be sent.</returns>
        Task HandleMessageAsync(ISphynxClient client, TMessage message, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents a type that handles generic messages requests from clients.
    /// </summary>
    public interface IMessageHandler : IMessageHandler<SphynxMessage>
    {
    }
}
