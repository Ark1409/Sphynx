// Copyright (c) Ark -α- & Specyy.Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Network.Packet;
using Sphynx.Server.Client;
using Sphynx.Server.Infrastructure.Handlers;

namespace Sphynx.Server.Infrastructure.Routing
{
    /// <summary>
    /// Represents a router that dispatches incoming messages to their corresponding handlers.
    /// </summary>
    public interface IMessageRouter
    {
        /// <summary>
        /// Registers a message handler for the specified message type.
        /// </summary>
        /// <typeparam name="TMessage">The type of message the handler processes.</typeparam>
        /// <param name="handler">The handler instance responsible for processing the specified message type.</param>
        IMessageRouter UseHandler<TMessage>(IMessageHandler<TMessage> handler) where TMessage : SphynxMessage;

        /// <summary>
        /// Executes the appropriate handler for the given message, if one is registered.
        /// </summary>
        /// <param name="ctx">The client context from which the message was received.</param>
        /// <param name="message">The message to be handled.</param>
        /// <param name="token">A cancellation token for aborting the handling operation.</param>
        /// <returns>A task that completes once the message has been handled or no handler is found.</returns>
        /// <exception cref="ArgumentException">If no handler was found for the <paramref name="message"/>.</exception>
        Task ExecuteAsync(ISphynxClient ctx, SphynxMessage message, CancellationToken token = default);
    }
}
