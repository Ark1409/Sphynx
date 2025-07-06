// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Sphynx.ServerV2.Infrastructure.RateLimiting
{
    /// <summary>
    /// A rate limiter which limits clients based on permits.
    /// </summary>
    public interface IRateLimiter
    {
        /// <summary>
        /// The maximum number of permits.
        /// </summary>
        int MaxPermits { get; }

        /// <summary>
        /// The period after which permits are replenished.
        /// </summary>
        TimeSpan Period { get; }

        /// <summary>
        /// Average permit replenishment rate in seconds.
        /// </summary>
        double RateSeconds { get; }

        /// <summary>
        /// Attempts to consume <paramref name="count"/> permits. If insufficient permits are available,
        /// returns the estimated wait time required to obtain them.
        /// </summary>
        /// <param name="count">The number of permits to consume.</param>
        /// <param name="cancellationToken">A cancellation token for the consume operation.</param>
        /// <returns>
        /// <ul>
        ///     <li>If <paramref name="count"/> permits are available, <see cref="TimeSpan.Zero"/>.</li>
        ///     <li>Else if <paramref name="count"/> is positive and less than or equal to <see cref="MaxPermits"/>,
        ///         the estimated wait time to obtain <paramref name="count"/> permits, assuming no other consumptions occur.</li>
        ///     <li>Else if <paramref name="count"/> exceeds <see cref="MaxPermits"/>, <see cref="Timeout.InfiniteTimeSpan"/>.</li>
        /// </ul>
        /// </returns>
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
