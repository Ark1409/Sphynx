// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Sphynx.ServerV2.Infrastructure
{
    public class TokenBucketRateLimiter : IRateLimiter
    {
        public int MaxOperations { get; }
        public TimeSpan TimeWindow { get; }

        public double RateSeconds => MaxOperations / TimeWindow.TotalSeconds;
        public double RateTicks => MaxOperations / (double)TimeWindow.Ticks;

        public int Tokens { get; private set; }

        private long _lastTime;
        private readonly SemaphoreSlim _semaphore = new(1);

        public TokenBucketRateLimiter(int tokensPerSecond) : this(tokensPerSecond, TimeSpan.FromSeconds(1))
        {
        }

        public TokenBucketRateLimiter(int maxTokens, TimeSpan timeWindow)
        {
            MaxOperations = maxTokens > 0 ? maxTokens : throw new ArgumentOutOfRangeException(nameof(maxTokens), "Max tokens must be positive");
            TimeWindow = timeWindow;
        }

        public async ValueTask<TimeSpan> ConsumeTokensAsync(int count = 1, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                long now = DateTimeOffset.UtcNow.Ticks;
                long elapsed = now - _lastTime;

                _lastTime = now;

                double newTokens = Math.Min(elapsed * RateTicks, MaxOperations - Tokens);
                Tokens += (int)newTokens;

                if (Tokens < count)
                {
                    double fracNewTokens = newTokens - (long)newTokens;
                    double tokensRequired = count - (Tokens + fracNewTokens);
                    long ticksLeft = (long)(tokensRequired * 1 / RateTicks);

                    return TimeSpan.FromTicks(ticksLeft);
                }

                Tokens -= count;

                return TimeSpan.Zero;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Dispose()
        {
            _semaphore.Dispose();
        }
    }
}
