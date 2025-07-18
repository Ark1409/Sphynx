// Copyright (c) Ark -α- & Specyy.Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Network.PacketV2;
using Sphynx.ServerV2.Client;
using Sphynx.ServerV2.Infrastructure.RateLimiting;
using Sphynx.Storage;

namespace Sphynx.ServerV2.Infrastructure.Middleware
{
    public class RateLimitingMiddleware : RateLimitingMiddleware<Guid>
    {
        public RateLimitingMiddleware(Func<IRateLimiter> rateLimiterFactory) : base(rateLimiterFactory, client => client.ClientId)
        {
        }
    }

    public class RateLimitingMiddleware<TPartition> : IPacketMiddleware, IDisposable, IAsyncDisposable where TPartition : notnull
    {
        private readonly Func<IRateLimiter> _rateLimiterFactory;
        private readonly MemoryCache<TPartition, IRateLimiter> _rateLimiterCache = new();
        private readonly Func<ISphynxClient, TPartition> _clientPartitioner;

        public event Func<RateLimitInfo, Task>? OnRateLimited;

        public RateLimitingMiddleware(Func<IRateLimiter> rateLimiterFactory, Func<ISphynxClient, TPartition> clientPartitioner)
        {
            _rateLimiterFactory = rateLimiterFactory;
            _clientPartitioner = clientPartitioner;
        }

        public async Task InvokeAsync(ISphynxClient client, SphynxPacket packet, NextDelegate<SphynxPacket> next, CancellationToken token = default)
        {
            var partitionKey = _clientPartitioner(client);

            if (!_rateLimiterCache.TryGetItem(partitionKey, out var rateLimiter))
            {
                rateLimiter = _rateLimiterCache.GetOrAdd(partitionKey, (_, factory) =>
                {
                    var limiter = factory();
                    return new MemoryCache<TPartition, IRateLimiter>.CacheEntry(limiter, limiter.Period * 2);
                }, _rateLimiterFactory);
            }

            var waitTime = await rateLimiter.ConsumeAsync(cancellationToken: token).ConfigureAwait(false);

            if (waitTime > TimeSpan.Zero)
            {
                var rateLimitInfo = new RateLimitInfo { Partition = partitionKey, Client = client, Packet = packet, WaitTime = waitTime };

                await OnRateLimiting(rateLimitInfo).ConfigureAwait(false);

                if (OnRateLimited is not null)
                    await OnRateLimited.Invoke(rateLimitInfo).ConfigureAwait(false);

                return;
            }

            await next(client, packet, token).ConfigureAwait(false);
        }

        protected virtual Task OnRateLimiting(RateLimitInfo info)
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            OnRateLimited = null;
            _rateLimiterCache.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            OnRateLimited = null;
            return _rateLimiterCache.DisposeAsync();
        }

        public readonly struct RateLimitInfo
        {
            public TPartition Partition { get; init; }
            public ISphynxClient Client { get; init; }
            public SphynxPacket Packet { get; init; }
            public TimeSpan WaitTime { get; init; }
        }
    }
}
