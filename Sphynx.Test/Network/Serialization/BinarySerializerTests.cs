// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework.Legacy;
using Sphynx.Core;
using Sphynx.Network.Serialization;
using Sphynx.Test.Utils;

namespace Sphynx.Test.Network.Serialization
{
    [TestFixture]
    public class BinarySerializerTests
    {
        #region Dictionaries

        [Test]
        public void BinarySerializer_ShouldSerializePrimitiveStringDictionary()
        {
            // Arrange
            var value = new Dictionary<DateTimeOffset, string?>
            {
                { new DateTime(1990, 12, 25, 1, 9, 1, DateTimeKind.Utc), null },
                { new DateTime(1992, 12, 30, 2, 8, 12, DateTimeKind.Utc), "" },
                { new DateTime(2012, 04, 12, 3, 7, 13, DateTimeKind.Utc), "foo" },
                { new DateTime(5023, 11, 01, 4, 6, 14, DateTimeKind.Utc), "bar" },
                { new DateTime(1090, 4, 21, 5, 10, 15, DateTimeKind.Utc), "\ud846\ude38" },
            };

            Span<byte> buffer = stackalloc byte[BinarySerializer.MaxSizeOf(value)];
            var serializer = new BinarySerializer(buffer);
            var deserializer = new BinaryDeserializer(buffer);

            // Act
            bool serialized = serializer.TryWriteDictionary(value);

            // Assert
            Assert.That(serialized, "Could not perform serialization.");
            Assert.That(serializer.Offset, Is.EqualTo(BinarySerializer.SizeOf(value)));
            Assert.That(deserializer.TryReadDictionary(out Dictionary<DateTimeOffset, string>? readValue), "Could not perform deserialization.");
            Assert.That(deserializer.Offset, Is.EqualTo(BinarySerializer.SizeOf(value)));

            Assert.That(readValue!.Count, Is.EqualTo(value.Count));
            foreach (var kvp in value)
            {
                Assert.That(readValue.ContainsKey(kvp.Key));
                Assert.That(readValue.ContainsValue(kvp.Value ?? string.Empty));
            }
        }

        [Test]
        public void BinarySerializer_ShouldSerializeStringPrimitiveDictionary()
        {
            // Arrange
            var value = new Dictionary<string, SnowflakeId>
            {
                { "", "test".AsSnowflakeId() },
                { "foo", "test1".AsSnowflakeId() },
                { "bar", "test2".AsSnowflakeId() },
                { "baz", "test3".AsSnowflakeId() },
                { "\ud876\ude21", "test4".AsSnowflakeId() },
            };

            Span<byte> buffer = stackalloc byte[BinarySerializer.MaxSizeOf(value)];
            var serializer = new BinarySerializer(buffer);
            var deserializer = new BinaryDeserializer(buffer);

            // Act
            bool serialized = serializer.TryWriteDictionary(value);

            // Assert
            Assert.That(serialized, "Could not perform serialization.");
            Assert.That(serializer.Offset, Is.EqualTo(BinarySerializer.SizeOf(value)));
            Assert.That(deserializer.TryReadDictionary(out Dictionary<string, SnowflakeId>? readValue), "Could not perform deserialization.");
            Assert.That(deserializer.Offset, Is.EqualTo(BinarySerializer.SizeOf(value)));

            CollectionAssert.AreEqual(value, readValue!);
        }

        [Test]
        public void BinarySerializer_ShouldSerializeStringDictionary()
        {
            // Arrange
            var value = new Dictionary<string, string?>()
            {
                { "", null },
                { "foo", "" },
                { "bar", "é" },
                { "Sphynx", "ç-1234567890/*-+!@#$%^&*()" },
                { "凹", "The quick brown fox jumps over the lazy dog" },
            };

            Span<byte> buffer = stackalloc byte[BinarySerializer.MaxSizeOf(value)];
            var serializer = new BinarySerializer(buffer);
            var deserializer = new BinaryDeserializer(buffer);

            // Act
            bool serialized = serializer.TryWriteDictionary(value);

            // Assert
            Assert.That(serialized, "Could not perform serialization.");
            Assert.That(serializer.Offset, Is.EqualTo(BinarySerializer.SizeOf(value)));
            Assert.That(deserializer.TryReadDictionary(out var readValue), "Could not perform deserialization.");
            Assert.That(deserializer.Offset, Is.EqualTo(BinarySerializer.SizeOf(value)));

            Assert.That(readValue!.Count, Is.EqualTo(value.Count));
            foreach (var kvp in value)
            {
                Assert.That(readValue.ContainsKey(kvp.Key));
                Assert.That(readValue.ContainsValue(kvp.Value ?? string.Empty));
            }
        }

