// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework.Internal;
using NUnit.Framework.Legacy;
using Sphynx.Core;
using Sphynx.Network.Serialization;

namespace Sphynx.Test.Serialization
{
    [TestFixture]
    public class BinarySerializerTests
    {
        private static readonly Randomizer _randomizer = new Randomizer();

        #region Dictionaries

        [Test]
        public void BinarySerializer_ShouldSerializePrimitiveStringDictionary([Random(0, 50, 1)] int size)
        {
            // Arrange
            var value = new Dictionary<DateTime, string?>(size);

            for (int i = 0; i < size; i++)
                value.Add(DateTime.Now, _randomizer.GetString());

            Span<byte> buffer = stackalloc byte[BinarySerializer.MaxSizeOf(value)];
            var serializer = new BinarySerializer(buffer);
            var deserializer = new BinaryDeserializer(buffer);

            // Act
            bool serialized = serializer.TryWriteDictionary(value);

            // Assert
            Assert.That(serialized, "Could not perform serialization.");
            Assert.That(serializer.Offset, Is.EqualTo(BinarySerializer.SizeOf(value)));
            Assert.That(deserializer.TryReadDictionary(out Dictionary<DateTime, string>? readValue),
                "Could not perform deserialization.");
            Assert.That(deserializer.Offset, Is.EqualTo(BinarySerializer.SizeOf(value)));

            Assert.That(readValue!.Count, Is.EqualTo(value.Count));
            foreach (var kvp in value)
            {
                Assert.That(readValue.ContainsKey(kvp.Key));
                Assert.That(readValue.ContainsValue(kvp.Value ?? string.Empty));
            }
        }

        [Test]
        public void BinarySerializer_ShouldSerializeStringPrimitiveDictionary([Random(0, 50, 1)] int size)
        {
            // Arrange
            var value = new Dictionary<string, SnowflakeId>(size);

            for (int i = 0; i < size; i++)
                value.Add(_randomizer.GetString(), SnowflakeId.NewId());

            Span<byte> buffer = stackalloc byte[BinarySerializer.MaxSizeOf(value)];
            var serializer = new BinarySerializer(buffer);
            var deserializer = new BinaryDeserializer(buffer);

            // Act
            bool serialized = serializer.TryWriteDictionary(value);

            // Assert
            Assert.That(serialized, "Could not perform serialization.");
            Assert.That(serializer.Offset, Is.EqualTo(BinarySerializer.SizeOf(value)));
            Assert.That(deserializer.TryReadDictionary(out Dictionary<string, SnowflakeId>? readValue),
                "Could not perform deserialization.");
            Assert.That(deserializer.Offset, Is.EqualTo(BinarySerializer.SizeOf(value)));

            CollectionAssert.AreEqual(value, readValue!);
        }

        [Test]
        public void BinarySerializer_ShouldSerializeStringDictionary([Random(0, 50, 1)] int size)
        {
            // Arrange
            var value = new Dictionary<string, string?>(size);

            for (int i = 0; i < size; i++)
                value.Add(_randomizer.GetString(), _randomizer.NextFloat(1) > 0.1 ? _randomizer.GetString() : null);

            Span<byte> buffer = stackalloc byte[BinarySerializer.MaxSizeOf(value)];
            var serializer = new BinarySerializer(buffer);
            var deserializer = new BinaryDeserializer(buffer);

            // Act
            bool serialized = serializer.TryWriteDictionary(value);

            // Assert
            Assert.That(serialized, "Could not perform serialization.");
            Assert.That(serializer.Offset, Is.EqualTo(BinarySerializer.SizeOf(value)));
            Assert.That(deserializer.TryReadDictionary(out var readValue),
                "Could not perform deserialization.");
            Assert.That(deserializer.Offset, Is.EqualTo(BinarySerializer.SizeOf(value)));

            Assert.That(readValue!.Count, Is.EqualTo(value.Count));
            foreach (var kvp in value)
            {
                Assert.That(readValue.ContainsKey(kvp.Key));
                Assert.That(readValue.ContainsValue(kvp.Value ?? string.Empty));
            }
        }

