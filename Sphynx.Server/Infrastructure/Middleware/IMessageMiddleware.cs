// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Network.Packet;
using Sphynx.Server.Client;
using Sphynx.Server.Infrastructure.Handlers;
using Sphynx.Server.Infrastructure.Routing;

namespace Sphynx.Server.Infrastructure.Middleware
{
    /// <summary>
    /// Represents the next middleware or handler in the message processing pipeline.
    /// </summary>
    /// <typeparam name="TMessage">The type of message being processed. This must stay consistent with the message given to
    /// <see cref="IMessageMiddleware.InvokeAsync"/>.</typeparam>
    /// <param name="client">The client context.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="ct">A cancellation token.</param>
    public delegate Task NextDelegate<in TMessage>(ISphynxClient client, TMessage message, CancellationToken ct) where TMessage : SphynxMessage;

    /// <summary>
    /// A component that can process messages of type <typeparamref name="TMessage"/> before or after the
    /// <see cref="IMessageHandler{TPacket}">handlers</see> in a <see cref="IMessageRouter">message pipeline</see>.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to be processed.</typeparam>
    public interface IMessageMiddleware<TMessage> where TMessage : SphynxMessage
    {
        /// <summary>
        /// Processes the specified message and optionally invokes the <paramref name="next"/> middleware or handler in the pipeline.
        /// </summary>
        /// <param name="client">The client associated with the message.</param>
        /// <param name="message">The message to be processed.</param>
        /// <param name="next">The next middleware or handler in the pipeline</param>
        /// <param name="cancellationToken">A cancellation token for the pipeline.</param>
        Task InvokeAsync(ISphynxClient client, TMessage message, NextDelegate<TMessage> next, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// A component that can process messages of any type before (or after) the
    /// <see cref="IMessageHandler{TPacket}">handlers</see> in a <see cref="IMessageRouter">message pipeline</see>.
    /// </summary>
    public interface IMessageMiddleware : IMessageMiddleware<SphynxMessage>
    {
    }
}