        [Test]
        public void BinarySerializer_ShouldSerializePrimitiveDictionary()
        {
            // Arrange
            var value = new Dictionary<int, double>
            {
                { 0, double.MinValue },
                { int.MinValue, double.MaxValue },
                { int.MaxValue, double.Epsilon },
                { int.MinValue / 2, double.NaN },
                { 638712, double.PositiveInfinity },
                { 1111, 12345.6789 },
            };

            Span<byte> buffer = stackalloc byte[BinarySerializer.MaxSizeOf(value)];
            var serializer = new BinarySerializer(buffer);
            var deserializer = new BinaryDeserializer(buffer);

            // Act
            bool serialized = serializer.TryWriteDictionary(value);

            // Assert
            Assert.That(serialized, "Could not perform serialization.");
            Assert.That(serializer.Offset, Is.EqualTo(BinarySerializer.SizeOf(value)));
            Assert.That(deserializer.TryReadDictionary<int, double>(out var readValue), "Could not perform deserialization.");
            Assert.That(deserializer.Offset, Is.EqualTo(BinarySerializer.SizeOf(value)));

            CollectionAssert.AreEqual(value, readValue!);
        }

        #endregion

        #region Collections

        [Test]
        public void BinarySerializer_ShouldSerializeStringSet()
        {
            // Arrange
            var value = new HashSet<string?>
            {
                "",
                "foo",
                "bar",
                "baz",
                "可口可樂"
            };

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

            CollectionAssert.AreEqual(value, readValue!);
        }

