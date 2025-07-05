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
    /// <summary>
    /// The default packet router.
    /// </summary>
    public class PacketRouter : IPacketRouter
    {
        /// <summary>
        /// Whether to throw an exception when attempting to <see cref="ExecuteAsync"/> a packet with an
        /// unregistered handler.
        /// </summary>
        public bool ThrowOnUnregistered { get; set; } = true;

        private readonly Dictionary<Type, NonGenericPacketPipeline> _pipelines = new();

        /// <summary>
        /// Applies middleware to the handlers of type <typeparamref name="TPacket"/> and all its children.
        /// </summary>
        /// <param name="middleware">The middleware to apply.</param>
        /// <typeparam name="TPacket">The packet type.</typeparam>
        public PacketRouter UseMiddleware<TPacket>(IPacketMiddleware<TPacket> middleware) where TPacket : SphynxPacket
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

        IPacketRouter IPacketRouter.UseHandler<TPacket>(IPacketHandler<TPacket> handler) => UseHandler(handler);

        /// <inheritdoc cref="UseHandler{TPacket}(Sphynx.ServerV2.Infrastructure.Handlers.IPacketHandler{TPacket})"/>
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

        /// <inheritdoc/>
        public Task ExecuteAsync(ISphynxClient client, SphynxPacket packet, CancellationToken cancellationToken = default)
        {
            if (!TryGetPipeline(packet.GetType(), out var pipeline, true))
                return ThrowOnUnregistered
                    ? Task.FromException(new ArgumentException($"No existing pipeline for packet {packet.PacketType}"))
                    : Task.CompletedTask;

            return pipeline.ExecuteAsync(client, packet, cancellationToken);
        }

        private void AddParentMiddleware(NonGenericPacketPipeline pipeline)
        {
            if (!TryGetPipeline(pipeline.PacketType, out var parentPipeline, true))
                return;

            foreach (var parentMiddleware in parentPipeline.Middleware)
                pipeline.AddMiddleware(parentMiddleware);
        }

        private void UpdateChildMiddleware<TPacket>(IPacketMiddleware<TPacket> middleware) where TPacket : SphynxPacket
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

        /// <summary>
        /// Removes all registered middlewares and handlers from this router.
        /// </summary>
        public void RemoveAll()
        {
            _pipelines.Clear();
        }
    }
}
