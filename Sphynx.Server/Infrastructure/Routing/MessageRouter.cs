// Copyright (c) Ark -α- & Specyy.Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Sphynx.Network.Packet;
using Sphynx.Server.Client;
using Sphynx.Server.Infrastructure.Handlers;
using Sphynx.Server.Infrastructure.Middleware;

namespace Sphynx.Server.Infrastructure.Routing
{
    /// <summary>
    /// The default message router.
    /// </summary>
    public class MessageRouter : IMessageRouter
    {
        /// <summary>
        /// Whether to throw an exception when attempting to <see cref="ExecuteAsync"/> a message with an
        /// unregistered handler.
        /// </summary>
        public bool ThrowOnUnregistered { get; set; } = true;

        private readonly Dictionary<Type, SphynxMessagePipeline> _pipelines = new();

        /// <summary>
        /// Applies middleware to the handlers of type <typeparamref name="TMessage"/> and all its children.
        /// </summary>
        /// <param name="middleware">The middleware to apply.</param>
        /// <typeparam name="TMessage">The message type.</typeparam>
        public MessageRouter UseMiddleware<TMessage>(IMessageMiddleware<TMessage> middleware) where TMessage : SphynxMessage
        {
            ArgumentNullException.ThrowIfNull(middleware);

            var messageType = typeof(TMessage);

            if (!TryGetPipeline(messageType, out var pipeline))
            {
                pipeline = SphynxMessagePipeline.Create(messageType, TryGetPipeline(messageType, out var parentPipeline, true)
                    ? parentPipeline.Handler
                    : NullMessageHandler.Instance);

                AddParentMiddleware(pipeline);

                _pipelines[messageType] = pipeline;
            }

            pipeline.AddMiddleware(middleware);
            UpdateChildMiddleware(middleware);

            return this;
        }

        IMessageRouter IMessageRouter.UseHandler<TPacket>(IMessageHandler<TPacket> handler) => UseHandler(handler);

        /// <inheritdoc cref="IMessageRouter.UseHandler{TPacket}(IMessageHandler{TPacket})"/>
        public MessageRouter UseHandler<TMessage>(IMessageHandler<TMessage> handler) where TMessage : SphynxMessage
        {
            ArgumentNullException.ThrowIfNull(handler);

            var messageType = typeof(TMessage);

            if (!TryGetPipeline(messageType, out var pipeline))
            {
                pipeline = SphynxMessagePipeline.Create(handler);
                AddParentMiddleware(pipeline);

                _pipelines[messageType] = pipeline;
            }

            pipeline.SetHandler(handler);

            return this;
        }

        /// <inheritdoc/>
        public Task ExecuteAsync(ISphynxClient client, SphynxMessage message, CancellationToken cancellationToken = default)
        {
            if (!TryGetPipeline(message.GetType(), out var pipeline, true))
                return ThrowOnUnregistered
                    ? Task.FromException(new ArgumentException($"No existing pipeline for message {message.GetType()}"))
                    : Task.CompletedTask;

            return pipeline.ExecuteAsync(client, message, cancellationToken);
        }

        private void AddParentMiddleware(SphynxMessagePipeline pipeline)
        {
            if (!TryGetPipeline(pipeline.MessageType, out var parentPipeline, true))
                return;

            foreach (var parentMiddleware in parentPipeline.Middleware)
                pipeline.AddMiddleware(parentMiddleware);
        }

        private void UpdateChildMiddleware<TMessage>(IMessageMiddleware<TMessage> middleware) where TMessage : SphynxMessage
        {
            var messageType = typeof(TMessage);

            foreach (var (type, currentPipeline) in _pipelines)
            {
                if (type == messageType)
                    continue;

                if (type.IsSubclassOf(messageType))
                    currentPipeline.AddMiddleware(middleware);
            }
        }

        private bool TryGetPipeline(Type messageType, [NotNullWhen(true)] out SphynxMessagePipeline? pipeline, bool tryParent = false)
        {
            Debug.Assert(typeof(SphynxMessage).IsAssignableFrom(messageType), $"Attempted to get pipeline for a {messageType}?");

            if (_pipelines.Count == 0)
            {
                pipeline = null;
                return false;
            }

            while (true)
            {
                if (_pipelines.TryGetValue(messageType, out pipeline!))
                    return true;

                if (!tryParent)
                    return false;

                if (messageType == typeof(SphynxMessage))
                {
                    pipeline = null;
                    return false;
                }

                messageType = messageType.BaseType!;
            }
        }

        /// <summary>
        /// Removes all registered middlewares and handlers from this router.
        /// </summary>
        public void RemoveAll()
        {
            _pipelines.Clear();
        }
    }
}