        [Test]
        public void BinarySerializer_ShouldSerializeStringList()
        {
            // Arrange
            var value = new List<string?>
            {
                null,
                "",
                "foo",
                "bar",
                "baz",
                "可口可樂"
            };

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
        public void BinarySerializer_ShouldSerializePrimitiveSet()
        {
            // Arrange
            var value = new HashSet<int>
            {
                int.MinValue,
                int.MaxValue,
                0,
                100,
                682317
            };

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
        public void BinarySerializer_ShouldSerializePrimitiveList()
        {
            // Arrange
            var value = new List<int>
            {
                int.MinValue,
                int.MaxValue,
                0,
                100,
                682317
            };

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
            var value = "test-id".AsSnowflakeId();
            Span<byte> buffer = stackalloc byte[BinarySerializer.MaxSizeOf<SnowflakeId>()];
            var serializer = new BinarySerializer(buffer);
            var deserializer = new BinaryDeserializer(buffer);

            Console.WriteLine(value.ToString());

            // Act
            bool serialized = serializer.TryWriteSnowflakeId(value);

            // Assert
            Assert.That(serialized, "Could not perform serialization.");
            Assert.That(serializer.Offset, Is.EqualTo(BinarySerializer.SizeOf<SnowflakeId>()));
            Assert.That(deserializer.TryReadSnowflakeId(out var readValue), "Could not perform deserialization.");
            Assert.That(deserializer.Offset, Is.EqualTo(BinarySerializer.SizeOf<SnowflakeId>()));

            Assert.That(readValue!.Value, Is.EqualTo(value));
        }

        [Test]
        public void BinarySerializer_ShouldSerializeGuid()
        {
            // Arrange
            var value = "test".AsGuid();
            Span<byte> buffer = stackalloc byte[BinarySerializer.MaxSizeOf<Guid>()];
            var serializer = new BinarySerializer(buffer);
            var deserializer = new BinaryDeserializer(buffer);

            // Act
            bool serialized = serializer.TryWriteGuid(value);

            // Assert
            Assert.That(serialized, "Could not perform serialization.");
            Assert.That(serializer.Offset, Is.EqualTo(BinarySerializer.SizeOf<Guid>()));
            Assert.That(deserializer.TryReadGuid(out var readValue), "Could not perform deserialization.");
            Assert.That(deserializer.Offset, Is.EqualTo(BinarySerializer.SizeOf<Guid>()));

            Assert.That(readValue!.Value, Is.EqualTo(value));
        }

        [Test]
        public void BinarySerializer_ShouldSerializeDateTime()
        {
            // Arrange
            var value = new DateTime(1990, 12, 25, 6, 9, 0, DateTimeKind.Utc);
            Span<byte> buffer = stackalloc byte[BinarySerializer.MaxSizeOf<DateTime>()];
            var serializer = new BinarySerializer(buffer);
            var deserializer = new BinaryDeserializer(buffer);

            // Act
            bool serialized = serializer.TryWriteDateTime(value);

            // Assert
            Assert.That(serialized, "Could not perform serialization.");
            Assert.That(serializer.Offset, Is.EqualTo(BinarySerializer.SizeOf<DateTime>()));
            Assert.That(deserializer.TryReadDateTime(out var readValue), "Could not perform deserialization.");
            Assert.That(deserializer.Offset, Is.EqualTo(BinarySerializer.SizeOf<DateTime>()));

            Assert.That(readValue!.Value.Kind, Is.EqualTo(value.Kind));
            Assert.That(readValue!.Value, Is.EqualTo(value));
        }

        [Test]
        public void BinarySerializer_ShouldSerializeDateTimeOffset()
        {
            // Arrange
            var value = new DateTimeOffset(new DateTime(1990, 12, 25, 6, 9, 0));
            Span<byte> buffer = stackalloc byte[BinarySerializer.MaxSizeOf<DateTimeOffset>()];
            var serializer = new BinarySerializer(buffer);
            var deserializer = new BinaryDeserializer(buffer);

            // Act
            bool serialized = serializer.TryWriteDateTimeOffset(value);

            // Assert
            Assert.That(serialized, "Could not perform serialization.");
            Assert.That(serializer.Offset, Is.EqualTo(BinarySerializer.SizeOf<DateTimeOffset>()));
            Assert.That(deserializer.TryReadDateTimeOffset(out var readValue), "Could not perform deserialization.");
            Assert.That(deserializer.Offset, Is.EqualTo(BinarySerializer.SizeOf<DateTimeOffset>()));

            Assert.That(readValue!.Value.Offset, Is.EqualTo(value.Offset));
            Assert.That(readValue!.Value, Is.EqualTo(value));
        }

        [TestCase(null), TestCase("")]
        [TestCase("Bob Mitchell"), TestCase("\u0638\u0639 \u063A\u063B")]
        [TestCase("Hartmann Schröder"), TestCase("Gro ß")]
        [TestCase("漢字"), TestCase("加拿大")]
        public void BinarySerializer_ShouldSerializeString(string? value)
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[BinarySerializer.MaxSizeOf(value)];
            var serializer = new BinarySerializer(buffer);
            var deserializer = new BinaryDeserializer(buffer);

            // Act
            bool serialized = serializer.TryWriteString(value);

            // Assert
            Assert.That(serialized, "Could not perform serialization.");
            Assert.That(serializer.Offset, Is.EqualTo(BinarySerializer.SizeOf(value)));
            Assert.That(deserializer.TryReadString(out string? readValue), "Could not perform deserialization.");
            Assert.That(deserializer.Offset, Is.EqualTo(BinarySerializer.SizeOf(value)));

            Assert.That(readValue, value is null ? Is.Empty : Is.EqualTo(value));
        }

        #endregion

        #region Primitives

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

        [TestCase(byte.MinValue)]
        [TestCase(byte.MaxValue)]
        [TestCase(123)]
        [TestCase(42)]
        public void BinarySerializer_ShouldSerializeByte(byte value)
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

        [TestCase(short.MinValue)]
        [TestCase(short.MaxValue)]
        [TestCase(0)]
        [TestCase(4252)]
        public void BinarySerializer_ShouldSerializeInt16(short value)
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

        [TestCase(ushort.MinValue)]
        [TestCase(ushort.MaxValue)]
        [TestCase((ushort)123)]
        [TestCase((ushort)4252)]
        public void BinarySerializer_ShouldSerializeUInt16(ushort value)
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

        [TestCase(int.MinValue)]
        [TestCase(int.MaxValue)]
        [TestCase(0)]
        [TestCase(4252)]
        public void BinarySerializer_ShouldSerializeInt32(int value)
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

        [TestCase(uint.MinValue)]
        [TestCase(uint.MaxValue)]
        [TestCase((uint)123)]
        [TestCase((uint)4252)]
        public void BinarySerializer_ShouldSerializeUInt32(uint value)
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

        [TestCase(long.MinValue)]
        [TestCase(long.MaxValue)]
        [TestCase(0)]
        [TestCase(4252)]
        public void BinarySerializer_ShouldSerializeInt64(long value)
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

        [TestCase(ulong.MinValue)]
        [TestCase(ulong.MaxValue)]
        [TestCase(123ul)]
        [TestCase(4252ul)]
        public void BinarySerializer_ShouldSerializeUInt64(ulong value)
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

        [TestCase(double.MinValue)]
        [TestCase(double.MaxValue)]
        [TestCase(0)]
        [TestCase(4252.69420d)]
        [TestCase(1234 + double.Epsilon)]
        public void BinarySerializer_ShouldSerializeDouble(double value)
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

        [TestCase(float.MinValue)]
        [TestCase(float.MaxValue)]
        [TestCase(0)]
        [TestCase(4252.69420f)]
        [TestCase(1234 + float.Epsilon)]
        public void BinarySerializer_ShouldSerializeFloat(float value)
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
