// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using Sphynx.Storage;

namespace Sphynx.Test.Storage
{
    [TestFixture]
    public class FixedObjectPoolTests
    {
        [Test]
        public void TestObjectPoolAdd()
        {
            // Arrange
            var pool = new FixedObjectPool<TestObject>(16);
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

        private class TestObject
        {
            public int Number { get; init; }
            public string Text { get; init; } = string.Empty;
        }
    }
}