        [Test]
        public void BinarySerializer_ShouldSerializePrimitiveDictionary([Random(0, 50, 1)] int size)
        {
            // Arrange
            var value = new Dictionary<int, double>(size);

            for (int i = 0; i < size; i++)
                value.Add(_randomizer.Next(), _randomizer.NextDouble());

            Span<byte> buffer = stackalloc byte[BinarySerializer.MaxSizeOf(value)];
            var serializer = new BinarySerializer(buffer);
            var deserializer = new BinaryDeserializer(buffer);

            // Act
            bool serialized = serializer.TryWriteDictionary(value);

            // Assert
            Assert.That(serialized, "Could not perform serialization.");
            Assert.That(serializer.Offset, Is.EqualTo(BinarySerializer.SizeOf(value)));
            Assert.That(deserializer.TryReadDictionary<int, double>(out var readValue),
                "Could not perform deserialization.");
            Assert.That(deserializer.Offset, Is.EqualTo(BinarySerializer.SizeOf(value)));

            CollectionAssert.AreEqual(value, readValue!);
        }

        #endregion

        #region Collections

        [Test]
        public void BinarySerializer_ShouldSerializeStringSet([Random(0, 50, 5)] int size)
        {
            // Arrange
            var value = new HashSet<string?>(size);

            for (int i = 0; i < size; i++)
                value.Add(_randomizer.NextFloat(1) > 0.1 ? _randomizer.GetString() : null);

            Span<byte> buffer = stackalloc byte[BinarySerializer.MaxSizeOf(value)];
            var serializer = new BinarySerializer(buffer);
            var deserializer = new BinaryDeserializer(buffer);

            // Act
            bool serialized = serializer.TryWriteCollection(value);

            // Assert
            Assert.That(serialized, "Could not perform serialization.");
            Assert.That(serializer.Offset, Is.EqualTo(BinarySerializer.SizeOf(value)));
            Assert.That(deserializer.TryReadCollection<HashSet<string>>(out var readValue),
                "Could not perform deserialization.");
            Assert.That(deserializer.Offset, Is.EqualTo(BinarySerializer.SizeOf(value)));

            CollectionAssert.AreEqual(value.Select(x => x ?? string.Empty), readValue!);
        }

        [Test]
        public void BinarySerializer_ShouldSerializeStringList([Random(0, 50, 5)] int size)
        {
            // Arrange
            var value = new List<string?>(size);

            for (int i = 0; i < size; i++)
                value.Add(_randomizer.NextFloat(1) > 0.1 ? _randomizer.GetString() : null);

            Span<byte> buffer = stackalloc byte[BinarySerializer.MaxSizeOf(value)];
            var serializer = new BinarySerializer(buffer);
            var deserializer = new BinaryDeserializer(buffer);

            // Act
            bool serialized = serializer.TryWriteCollection(value);

            // Assert
            Assert.That(serialized, "Could not perform serialization.");
            Assert.That(serializer.Offset, Is.EqualTo(BinarySerializer.SizeOf(value)));
            Assert.That(deserializer.TryReadStringList(out var readValue),
                "Could not perform deserialization.");
            Assert.That(deserializer.Offset, Is.EqualTo(BinarySerializer.SizeOf(value)));

            CollectionAssert.AreEqual(value.Select(x => x ?? string.Empty), readValue!);
        }

        [Test]
        public void BinarySerializer_ShouldSerializePrimitiveSet([Random(0, 50, 5)] int size)
        {
            // Arrange
            var value = new HashSet<int>(size);

            for (int i = 0; i < size; i++)
                value.Add(_randomizer.Next());

            Span<byte> buffer = stackalloc byte[BinarySerializer.MaxSizeOf(value)];
            var serializer = new BinarySerializer(buffer);
            var deserializer = new BinaryDeserializer(buffer);

            // Act
            bool serialized = serializer.TryWriteCollection(value);

            // Assert
            Assert.That(serialized, "Could not perform serialization.");
            Assert.That(serializer.Offset, Is.EqualTo(BinarySerializer.SizeOf(value)));
            Assert.That(deserializer.TryReadCollection<int, HashSet<int>>(out var readValue),
                "Could not perform deserialization.");
            Assert.That(deserializer.Offset, Is.EqualTo(BinarySerializer.SizeOf(value)));

            CollectionAssert.AreEqual(value, readValue!);
        }

        [Test]
        public void BinarySerializer_ShouldSerializePrimitiveList([Random(0, 50, 5)] int size)
        {
            // Arrange
            var value = new List<int>(size);

            for (int i = 0; i < size; i++)
                value.Add(_randomizer.Next());

            Span<byte> buffer = stackalloc byte[BinarySerializer.MaxSizeOf(value)];
            var serializer = new BinarySerializer(buffer);
            var deserializer = new BinaryDeserializer(buffer);

            // Act
            bool serialized = serializer.TryWriteCollection(value);

            // Assert
            Assert.That(serialized, "Could not perform serialization.");
            Assert.That(serializer.Offset, Is.EqualTo(BinarySerializer.SizeOf(value)));
            Assert.That(deserializer.TryReadList<int>(out var readValue),
                "Could not perform deserialization.");
            Assert.That(deserializer.Offset, Is.EqualTo(BinarySerializer.SizeOf(value)));

            CollectionAssert.AreEqual(value, readValue!);
        }

