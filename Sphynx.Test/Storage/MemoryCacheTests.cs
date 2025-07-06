// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Storage;

namespace Sphynx.Test.Storage
{
    [TestFixture]
    public class MemoryCacheTests
    {
        [Test]
        public void TryAdd_ShouldAddAndExpireEntry_WhenQueried()
        {
            // Arrange
            using var cache = new MemoryCache<int, string>(TimeSpan.FromMinutes(1));
            Assert.That(cache.TryGetEntry(1, out _), Is.False);

            // Act
            bool added = cache.TryAdd(1, "1", TimeSpan.FromMilliseconds(250));

            // Assert
            Assert.That(added);
            Assert.That(cache.TryGetEntry(1, out var entry));

            Assert.That(entry.HasValue);
            Assert.That(entry.Value.Item == "1");
            Assert.That(entry.Value.Lifetime == TimeSpan.FromMilliseconds(250));

            Thread.Sleep(250);
            Assert.That(cache.TryGetEntry(1, out _), Is.False);
        }

        [Test]
        public void GetOrAdd_ShouldReturnExistingEntry_WhenAlreadyExists()
        {
            // Arrange
            using var cache = new MemoryCache<int, string>(TimeSpan.FromDays(365));

            Assert.That(cache.TryAdd(1, "1", TimeSpan.FromHours(1)));

            // Act
            cache.GetOrAdd<object?>(1, (_, _) => new MemoryCache<int, string>.CacheEntry("2", TimeSpan.FromDays(1)), null);

            // Assert
            Assert.That(cache.TryGetEntry(1, out var entry));
            Assert.That(entry.HasValue);
            Assert.That(entry.Value.Item, Is.EqualTo("1"));
            Assert.That(entry.Value.Lifetime, Is.EqualTo(TimeSpan.FromHours(1)));
        }

        [Test]
        public void TryExpire_ShouldRemoveEntry_WhenNotAlreadyExpired()
        {
            // Arrange
            using var cache = new MemoryCache<int, string>(TimeSpan.FromDays(1));

            Assert.That(cache.TryAdd(1, "1", TimeSpan.FromHours(1)));

            // Act
            cache.TryExpire(1, out string? item);

            // Assert
            Assert.That(item, Is.EqualTo("1"));
            Assert.That(cache.TryGetEntry(1, out var entry), Is.False);
            Assert.That(entry.HasValue, Is.False);
        }

        [Test]
        public void AddOrUpdate_ShouldUpdateEntry_WhenAlreadyExists()
        {
            // Arrange
            using var cache = new MemoryCache<int, string>(TimeSpan.FromDays(1));
            cache.AddOrUpdate(1, "1", TimeSpan.FromHours(1));

            Assert.That(cache.ContainsKey(1));

            // Act
            cache.AddOrUpdate<object?>(1, (_, _) => new MemoryCache<int, string>.CacheEntry("2", TimeSpan.Zero),
                (_, existing, _) => new MemoryCache<int, string>.CacheEntry("3", existing.Lifetime * 2),
                null);

            // Assert
            Assert.That(cache.TryGetEntry(1, out var entry));
            Assert.That(entry.HasValue);
            Assert.That(entry.Value.Item, Is.EqualTo("3"));
            Assert.That(entry.Value.Lifetime, Is.EqualTo(TimeSpan.FromHours(2)));
        }
    }
}
