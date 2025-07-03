// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;

namespace Sphynx.ServerV2.Infrastructure.RateLimiting
{
    public class TokenBucketRateLimiter : IRateLimiter
    {
        public int MaxPermits { get; }
        public TimeSpan TimeWindow { get; }

        public double RateSeconds => RateTicks * TimeSpan.TicksPerSecond;
        public double RateTicks { get; }

        public int Tokens
        {
            get
            {
                _semaphore.Wait();

                try
                {
                    return ReplenishTokens(out _);
                }
                finally
                {
                    _semaphore.Release();
                }
            }
        }

        private int _tokens;

        private long _lastTime;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public TokenBucketRateLimiter(int tokensPerSecond, int maxTokens) : this(tokensPerSecond, maxTokens, TimeSpan.FromSeconds(1))
        {
        }

        public TokenBucketRateLimiter(int tokensPerPeriod, int maxTokens, TimeSpan period)
        {
            MaxPermits = maxTokens > 0 ? maxTokens : throw new ArgumentOutOfRangeException(nameof(maxTokens), "Max tokens must be positive");
            TimeWindow = ((double)maxTokens / tokensPerPeriod) * period;
            RateTicks = tokensPerPeriod * period.Ticks;
        }

        public async ValueTask<TimeSpan> ConsumeAsync(int count = 1, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                ReplenishTokens(out double newTokens);

                if (_tokens < count)
                {
                    double fracNewTokens = newTokens - (long)newTokens;
                    double tokensRequired = count - (_tokens + fracNewTokens);
                    long ticksLeft = (long)(tokensRequired * 1 / RateTicks);

                    return TimeSpan.FromTicks(ticksLeft);
                }

                _tokens -= count;

                return TimeSpan.Zero;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private int ReplenishTokens(out double newTokens)
        {
            Debug.Assert(_semaphore.CurrentCount == 0, "Semaphore should be held before replenishing tokens");

            if (_tokens < MaxPermits)
            {
                long now = DateTimeOffset.UtcNow.Ticks;
                long elapsedTicks = now - _lastTime;

                _lastTime = now;

                newTokens = Math.Min(elapsedTicks * RateTicks, MaxPermits - _tokens);
                _tokens += (int)newTokens;
            }
            else
            {
                newTokens = 0;
            }

            return _tokens;
        }
    }
}