        #endregion

        #region Common Types

        [Test]
        public void BinarySerializer_ShouldSerializeSnowflakeId()
        {
            // Arrange
            var value = SnowflakeId.NewId();
            Span<byte> buffer = stackalloc byte[BinarySerializer.MaxSizeOf<SnowflakeId>()];
            var serializer = new BinarySerializer(buffer);
            var deserializer = new BinaryDeserializer(buffer);

            // Act
            bool serialized = serializer.TryWriteSnowflakeId(value);

            // Assert
            Assert.That(serialized, "Could not perform serialization.");
            Assert.That(serializer.Offset, Is.EqualTo(BinarySerializer.SizeOf<SnowflakeId>()));
            Assert.That(deserializer.TryReadSnowflakeId(out var readValue), "Could not perform deserialization.");
            Assert.That(deserializer.Offset, Is.EqualTo(BinarySerializer.SizeOf<SnowflakeId>()));

            Assert.That(value, Is.EqualTo(readValue!.Value));
        }

        [Test]
        public void BinarySerializer_ShouldSerializeGuid()
        {
            // Arrange
            var value = Guid.NewGuid();
            Span<byte> buffer = stackalloc byte[BinarySerializer.MaxSizeOf<Guid>()];
            var serializer = new BinarySerializer(buffer);
            var deserializer = new BinaryDeserializer(buffer);

            // Act
            bool serialized = serializer.TryWriteGuid(value);

            // Assert
            Assert.That(serialized, "Could not perform serialization.");
            Assert.That(BinarySerializer.SizeOf<Guid>(), Is.EqualTo(serializer.Offset));
            Assert.That(deserializer.TryReadGuid(out var readValue), "Could not perform deserialization.");
            Assert.That(BinarySerializer.SizeOf<Guid>(), Is.EqualTo(deserializer.Offset));

            Assert.That(value, Is.EqualTo(readValue!.Value));
        }

        [Test]
        public void BinarySerializer_ShouldSerializeDateTime()
        {
            // Arrange
            var value = DateTime.UtcNow;
            Span<byte> buffer = stackalloc byte[BinarySerializer.MaxSizeOf<DateTime>()];
            var serializer = new BinarySerializer(buffer);
            var deserializer = new BinaryDeserializer(buffer);

            // Act
            bool serialized = serializer.TryWriteDateTime(value);

            // Assert
            Assert.That(serialized, "Could not perform serialization.");
            Assert.That(BinarySerializer.SizeOf<DateTime>(), Is.EqualTo(serializer.Offset));
            Assert.That(deserializer.TryReadDateTime(out var readValue), "Could not perform deserialization.");
            Assert.That(BinarySerializer.SizeOf<DateTime>(), Is.EqualTo(deserializer.Offset));

            Assert.That(value, Is.EqualTo(readValue!.Value));
        }

        [TestCase(null), TestCase("")]
        [TestCase(""), TestCase("")]
        [TestCase(""), TestCase("")]
        public void BinarySerializer_ShouldSerializeString(string? value)
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[BinarySerializer.MaxSizeOf(value =
                (value == "" ? _randomizer.GetString() : value))];
            var serializer = new BinarySerializer(buffer);
            var deserializer = new BinaryDeserializer(buffer);

            // Act
            bool serialized = serializer.TryWriteString(value);

            // Assert
            Assert.That(serialized, "Could not perform serialization.");
            Assert.That(BinarySerializer.SizeOf(value), Is.EqualTo(serializer.Offset));
            Assert.That(deserializer.TryReadString(out string? readValue), "Could not perform deserialization.");
            Assert.That(BinarySerializer.SizeOf(value), Is.EqualTo(deserializer.Offset));

            if (value is null)
                Assert.That(readValue, Is.Empty);
            else
                Assert.That(value, Is.EqualTo(readValue));
        }

        #endregion

        #region Primitives

        private const int PRIMITIVE_COUNT = 5;

