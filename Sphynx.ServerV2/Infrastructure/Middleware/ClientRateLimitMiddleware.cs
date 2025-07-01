// Copyright (c) Ark -α- & Specyy.Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Network.PacketV2;
using Sphynx.ServerV2.Client;
using Sphynx.ServerV2.Infrastructure.RateLimiting;
using Sphynx.Storage;

namespace Sphynx.ServerV2.Infrastructure.Middleware
{
    public sealed class ClientRateLimitMiddleware : IPacketMiddleware
    {
        private readonly Func<IRateLimiter> _rateLimiterFactory;
        private readonly MemoryCache<Guid, IRateLimiter> _rateLimiterCache = new();

        public event Func<RateLimitInfo, Task>? OnRateLimited;

        public ClientRateLimitMiddleware(Func<IRateLimiter> rateLimiterFactory)
        {
            _rateLimiterFactory = rateLimiterFactory;
        }

        public async Task InvokeAsync(ISphynxClient client, SphynxPacket packet, NextDelegate<SphynxPacket> next, CancellationToken token = default)
        {
            if (!_rateLimiterCache.TryGetItem(client.ClientId, out var rateLimiter))
            {
                rateLimiter = _rateLimiterCache.GetOrAdd(client.ClientId, ((id, factory) =>
                {
                    var limiter = _rateLimiterFactory();
                    return new MemoryCache<Guid, IRateLimiter>.CacheEntry(limiter, limiter.TimeWindow * 2);
                }), _rateLimiterFactory);
            }

            var timeLeft = await rateLimiter.ConsumeAsync(cancellationToken: token);

            if (timeLeft > TimeSpan.Zero)
            {
                if (OnRateLimited is not null)
                {
                    var rateLimitInfo = new RateLimitInfo { Client = client, Packet = packet, TimeLeft = timeLeft };
                    await OnRateLimited.Invoke(rateLimitInfo);
                }

                return;
            }

            await next(client, packet, token);
        }
    }

    public readonly struct RateLimitInfo
    {
        public ISphynxClient Client { get; init; }
        public SphynxPacket Packet { get; init; }
        public TimeSpan TimeLeft { get; init; }
    }
}
