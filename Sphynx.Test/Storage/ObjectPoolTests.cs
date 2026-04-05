// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Storage;

namespace Sphynx.Test.Storage
{
    [TestFixture]
    public class ObjectPoolTests
    {
        [Test]
        public void Return_ShouldAddItem_WhenPoolIsEmpty()
        {
            // Arrange
            const int POOL_SIZE = 16;
            var pool = new ObjectPool<TestObject>(POOL_SIZE);
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
        public void TryTake_ShouldRemoveItem_WhenPoolIsNotEmpty()
        {
            // Arrange
            const int POOL_SIZE = 16;
            var pool = new ObjectPool<TestObject>(POOL_SIZE);
            var obj = new TestObject
            {
                Number = 10,
                Text = "Test"
            };

            // Act
            pool.Return(obj);
            bool taken = pool.TryTake(out _);

            // Assert
            Assert.That(taken, "Took object from empty pool?");
            Assert.That(pool.TryTake(out _), Is.False);
        }

        [Test]
        public void Clear_ShouldRemoveAllItems_WhenPoolIsNotEmpty()
        {
            // Arrange
            const int POOL_SIZE = 16;
            var pool = new ObjectPool<TestObject>(POOL_SIZE);
            var obj = new TestObject
            {
                Number = 0,
                Text = "Test"
            };

            for (int i = 0; i < POOL_SIZE; i++)
                pool.Return(obj);

            // Act
            int clearCount = 0;
            pool.Clear(x => clearCount++);

            // Assert
            Assert.That(clearCount, Is.EqualTo(POOL_SIZE));
        }

        private class TestObject
        {
            public int Number { get; set; }
            public string Text { get; set; } = string.Empty;
        }
    }
}