        [TestCase(true)]
        [TestCase(false)]
        public void BinarySerializer_ShouldSerializeBool(bool value)
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[BinarySerializer.MaxSizeOf<bool>()];
            var serializer = new BinarySerializer(buffer);
            var deserializer = new BinaryDeserializer(buffer);

            // Act
            bool serialized = serializer.TryWriteBool(value);

            // Assert
            Assert.That(serialized, "Could not perform serialization.");
            Assert.That(BinarySerializer.SizeOf<bool>(), Is.EqualTo(serializer.Offset));
            Assert.That(deserializer.TryReadBool(out bool? readValue), "Could not perform deserialization.");
            Assert.That(BinarySerializer.SizeOf<bool>(), Is.EqualTo(deserializer.Offset));

            Assert.That(value, Is.EqualTo(readValue!.Value));
        }

        [Test]
        public void BinarySerializer_ShouldSerializeByte(
            [Random(byte.MinValue, byte.MaxValue, PRIMITIVE_COUNT, Distinct = true)] byte value)
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[BinarySerializer.MaxSizeOf<byte>()];
            var serializer = new BinarySerializer(buffer);
            var deserializer = new BinaryDeserializer(buffer);

            // Act
            bool serialized = serializer.TryWriteByte(value);

            // Assert
            Assert.That(serialized, "Could not perform serialization.");
            Assert.That(BinarySerializer.SizeOf<byte>(), Is.EqualTo(serializer.Offset));
            Assert.That(deserializer.TryReadByte(out byte? readValue), "Could not perform deserialization.");
            Assert.That(BinarySerializer.SizeOf<byte>(), Is.EqualTo(deserializer.Offset));

            Assert.That(value, Is.EqualTo(readValue!.Value));
        }

        [Test]
        public void BinarySerializer_ShouldSerializeInt16(
            [Random(short.MinValue, short.MaxValue, PRIMITIVE_COUNT, Distinct = true)] short value)
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[BinarySerializer.MaxSizeOf<short>()];
            var serializer = new BinarySerializer(buffer);
            var deserializer = new BinaryDeserializer(buffer);

            // Act
            bool serialized = serializer.TryWriteInt16(value);

            // Assert
            Assert.That(serialized, "Could not perform serialization.");
            Assert.That(BinarySerializer.SizeOf<short>(), Is.EqualTo(serializer.Offset));
            Assert.That(deserializer.TryReadInt16(out short? readValue), "Could not perform deserialization.");
            Assert.That(BinarySerializer.SizeOf<short>(), Is.EqualTo(deserializer.Offset));

            Assert.That(value, Is.EqualTo(readValue!.Value));
        }

        [Test]
        public void BinarySerializer_ShouldSerializeUInt16(
            [Random(ushort.MinValue, ushort.MaxValue, PRIMITIVE_COUNT, Distinct = true)] ushort value)
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[BinarySerializer.MaxSizeOf<ushort>()];
            var serializer = new BinarySerializer(buffer);
            var deserializer = new BinaryDeserializer(buffer);

            // Act
            bool serialized = serializer.TryWriteUInt16(value);

            // Assert
            Assert.That(serialized, "Could not perform serialization.");
            Assert.That(BinarySerializer.SizeOf<ushort>(), Is.EqualTo(serializer.Offset));
            Assert.That(deserializer.TryReadUInt16(out ushort? readValue), "Could not perform deserialization.");
            Assert.That(BinarySerializer.SizeOf<ushort>(), Is.EqualTo(deserializer.Offset));

            Assert.That(value, Is.EqualTo(readValue!.Value));
        }

        [Test]
        public void BinarySerializer_ShouldSerializeInt32(
            [Random(int.MinValue, int.MaxValue, PRIMITIVE_COUNT, Distinct = true)] int value)
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[BinarySerializer.MaxSizeOf<int>()];
            var serializer = new BinarySerializer(buffer);
            var deserializer = new BinaryDeserializer(buffer);

            // Act
            bool serialized = serializer.TryWriteInt32(value);

            // Assert
            Assert.That(serialized, "Could not perform serialization.");
            Assert.That(BinarySerializer.SizeOf<int>(), Is.EqualTo(serializer.Offset));
            Assert.That(deserializer.TryReadInt32(out int? readValue), "Could not perform deserialization.");
            Assert.That(BinarySerializer.SizeOf<int>(), Is.EqualTo(deserializer.Offset));

            Assert.That(value, Is.EqualTo(readValue!.Value));
        }

        [Test]
        public void BinarySerializer_ShouldSerializeUInt32(
            [Random(uint.MinValue, uint.MaxValue, PRIMITIVE_COUNT, Distinct = true)] uint value)
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[BinarySerializer.MaxSizeOf<uint>()];
            var serializer = new BinarySerializer(buffer);
            var deserializer = new BinaryDeserializer(buffer);

            // Act
            bool serialized = serializer.TryWriteUInt32(value);

            // Assert
            Assert.That(serialized, "Could not perform serialization.");
            Assert.That(BinarySerializer.SizeOf<uint>(), Is.EqualTo(serializer.Offset));
            Assert.That(deserializer.TryReadUInt32(out uint? readValue), "Could not perform deserialization.");
            Assert.That(BinarySerializer.SizeOf<uint>(), Is.EqualTo(deserializer.Offset));

            Assert.That(value, Is.EqualTo(readValue!.Value));
        }

        [Test]
        public void BinarySerializer_ShouldSerializeInt64(
            [Random(long.MinValue, long.MaxValue, PRIMITIVE_COUNT, Distinct = true)] long value)
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[BinarySerializer.MaxSizeOf<long>()];
            var serializer = new BinarySerializer(buffer);
            var deserializer = new BinaryDeserializer(buffer);

            // Act
            bool serialized = serializer.TryWriteInt64(value);

            // Assert
            Assert.That(serialized, "Could not perform serialization.");
            Assert.That(BinarySerializer.SizeOf<long>(), Is.EqualTo(serializer.Offset));
            Assert.That(deserializer.TryReadInt64(out long? readValue), "Could not perform deserialization.");
            Assert.That(BinarySerializer.SizeOf<long>(), Is.EqualTo(deserializer.Offset));

            Assert.That(value, Is.EqualTo(readValue!.Value));
        }

        [Test]
        public void BinarySerializer_ShouldSerializeUInt64(
            [Random(ulong.MinValue, ulong.MaxValue, PRIMITIVE_COUNT, Distinct = true)] ulong value)
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[BinarySerializer.MaxSizeOf<ulong>()];
            var serializer = new BinarySerializer(buffer);
            var deserializer = new BinaryDeserializer(buffer);

            // Act
            bool serialized = serializer.TryWriteUInt64(value);

            // Assert
            Assert.That(serialized, "Could not perform serialization.");
            Assert.That(BinarySerializer.SizeOf<ulong>(), Is.EqualTo(serializer.Offset));
            Assert.That(deserializer.TryReadUInt64(out ulong? readValue), "Could not perform deserialization.");
            Assert.That(BinarySerializer.SizeOf<ulong>(), Is.EqualTo(deserializer.Offset));

            Assert.That(value, Is.EqualTo(readValue!.Value));
        }

        [Test]
        public void BinarySerializer_ShouldSerializeDouble(
            [Random(float.MinValue, float.MaxValue, PRIMITIVE_COUNT)] double value)
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[BinarySerializer.MaxSizeOf<double>()];
            var serializer = new BinarySerializer(buffer);
            var deserializer = new BinaryDeserializer(buffer);

            // Act
            bool serialized = serializer.TryWriteDouble(value);

            // Assert
            Assert.That(serialized, "Could not perform serialization.");
            Assert.That(BinarySerializer.SizeOf<double>(), Is.EqualTo(serializer.Offset));
            Assert.That(deserializer.TryReadDouble(out double? readValue), "Could not perform deserialization.");
            Assert.That(BinarySerializer.SizeOf<double>(), Is.EqualTo(deserializer.Offset));

            Assert.That(value, Is.EqualTo(readValue!.Value));
        }

        [Test]
        public void BinarySerializer_ShouldSerializeFloat(
            [Random(float.MinValue, float.MaxValue, PRIMITIVE_COUNT, Distinct = true)] float value)
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[BinarySerializer.MaxSizeOf<float>()];
            var serializer = new BinarySerializer(buffer);
            var deserializer = new BinaryDeserializer(buffer);

            // Act
            bool serialized = serializer.TryWriteFloat(value);

            // Assert
            Assert.That(serialized, "Could not perform serialization.");
            Assert.That(BinarySerializer.SizeOf<float>(), Is.EqualTo(serializer.Offset));
            Assert.That(deserializer.TryReadFloat(out float? readValue), "Could not perform deserialization.");
            Assert.That(BinarySerializer.SizeOf<float>(), Is.EqualTo(deserializer.Offset));

            Assert.That(value, Is.EqualTo(readValue!.Value));
        }

        #endregion
    }
}
