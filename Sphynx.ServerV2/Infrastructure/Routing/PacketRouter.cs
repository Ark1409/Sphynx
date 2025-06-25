// Copyright (c) Ark -α- & Specyy.Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Sphynx.Network.PacketV2;
using Sphynx.ServerV2.Client;
using Sphynx.ServerV2.Infrastructure.Handlers;
using Sphynx.ServerV2.Infrastructure.Middleware;

namespace Sphynx.ServerV2.Infrastructure.Routing
{
    public class PacketRouter
    {
        private readonly Dictionary<Type, NonGenericPacketPipeline> _pipelines = new();

        public PacketRouter UseMiddleware<TPacket>(IMiddleware<TPacket> middleware) where TPacket : SphynxPacket
        {
            ArgumentNullException.ThrowIfNull(middleware, nameof(middleware));

            var packetType = typeof(TPacket);

            if (!TryGetPipeline(packetType, out var pipeline))
            {
                pipeline = NonGenericPacketPipeline.Create(packetType, TryGetPipeline(packetType, out var parentPipeline, true)
                    ? parentPipeline.Handler
                    : NullPacketHandler.Instance);

                AddParentMiddleware(pipeline);

                _pipelines[packetType] = pipeline;
            }

            pipeline.AddMiddleware(middleware);
            UpdateChildMiddleware(middleware);

            return this;
        }

        public PacketRouter UseHandler<TPacket>(IPacketHandler<TPacket> handler) where TPacket : SphynxPacket
        {
            ArgumentNullException.ThrowIfNull(handler, nameof(handler));

            var packetType = typeof(TPacket);

            if (!TryGetPipeline(packetType, out var pipeline))
            {
                pipeline = NonGenericPacketPipeline.Create(handler);
                AddParentMiddleware(pipeline);

                _pipelines[packetType] = pipeline;
            }

            pipeline.SetHandler(handler);
            return this;
        }

        public Task ExecuteAsync<TPacket>(ISphynxClient client, TPacket packet, CancellationToken cancellationToken = default)
            where TPacket : SphynxPacket
        {
            if (!TryGetPipeline(typeof(TPacket), out var pipeline, true))
                throw new ArgumentException($"No existing pipeline for packet {packet.PacketType}");

            return pipeline.ExecuteAsync(client, packet, cancellationToken);
        }

        private void AddParentMiddleware(NonGenericPacketPipeline pipeline)
        {
            if (!TryGetPipeline(pipeline.PacketType, out var parentPipeline, true))
                return;

            foreach (var parentMiddleware in parentPipeline.Middleware)
                pipeline.AddMiddleware(parentMiddleware);
        }

        private void UpdateChildMiddleware<TPacket>(IMiddleware<TPacket> middleware) where TPacket : SphynxPacket
        {
            var packetType = typeof(TPacket);

            foreach (var (type, currentPipeline) in _pipelines)
            {
                if (type == packetType)
                    continue;

                if (type.IsSubclassOf(packetType))
                    currentPipeline.AddMiddleware(middleware);
            }
        }

        private bool TryGetPipeline(Type packetType, [NotNullWhen(true)] out NonGenericPacketPipeline? pipeline, bool tryParent = false)
        {
            Debug.Assert(typeof(SphynxPacket).IsAssignableFrom(packetType), "Attempted to get pipeline for non-packet type?");

            if (_pipelines.Count == 0)
            {
                pipeline = null;
                return false;
            }

            while (true)
            {
                if (_pipelines.TryGetValue(packetType, out pipeline!))
                    return true;

                if (!tryParent)
                    return false;

                if (packetType == typeof(SphynxPacket))
                {
                    pipeline = null;
                    return false;
                }

                packetType = packetType.BaseType!;
            }
        }
    }
}
