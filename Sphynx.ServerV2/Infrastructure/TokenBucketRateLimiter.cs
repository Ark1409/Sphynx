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

        public TokenBucketRateLimiter(int maxTokens, TimeSpan timeWindow)
        {
            MaxOperations = maxTokens;
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

                Tokens += (int)(elapsed * RateTicks);

                if (Tokens < count)
                {
                    int tokensRequired = Tokens - count;
                    long timeLeft = (long)(tokensRequired * 1 / RateTicks);

                    return TimeSpan.FromTicks(timeLeft);
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
