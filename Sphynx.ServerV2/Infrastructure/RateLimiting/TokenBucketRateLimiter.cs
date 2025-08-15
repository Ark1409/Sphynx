// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;

namespace Sphynx.ServerV2.Infrastructure.RateLimiting
{
    public class TokenBucketRateLimiter : IRateLimiter
    {
        public int MaxPermits => MaxTokens;
        public int MaxTokens { get; }
        public TimeSpan Period { get; }

        public double RateSeconds => _tokensPerPeriod / Period.TotalSeconds;
        public double RateTicks => _tokensPerPeriod / (double)Period.Ticks;

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
        private readonly int _tokensPerPeriod;

        private long _lastTime;
        private long _accumTime;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public TokenBucketRateLimiter(int tokensPerSecond, int maxTokens) : this(tokensPerSecond, maxTokens, TimeSpan.FromSeconds(1))
        {
        }

        public TokenBucketRateLimiter(int tokensPerPeriod, int maxTokens, TimeSpan period)
        {
            if (tokensPerPeriod < 0)
                throw new ArgumentOutOfRangeException(nameof(tokensPerPeriod), "Tokens per period must be positive");

            if (maxTokens < 0)
                throw new ArgumentOutOfRangeException(nameof(maxTokens), "Max tokens must be positive");

            if (period <= TimeSpan.Zero)
                throw new ArgumentException("Period must be greater than zero", nameof(period));

            _tokensPerPeriod = tokensPerPeriod;
            _tokens = maxTokens;
            _lastTime = DateTimeOffset.UtcNow.Ticks;

            MaxTokens = maxTokens;
            Period = period;
        }

        /// <inheritdoc/>
        public ValueTask<TimeSpan> ConsumeAsync(int count = 1, CancellationToken cancellationToken = default)
        {
            if (count < 0)
                return ValueTask.FromException<TimeSpan>(new ArgumentException("Count must be greater or equal to 0", nameof(count)));

            if (count == 0)
                return ValueTask.FromResult(TimeSpan.Zero);

            if (count > MaxTokens || (_tokensPerPeriod == 0 && _tokens < count))
                return ValueTask.FromResult(TimeSpan.MaxValue);

            return ConsumeInternalAsync(count, cancellationToken);
        }

        private async ValueTask<TimeSpan> ConsumeInternalAsync(int count, CancellationToken cancellationToken)
        {
            Debug.Assert(count >= 0 && count <= MaxTokens);

            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                ReplenishTokens(out _);

                if (_tokens < count)
                {
                    int tokensRequired = count - _tokens;
                    long ticksRequired = (long)(Period.Ticks * Math.Ceiling((double)tokensRequired / _tokensPerPeriod) - _accumTime);

                    return TimeSpan.FromTicks(ticksRequired);
                }

                cancellationToken.ThrowIfCancellationRequested();

                _tokens -= count;

                return TimeSpan.Zero;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private int ReplenishTokens(out int newTokens)
        {
            Debug.Assert(_semaphore.CurrentCount == 0, "Semaphore should be held before replenishing tokens");

            if (_tokens < MaxTokens)
            {
                long now = DateTimeOffset.UtcNow.Ticks;
                long elapsed = now - _lastTime;

                _accumTime += elapsed;
                _lastTime = now;

                long periods = _accumTime / Period.Ticks;

                try
                {
                    newTokens = (int)Math.Min(checked(periods * _tokensPerPeriod), MaxTokens - _tokens);
                }
                catch (OverflowException)
                {
                    newTokens = MaxTokens - _tokens;
                }

                _tokens += newTokens;
                _accumTime -= periods * Period.Ticks;
            }
            else
            {
                newTokens = 0;
            }

            return _tokens;
        }
    }
}
