// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Storage;

namespace Sphynx.Test.Storage
{
    [TestFixture]
    public class FixedObjectPoolTests
    {
        [Test]
        public void FixedObjectPool_ShouldAdd_WHenPoolEmpty()
        {
            // Arrange
            var pool = new FixedObjectPool<TestObject>(16, fastChecks: false);
            var obj = new TestObject
            {
                Number = 10,
                Text = "Test"
            };

            // Act
            bool returned = pool.Return(obj);
            bool took = pool.TryTake(out var pooledObj);

            // Assert
            Assert.That(returned, "Could not return object to pool");
            Assert.That(took, "Could not take object from pool");

            Assert.That(pooledObj, Is.EqualTo(obj));
            Assert.That(ReferenceEquals(pooledObj, obj));
        }

        [Test]
        public void FixedObjectPool_ShouldRemove_WhenPoolNotEmpty()
        {
            // Arrange
            const int POOL_SIZE = 16;
            var pool = new FixedObjectPool<TestObject>(POOL_SIZE, fastChecks: false);
            var obj = new TestObject
            {
                Number = 10,
                Text = "Test"
            };

            // Act
            bool returned = true;

            for (int i = 0; i < POOL_SIZE; i++)
                returned &= pool.Return(obj);

            bool taken = true;

            for (int i = 0; i < POOL_SIZE; i++)
                taken &= pool.TryTake(out _);

            // Assert
            Assert.That(returned, "Could not return object to pool");
            Assert.That(taken, "Took object from empty pool?");
        }

        private class TestObject
        {
            public int Number { get; init; }
            public string Text { get; init; } = string.Empty;
        }
    }
}
