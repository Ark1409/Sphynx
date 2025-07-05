// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Sphynx.ServerV2.Infrastructure.RateLimiting
{
    public interface IRateLimiter
    {
        int MaxPermits { get; }
        TimeSpan Period { get; }

        double RateSeconds { get; }

        ValueTask<TimeSpan> ConsumeAsync(int count = 1, CancellationToken cancellationToken = default);
    }

    public static class RateLimiterExtensions
    {
        public static async ValueTask<bool> TryConsumeAsync(this IRateLimiter limiter, int count = 1, CancellationToken cancellationToken = default)
        {
            return await limiter.ConsumeAsync(count, cancellationToken) == TimeSpan.Zero;
        }

        public static bool TryConsume(this IRateLimiter limiter, int count = 1)
        {
            return Consume(limiter, count) == TimeSpan.Zero;
        }

        public static TimeSpan Consume(this IRateLimiter limiter, int count = 1)
        {
            var consumeTask = limiter.ConsumeAsync(count);

            if (!consumeTask.IsCompleted)
                consumeTask.AsTask().Wait();

            return consumeTask.Result;
        }
    }
}
