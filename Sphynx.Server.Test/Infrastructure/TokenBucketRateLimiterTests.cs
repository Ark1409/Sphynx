// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.ServerV2.Infrastructure.RateLimiting;

namespace Sphynx.Server.Test.Infrastructure
{
    [TestFixture]
    public class TokenBucketRateLimiterTests
    {
        [Test]
        public void Rate_ShouldBeCorrect_OnInit()
        {
            // Arrange
            var rateLimiter = new TokenBucketRateLimiter(3, 1, TimeSpan.FromMinutes(1));

            // Act
            double rateTicks = rateLimiter.RateTicks;
            double rateSeconds = rateLimiter.RateSeconds;

            // Assert
            double expectedRateTicks = 3.0 / rateLimiter.Period.Ticks;
            double expectedRateSeconds = 3.0 / rateLimiter.Period.TotalSeconds;

            Assert.That(rateTicks, Is.EqualTo(expectedRateTicks).Within(0.001));
            Assert.That(rateSeconds, Is.EqualTo(expectedRateSeconds).Within(0.001));
        }

        [Test]
        public void TryConsume_ShouldReturnTrue_OnInit()
        {
            // Arrange
            var rateLimiter = new TokenBucketRateLimiter(1, 1);

            // Act
            bool consumed = rateLimiter.TryConsume();

            // Assert
            Assert.That(consumed);
        }

        [Test]
        public void TryConsume_ShouldRateLimit_WhenEmpty()
        {
            // Arrange
            var rateLimiter = new TokenBucketRateLimiter(10, 10, TimeSpan.FromDays(365));
            Assert.That(rateLimiter.TryConsume(10));

            // Act
            bool rateLimited = rateLimiter.TryConsume();

            // Assert
            Assert.That(rateLimited, Is.False);
        }

        [Test]
        public void Consume_ShouldReturnRateLimit_WhenEmpty()
        {
            // Arrange
            var rateLimiter = new TokenBucketRateLimiter(10, 10, TimeSpan.FromDays(365));
            Assert.That(rateLimiter.TryConsume(10));

            // Act
            var timeLeft = rateLimiter.Consume();

            // Assert
            var expectedTimeLeft = TimeSpan.FromDays(365);

            Assert.That(timeLeft.Ticks, Is.GreaterThan(0));
            Assert.That(timeLeft.TotalDays, Is.EqualTo(expectedTimeLeft.TotalDays).Within(0.1));
        }
    }
}
