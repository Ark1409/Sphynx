// Copyright (c) Ark -α- & Specyy.Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.CompilerServices;
using Sphynx.Network.PacketV2;
using Sphynx.ServerV2.Client;
using Sphynx.ServerV2.Infrastructure.Handlers;
using Sphynx.ServerV2.Infrastructure.Middleware;
using NonGenericHandler = Sphynx.ServerV2.Infrastructure.Handlers.IPacketHandler<Sphynx.Network.PacketV2.SphynxPacket>;
using NonGenericMiddleware = Sphynx.ServerV2.Infrastructure.Middleware.IMiddleware<Sphynx.Network.PacketV2.SphynxPacket>;

namespace Sphynx.ServerV2.Infrastructure.Routing
{
    public class NonGenericPacketPipeline
    {
        public NonGenericHandler Handler { get; private set; }
        public IEnumerable<NonGenericMiddleware> Middleware => _middleware ?? Enumerable.Empty<NonGenericMiddleware>();

        public Type PacketType { get; }

        private NextDelegate<SphynxPacket> Pipeline => _pipelineCache ??= BuildPipeline();
        private NextDelegate<SphynxPacket>? _pipelineCache;

        private List<NonGenericMiddleware>? _middleware;

        private NonGenericPacketPipeline(Type packetType, NonGenericHandler handler)
        {
            PacketType = packetType;
            Handler = handler;
        }

        internal static NonGenericPacketPipeline Create(Type packetType, NonGenericHandler handler)
        {
            ArgumentNullException.ThrowIfNull(packetType, nameof(packetType));
            ArgumentNullException.ThrowIfNull(handler, nameof(handler));

            if (!typeof(SphynxPacket).IsAssignableFrom(packetType))
                throw new ArgumentException($"Packet type {packetType} does not derive from {typeof(SphynxPacket)}", nameof(packetType));

            return new NonGenericPacketPipeline(packetType, handler);
        }

        public static NonGenericPacketPipeline Create<TPacket>(IPacketHandler<TPacket> handler) where TPacket : SphynxPacket
        {
            ArgumentNullException.ThrowIfNull(handler, nameof(handler));

            var nonGenericHandler = handler as NonGenericHandler ?? new NonGenericHandlerAdapter<TPacket>(handler);
            return new NonGenericPacketPipeline(typeof(TPacket), nonGenericHandler);
        }

        public Task ExecuteAsync(ISphynxClient client, SphynxPacket packet, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return _middleware?.Count == 0 ? Handler.HandlePacketAsync(client, packet, token) : Pipeline(client, packet, token);
        }

        public void AddMiddleware<TParent>(IMiddleware<TParent> middleware) where TParent : SphynxPacket
        {
            ArgumentNullException.ThrowIfNull(middleware, nameof(middleware));

            if (!typeof(TParent).IsAssignableFrom(PacketType))
                throw new ArgumentException($"Cannot use middleware of {typeof(TParent)} on packet {PacketType}", nameof(middleware));

            var nonGenericMiddleware = middleware as NonGenericMiddleware ?? new NonGenericMiddlewareAdapter<TParent>(middleware);

            _middleware ??= new List<NonGenericMiddleware>();
            _middleware.Add(nonGenericMiddleware);

            InvalidatePipeline();
        }

        public void SetHandler<TPacket>(IPacketHandler<TPacket> handler) where TPacket : SphynxPacket
        {
            ArgumentNullException.ThrowIfNull(handler, nameof(handler));

            if (Handler == handler || (Handler is NonGenericHandlerAdapter<TPacket> nonGeneric && nonGeneric.InnerHandler == handler))
                return;

            if (PacketType != typeof(TPacket))
                throw new ArgumentException($"Cannot use handler of {typeof(TPacket)} on packet {PacketType}", nameof(handler));

            if (Handler is NonGenericHandlerAdapter<TPacket> nonGenericHandler)
                nonGenericHandler.InnerHandler = handler;
            else
                Handler = handler as NonGenericHandler ?? new NonGenericHandlerAdapter<TPacket>(handler);

            InvalidatePipeline();
        }

        private NextDelegate<SphynxPacket> BuildPipeline()
        {
            NextDelegate<SphynxPacket> pipeline = (client, packet, token) => Handler.HandlePacketAsync(client, packet, token);

            if (_middleware is not null)
            {
                for (int i = _middleware.Count - 1; i >= 0; i--)
                {
                    var next = pipeline;
                    var current = _middleware[i];

                    pipeline = (client, packet, token) => current.InvokeAsync(client, packet, next, token);
                }
            }

            return pipeline;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InvalidatePipeline() => _pipelineCache = null;

        private class NonGenericHandlerAdapter<TPacket> : NonGenericHandler where TPacket : SphynxPacket
        {
            internal IPacketHandler<TPacket> InnerHandler { get; set; }

            public NonGenericHandlerAdapter(IPacketHandler<TPacket> innerHandler) => InnerHandler = innerHandler;

            public Task HandlePacketAsync(ISphynxClient client, SphynxPacket packet, CancellationToken token) =>
                InnerHandler.HandlePacketAsync(client, (TPacket)packet, token);
        }

        private class NonGenericMiddlewareAdapter<TPacket> : NonGenericMiddleware where TPacket : SphynxPacket
        {
            internal IMiddleware<TPacket> InnerMiddleware { get; set; }

            public NonGenericMiddlewareAdapter(IMiddleware<TPacket> innerMiddleware) => InnerMiddleware = innerMiddleware;

            public Task InvokeAsync(ISphynxClient client, SphynxPacket packet, NextDelegate<SphynxPacket> next, CancellationToken token) =>
                InnerMiddleware.InvokeAsync(client, (TPacket)packet, next, token);
        }
    }
}
