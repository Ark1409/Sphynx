// Copyright (c) Ark -α- & Specyy.Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.CompilerServices;
using Sphynx.Network.PacketV2;
using Sphynx.ServerV2.Client;
using Sphynx.ServerV2.Infrastructure.Handlers;
using Sphynx.ServerV2.Infrastructure.Middleware;
using SphynxPacketHandler = Sphynx.ServerV2.Infrastructure.Handlers.IPacketHandler<Sphynx.Network.PacketV2.SphynxPacket>;
using SphynxPacketMiddleware = Sphynx.ServerV2.Infrastructure.Middleware.IPacketMiddleware<Sphynx.Network.PacketV2.SphynxPacket>;

namespace Sphynx.ServerV2.Infrastructure.Routing
{
    public class SphynxPacketPipeline
    {
        public SphynxPacketHandler Handler { get; private set; }
        public IEnumerable<SphynxPacketMiddleware> Middleware => _middleware ?? Enumerable.Empty<SphynxPacketMiddleware>();

        public Type PacketType { get; }

        private NextDelegate<SphynxPacket> Pipeline => _pipelineCache ??= BuildPipeline();
        private NextDelegate<SphynxPacket>? _pipelineCache;

        private List<SphynxPacketMiddleware>? _middleware;

        private SphynxPacketPipeline(Type packetType, SphynxPacketHandler handler)
        {
            PacketType = packetType;
            Handler = handler;
        }

        public static SphynxPacketPipeline Create(Type packetType, SphynxPacketHandler handler)
        {
            ArgumentNullException.ThrowIfNull(packetType);
            ArgumentNullException.ThrowIfNull(handler);

            if (!typeof(SphynxPacket).IsAssignableFrom(packetType))
                throw new ArgumentException($"Packet type {packetType} does not derive from {typeof(SphynxPacket)}", nameof(packetType));

            return new SphynxPacketPipeline(packetType, handler);
        }

        public static SphynxPacketPipeline Create<TPacket>(IPacketHandler<TPacket> handler) where TPacket : SphynxPacket
        {
            ArgumentNullException.ThrowIfNull(handler);

            var nonGenericHandler = handler as SphynxPacketHandler ?? new GenericHandlerAdapter<TPacket>(handler);
            return new SphynxPacketPipeline(typeof(TPacket), nonGenericHandler);
        }

        public Task ExecuteAsync(ISphynxClient client, SphynxPacket packet, CancellationToken token = default)
        {
            if (token.IsCancellationRequested)
                return Task.FromCanceled(token);

            return _middleware?.Count == 0 ? Handler.HandlePacketAsync(client, packet, token) : Pipeline(client, packet, token);
        }

        public void AddMiddleware<TParent>(IPacketMiddleware<TParent> middleware) where TParent : SphynxPacket
        {
            ArgumentNullException.ThrowIfNull(middleware);

            if (!typeof(TParent).IsAssignableFrom(PacketType))
                throw new ArgumentException($"Cannot use middleware of {typeof(TParent)} on packet {PacketType}", nameof(middleware));

            var sphynxPacketMiddleware = middleware as SphynxPacketMiddleware ?? new GenericMiddlewareAdapter<TParent>(middleware);

            _middleware ??= new List<SphynxPacketMiddleware>();
            _middleware.Add(sphynxPacketMiddleware);

            InvalidatePipeline();
        }

        public void SetHandler<TPacket>(IPacketHandler<TPacket> handler) where TPacket : SphynxPacket
        {
            ArgumentNullException.ThrowIfNull(handler);

            if (Handler == handler || (Handler is GenericHandlerAdapter<TPacket> nonGeneric && nonGeneric.InnerHandler == handler))
                return;

            if (PacketType != typeof(TPacket))
                throw new ArgumentException($"Cannot use handler of {typeof(TPacket)} on packet {PacketType}", nameof(handler));

            if (Handler is GenericHandlerAdapter<TPacket> handlerAdapter)
                handlerAdapter.InnerHandler = handler;
            else
                Handler = handler as SphynxPacketHandler ?? new GenericHandlerAdapter<TPacket>(handler);

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

        private class GenericHandlerAdapter<TPacket> : SphynxPacketHandler where TPacket : SphynxPacket
        {
            internal IPacketHandler<TPacket> InnerHandler { get; set; }

            public GenericHandlerAdapter(IPacketHandler<TPacket> innerHandler) => InnerHandler = innerHandler;

            public Task HandlePacketAsync(ISphynxClient client, SphynxPacket packet, CancellationToken token) =>
                InnerHandler.HandlePacketAsync(client, (TPacket)packet, token);
        }

        private class GenericMiddlewareAdapter<TPacket> : SphynxPacketMiddleware where TPacket : SphynxPacket
        {
            internal IPacketMiddleware<TPacket> InnerMiddleware { get; set; }

            public GenericMiddlewareAdapter(IPacketMiddleware<TPacket> innerMiddleware) => InnerMiddleware = innerMiddleware;

            public Task InvokeAsync(ISphynxClient client, SphynxPacket packet, NextDelegate<SphynxPacket> next, CancellationToken token) =>
                InnerMiddleware.InvokeAsync(client, (TPacket)packet, next, token);
        }
    }
}
