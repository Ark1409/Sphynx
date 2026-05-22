// Copyright (c) Ark -α- & Specyy.Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.CompilerServices;
using Sphynx.Network.Packet;
using Sphynx.Server.Client;
using Sphynx.Server.Infrastructure.Handlers;
using Sphynx.Server.Infrastructure.Middleware;
using ISphynxMessageHandler = Sphynx.Server.Infrastructure.Handlers.IMessageHandler<Sphynx.Network.Packet.SphynxMessage>;
using ISphynxMessageMiddleware = Sphynx.Server.Infrastructure.Middleware.IMessageMiddleware<Sphynx.Network.Packet.SphynxMessage>;

namespace Sphynx.Server.Infrastructure.Routing
{
    public class SphynxMessagePipeline
    {
        public ISphynxMessageHandler Handler { get; private set; }
        public IEnumerable<ISphynxMessageMiddleware> Middleware => _middleware ?? Enumerable.Empty<ISphynxMessageMiddleware>();

        public Type MessageType { get; }

        private NextDelegate<SphynxMessage> Pipeline => _pipelineCache ??= BuildPipeline();
        private NextDelegate<SphynxMessage>? _pipelineCache;

        private List<ISphynxMessageMiddleware>? _middleware;

        private SphynxMessagePipeline(Type messageType, ISphynxMessageHandler handler)
        {
            MessageType = messageType;
            Handler = handler;
        }

        public static SphynxMessagePipeline Create(Type messageType, ISphynxMessageHandler handler)
        {
            ArgumentNullException.ThrowIfNull(messageType);
            ArgumentNullException.ThrowIfNull(handler);

            if (!typeof(SphynxMessage).IsAssignableFrom(messageType))
                throw new ArgumentException($"Message type {messageType} does not derive from {typeof(SphynxMessage)}", nameof(messageType));

            return new SphynxMessagePipeline(messageType, handler);
        }

        public static SphynxMessagePipeline Create<TPacket>(IMessageHandler<TPacket> handler) where TPacket : SphynxMessage
        {
            ArgumentNullException.ThrowIfNull(handler);

            var genericHandler = handler as ISphynxMessageHandler ?? new GenericHandlerAdapter<TPacket>(handler);
            return new SphynxMessagePipeline(typeof(TPacket), genericHandler);
        }

        public Task ExecuteAsync(ISphynxClient client, SphynxMessage message, CancellationToken token = default)
        {
            if (token.IsCancellationRequested)
                return Task.FromCanceled(token);

            return _middleware is null || _middleware.Count == 0
                ? Handler.HandleMessageAsync(client, message, token)
                : Pipeline(client, message, token);
        }

        public void AddMiddleware<TParent>(IMessageMiddleware<TParent> middleware) where TParent : SphynxMessage
        {
            ArgumentNullException.ThrowIfNull(middleware);

            if (!typeof(TParent).IsAssignableFrom(MessageType))
                throw new ArgumentException($"Cannot use middleware of {typeof(TParent)} on message {MessageType}", nameof(middleware));

            var sphynxPacketMiddleware = middleware as ISphynxMessageMiddleware ?? new GenericMiddlewareAdapter<TParent>(middleware);

            _middleware ??= new List<ISphynxMessageMiddleware>();
            _middleware.Add(sphynxPacketMiddleware);

            InvalidatePipeline();
        }

        public void SetHandler<TPacket>(IMessageHandler<TPacket> handler) where TPacket : SphynxMessage
        {
            ArgumentNullException.ThrowIfNull(handler);

            if (Handler == handler || (Handler is GenericHandlerAdapter<TPacket> generic && generic.InnerHandler == handler))
                return;

            if (MessageType != typeof(TPacket))
                throw new ArgumentException($"Cannot use handler of {typeof(TPacket)} on message {MessageType}", nameof(handler));

            if (Handler is GenericHandlerAdapter<TPacket> handlerAdapter)
                handlerAdapter.InnerHandler = handler;
            else
                Handler = handler as ISphynxMessageHandler ?? new GenericHandlerAdapter<TPacket>(handler);

            InvalidatePipeline();
        }

        private NextDelegate<SphynxMessage> BuildPipeline()
        {
            NextDelegate<SphynxMessage> pipeline = (client, message, token) => Handler.HandleMessageAsync(client, message, token);

            if (_middleware is not null)
            {
                for (int i = _middleware.Count - 1; i >= 0; i--)
                {
                    var next = pipeline;
                    var current = _middleware[i];

                    pipeline = (client, message, token) => current.InvokeAsync(client, message, next, token);
                }
            }

            return pipeline;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InvalidatePipeline() => _pipelineCache = null;

        private class GenericHandlerAdapter<TPacket> : ISphynxMessageHandler where TPacket : SphynxMessage
        {
            internal IMessageHandler<TPacket> InnerHandler { get; set; }

            public GenericHandlerAdapter(IMessageHandler<TPacket> innerHandler) => InnerHandler = innerHandler;

            public Task HandleMessageAsync(ISphynxClient client, SphynxMessage message, CancellationToken token) =>
                InnerHandler.HandleMessageAsync(client, (TPacket)message, token);
        }

        private class GenericMiddlewareAdapter<TPacket> : ISphynxMessageMiddleware where TPacket : SphynxMessage
        {
            internal IMessageMiddleware<TPacket> InnerMiddleware { get; set; }

            public GenericMiddlewareAdapter(IMessageMiddleware<TPacket> innerMiddleware) => InnerMiddleware = innerMiddleware;

            public Task InvokeAsync(ISphynxClient client, SphynxMessage message, NextDelegate<SphynxMessage> next, CancellationToken token) =>
                InnerMiddleware.InvokeAsync(client, (TPacket)message, next, token);
        }
    }
}
