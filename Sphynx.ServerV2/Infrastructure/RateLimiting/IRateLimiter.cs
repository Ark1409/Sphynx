// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Sphynx.ServerV2.Infrastructure.RateLimiting
{
    public interface IRateLimiter
    {
        int MaxPermits { get; }
        TimeSpan TimeWindow { get; }

        double RateSeconds { get; }

        ValueTask<TimeSpan> ConsumeAsync(int count = 1, CancellationToken cancellationToken = default);
    }

    public static class RateLimiterExtensions
    {
        public static async ValueTask<bool> TryConsumeAsync(this IRateLimiter limiter, int count = 1, CancellationToken cancellationToken = default)
        {
            return await limiter.ConsumeAsync(count, cancellationToken) > TimeSpan.Zero;
        }
    }
}
