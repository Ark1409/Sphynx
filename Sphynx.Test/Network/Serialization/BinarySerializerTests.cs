// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Nerdbank.Streams;
using NUnit.Framework.Legacy;
using Sphynx.Core;
using Sphynx.Network.Serialization;
using Sphynx.Storage;
using Sphynx.Test.Utils;

namespace Sphynx.Test.Network.Serialization
{
    [TestFixture]
    public class BinarySerializerTests : SerializerTest
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

            var serializer = new BinarySerializer(Sequence);

            // Act
            serializer.WriteDictionary(value);

            // Assert
            var deserializer = new BinaryDeserializer(Sequence);

            Assert.That(serializer.BytesWritten, Is.EqualTo(BinarySerializer.SizeOf(value)));
            Assert.That(deserializer.TryReadDictionary(out Dictionary<DateTimeOffset, string?>? readValue), "Could not perform deserialization.");
            Assert.That(deserializer.Offset, Is.EqualTo(BinarySerializer.SizeOf(value)));

            Assert.That(readValue!.Count, Is.EqualTo(value.Count));
            foreach (var kvp in value)
            {
                Assert.That(readValue.ContainsKey(kvp.Key));
                Assert.That(readValue.ContainsValue(kvp.Value));
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

            var serializer = new BinarySerializer(Sequence);

            // Act
            serializer.WriteDictionary(value);

            // Assert
            var deserializer = new BinaryDeserializer(Sequence);

            Assert.That(serializer.BytesWritten, Is.EqualTo(BinarySerializer.SizeOf(value)));
            Assert.That(deserializer.TryReadDictionary(out Dictionary<string, SnowflakeId>? readValue), "Could not perform deserialization.");
            Assert.That(deserializer.Offset, Is.EqualTo(BinarySerializer.SizeOf(value)));

            CollectionAssert.AreEqual(value, readValue!);
        }

        [Test]
        public void BinarySerializer_ShouldSerializeStringDictionary()
        {
            // Arrange
            var value = new Dictionary<string, string?>
            {
                { "", null },
                { "foo", "" },
                { "bar", "é" },
                { "Sphynx", "ç-1234567890/*-+!@#$%^&*()" },
                { "凹", "The quick brown fox jumps over the lazy dog" },
            };

            var serializer = new BinarySerializer(Sequence);

            // Act
            serializer.WriteDictionary(value);

            // Assert
            var deserializer = new BinaryDeserializer(Sequence);
            Assert.That(serializer.BytesWritten, Is.EqualTo(BinarySerializer.SizeOf(value)));
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

            var serializer = new BinarySerializer(Sequence);

            // Act
            serializer.WriteDictionary(value);

            // Assert
            var deserializer = new BinaryDeserializer(Sequence);

            Assert.That(serializer.BytesWritten, Is.EqualTo(BinarySerializer.SizeOf(value)));
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
                null,
                "",
                "foo",
                "bar",
                "baz",
                "可口可樂"
            };

            var serializer = new BinarySerializer(Sequence);

            // Act
            serializer.WriteCollection(value);

            // Assert
            var deserializer = new BinaryDeserializer(Sequence);

            Assert.That(serializer.BytesWritten, Is.EqualTo(BinarySerializer.SizeOf(value)));
            Assert.That(deserializer.TryReadCollection<HashSet<string?>>(out var readValue), "Could not perform deserialization.");
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

            var serializer = new BinarySerializer(Sequence);

            // Act
            serializer.WriteCollection(value);

            // Assert
            var deserializer = new BinaryDeserializer(Sequence);

            Assert.That(serializer.BytesWritten, Is.EqualTo(BinarySerializer.SizeOf(value)));
            Assert.That(deserializer.TryReadStringList(out var readValue), "Could not perform deserialization.");
            Assert.That(deserializer.Offset, Is.EqualTo(BinarySerializer.SizeOf(value)));

            CollectionAssert.AreEqual(value, readValue!);
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

            var serializer = new BinarySerializer(Sequence);

            // Act
            serializer.WriteCollection(value);

            // Assert
            var deserializer = new BinaryDeserializer(Sequence);

            Assert.That(serializer.BytesWritten, Is.EqualTo(BinarySerializer.SizeOf(value)));
            Assert.That(deserializer.TryReadCollection<int, HashSet<int>>(out var readValue), "Could not perform deserialization.");
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

            var serializer = new BinarySerializer(Sequence);

            // Act
            serializer.WriteCollection(value);

            // Assert
            var deserializer = new BinaryDeserializer(Sequence);

            Assert.That(serializer.BytesWritten, Is.EqualTo(BinarySerializer.SizeOf(value)));
            Assert.That(deserializer.TryReadList<int>(out var readValue), "Could not perform deserialization.");
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
            var serializer = new BinarySerializer(Sequence);

            // Act
            serializer.WriteSnowflakeId(value);

            // Assert
            var deserializer = new BinaryDeserializer(Sequence);

            Assert.That(serializer.BytesWritten, Is.EqualTo(BinarySerializer.SizeOf<SnowflakeId>()));
            Assert.That(deserializer.TryReadSnowflakeId(out var readValue), "Could not perform deserialization.");
            Assert.That(deserializer.Offset, Is.EqualTo(BinarySerializer.SizeOf<SnowflakeId>()));

            Assert.That(readValue, Is.EqualTo(value));
        }

        [Test]
        public void BinarySerializer_ShouldSerializeGuid()
        {
            // Arrange
            var value = "test".AsGuid();
            var serializer = new BinarySerializer(Sequence);

            // Act
            serializer.WriteGuid(value);

            // Assert
            var deserializer = new BinaryDeserializer(Sequence);

            Assert.That(serializer.BytesWritten, Is.EqualTo(BinarySerializer.SizeOf<Guid>()));
            Assert.That(deserializer.TryReadGuid(out var readValue), "Could not perform deserialization.");
            Assert.That(deserializer.Offset, Is.EqualTo(BinarySerializer.SizeOf<Guid>()));

            Assert.That(readValue, Is.EqualTo(value));
        }

        [Test]
        public void BinarySerializer_ShouldSerializeDateTime()
        {
            // Arrange
            var value = new DateTime(1991, 12, 25, 6, 9, 0, DateTimeKind.Utc);
            var serializer = new BinarySerializer(Sequence);

            // Act
            serializer.WriteDateTime(value);

            // Assert
            var deserializer = new BinaryDeserializer(Sequence);

            Assert.That(serializer.BytesWritten, Is.EqualTo(BinarySerializer.SizeOf<DateTime>()));
            Assert.That(deserializer.TryReadDateTime(out var readValue), "Could not perform deserialization.");
            Assert.That(deserializer.Offset, Is.EqualTo(BinarySerializer.SizeOf<DateTime>()));

            Assert.That(readValue.Kind, Is.EqualTo(value.Kind));
            Assert.That(readValue, Is.EqualTo(value));
        }

        [Test]
        public void BinarySerializer_ShouldSerializeDateTimeOffset()
        {
            // Arrange
            var value = new DateTimeOffset(new DateTime(1990, 12, 25, 6, 9, 0));
            var serializer = new BinarySerializer(Sequence);

            // Act
            serializer.WriteDateTimeOffset(value);

            // Assert
            var deserializer = new BinaryDeserializer(Sequence);

            Assert.That(serializer.BytesWritten, Is.EqualTo(BinarySerializer.SizeOf<DateTimeOffset>()));
            Assert.That(deserializer.TryReadDateTimeOffset(out var readValue), "Could not perform deserialization.");
            Assert.That(deserializer.Offset, Is.EqualTo(BinarySerializer.SizeOf<DateTimeOffset>()));

            Assert.That(readValue.Offset, Is.EqualTo(value.Offset));
            Assert.That(readValue, Is.EqualTo(value));
        }

        [TestCase(null), TestCase("")]
        [TestCase("Bob Mitchell"), TestCase("\u0638\u0639 \u063A\u063B")]
        [TestCase("Hartmann Schröder"), TestCase("Gro ß")]
        [TestCase("漢字"), TestCase("加拿大")]
        public void BinarySerializer_ShouldSerializeString(string? value)
        {
            // Arrange
            var serializer = new BinarySerializer(Sequence);

            // Act
            serializer.WriteString(value);

            // Assert
            var deserializer = new BinaryDeserializer(Sequence);

            Assert.That(serializer.BytesWritten, Is.EqualTo(BinarySerializer.SizeOf(value)));
            Assert.That(deserializer.TryReadString(out string? readValue), "Could not perform deserialization.");
            Assert.That(deserializer.Offset, Is.EqualTo(BinarySerializer.SizeOf(value)));

            Assert.That(readValue, Is.EqualTo(value));
        }

        #endregion

        #region Primitives

        [TestCase(true)]
        [TestCase(false)]
        public void BinarySerializer_ShouldSerializeBool(bool value)
        {
            // Arrange
            var sequence = SequenceRental.Value.Value;
            var serializer = new BinarySerializer(sequence);

            // Act
            serializer.WriteBool(value);

            // Assert
            var deserializer = new BinaryDeserializer(sequence);

            Assert.That(BinarySerializer.SizeOf<bool>(), Is.EqualTo(serializer.BytesWritten));
            Assert.That(deserializer.TryReadBool(out bool readValue), "Could not perform deserialization.");
            Assert.That(BinarySerializer.SizeOf<bool>(), Is.EqualTo(deserializer.Offset));

            Assert.That(value, Is.EqualTo(readValue));
        }

        [TestCase(byte.MinValue)]
        [TestCase(byte.MaxValue)]
        [TestCase(123)]
        [TestCase(42)]
        public void BinarySerializer_ShouldSerializeByte(byte value)
        {
            // Arrange
            var sequence = SequenceRental.Value.Value;
            var serializer = new BinarySerializer(sequence);

            // Act
            serializer.WriteUInt8(value);

            // Assert
            var deserializer = new BinaryDeserializer(sequence);

            Assert.That(BinarySerializer.SizeOf<byte>(), Is.EqualTo(serializer.BytesWritten));
            Assert.That(deserializer.TryReadUInt8(out byte readValue), "Could not perform deserialization.");
            Assert.That(BinarySerializer.SizeOf<byte>(), Is.EqualTo(deserializer.Offset));

            Assert.That(value, Is.EqualTo(readValue));
        }

        [TestCase(short.MinValue)]
        [TestCase(short.MaxValue)]
        [TestCase(0)]
        [TestCase(4252)]
        public void BinarySerializer_ShouldSerializeInt16(short value)
        {
            // Arrange
            var sequence = SequenceRental.Value.Value;
            var serializer = new BinarySerializer(sequence);

            // Act
            serializer.WriteInt16(value);

            // Assert
            var deserializer = new BinaryDeserializer(sequence);

            Assert.That(BinarySerializer.SizeOf<short>(), Is.EqualTo(serializer.BytesWritten));
            Assert.That(deserializer.TryReadInt16(out short readValue), "Could not perform deserialization.");
            Assert.That(BinarySerializer.SizeOf<short>(), Is.EqualTo(deserializer.Offset));

            Assert.That(value, Is.EqualTo(readValue));
        }

        [TestCase(ushort.MinValue)]
        [TestCase(ushort.MaxValue)]
        [TestCase((ushort)123)]
        [TestCase((ushort)4252)]
        public void BinarySerializer_ShouldSerializeUInt16(ushort value)
        {
            // Arrange
            var sequence = SequenceRental.Value.Value;
            var serializer = new BinarySerializer(sequence);

            // Act
            serializer.WriteUInt16(value);

            // Assert
            var deserializer = new BinaryDeserializer(sequence);

            Assert.That(BinarySerializer.SizeOf<ushort>(), Is.EqualTo(serializer.BytesWritten));
            Assert.That(deserializer.TryReadUInt16(out ushort readValue), "Could not perform deserialization.");
            Assert.That(BinarySerializer.SizeOf<ushort>(), Is.EqualTo(deserializer.Offset));

            Assert.That(value, Is.EqualTo(readValue));
        }

        [TestCase(int.MinValue)]
        [TestCase(int.MaxValue)]
        [TestCase(0)]
        [TestCase(4252)]
        public void BinarySerializer_ShouldSerializeInt32(int value)
        {
            // Arrange
            var sequence = SequenceRental.Value.Value;
            var serializer = new BinarySerializer(sequence);

            // Act
            serializer.WriteInt32(value);

            // Assert
            var deserializer = new BinaryDeserializer(sequence);

            Assert.That(BinarySerializer.SizeOf<int>(), Is.EqualTo(serializer.BytesWritten));
            Assert.That(deserializer.TryReadInt32(out int readValue), "Could not perform deserialization.");
            Assert.That(BinarySerializer.SizeOf<int>(), Is.EqualTo(deserializer.Offset));

            Assert.That(value, Is.EqualTo(readValue));
        }

        [TestCase(uint.MinValue)]
        [TestCase(uint.MaxValue)]
        [TestCase((uint)123)]
        [TestCase((uint)4252)]
        public void BinarySerializer_ShouldSerializeUInt32(uint value)
        {
            // Arrange
            var sequence = SequenceRental.Value.Value;
            var serializer = new BinarySerializer(sequence);

            // Act
            serializer.WriteUInt32(value);

            // Assert
            var deserializer = new BinaryDeserializer(sequence);

            Assert.That(BinarySerializer.SizeOf<uint>(), Is.EqualTo(serializer.BytesWritten));
            Assert.That(deserializer.TryReadUInt32(out uint readValue), "Could not perform deserialization.");
            Assert.That(BinarySerializer.SizeOf<uint>(), Is.EqualTo(deserializer.Offset));

            Assert.That(value, Is.EqualTo(readValue));
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

            // Act
            serializer.WriteInt64(value);

            // Assert
            var deserializer = new BinaryDeserializer(buffer);

            Assert.That(BinarySerializer.SizeOf<long>(), Is.EqualTo(serializer.BytesWritten));
            Assert.That(deserializer.TryReadInt64(out long readValue), "Could not perform deserialization.");
            Assert.That(BinarySerializer.SizeOf<long>(), Is.EqualTo(deserializer.Offset));

            Assert.That(value, Is.EqualTo(readValue));
        }

        [TestCase(ulong.MinValue)]
        [TestCase(ulong.MaxValue)]
        [TestCase(123ul)]
        [TestCase(4252ul)]
        public void BinarySerializer_ShouldSerializeUInt64(ulong value)
        {
            // Arrange
            var sequence = SequenceRental.Value.Value;
            var serializer = new BinarySerializer(sequence);

            // Act
            serializer.WriteUInt64(value);

            // Assert
            var deserializer = new BinaryDeserializer(sequence);

            Assert.That(BinarySerializer.SizeOf<ulong>(), Is.EqualTo(serializer.BytesWritten));
            Assert.That(deserializer.TryReadUInt64(out ulong readValue), "Could not perform deserialization.");
            Assert.That(BinarySerializer.SizeOf<ulong>(), Is.EqualTo(deserializer.Offset));

            Assert.That(value, Is.EqualTo(readValue));
        }

        [TestCase(double.MinValue)]
        [TestCase(double.MaxValue)]
        [TestCase(0)]
        [TestCase(4252.69420d)]
        [TestCase(1234 + double.Epsilon)]
        public void BinarySerializer_ShouldSerializeDouble(double value)
        {
            // Arrange
            var sequence = SequenceRental.Value.Value;
            var serializer = new BinarySerializer(sequence);

            // Act
            serializer.WriteDouble(value);

            // Assert
            var deserializer = new BinaryDeserializer(sequence);

            Assert.That(BinarySerializer.SizeOf<double>(), Is.EqualTo(serializer.BytesWritten));
            Assert.That(deserializer.TryReadDouble(out double readValue), "Could not perform deserialization.");
            Assert.That(BinarySerializer.SizeOf<double>(), Is.EqualTo(deserializer.Offset));

            Assert.That(value, Is.EqualTo(readValue));
        }

        [TestCase(float.MinValue)]
        [TestCase(float.MaxValue)]
        [TestCase(0)]
        [TestCase(4252.69420f)]
        [TestCase(1234 + float.Epsilon)]
        public void BinarySerializer_ShouldSerializeFloat(float value)
        {
            // Arrange
            var sequence = SequenceRental.Value.Value;
            var serializer = new BinarySerializer(sequence);

            // Act
            serializer.WriteSingle(value);

            // Assert
            var deserializer = new BinaryDeserializer(sequence);

            Assert.That(BinarySerializer.SizeOf<float>(), Is.EqualTo(serializer.BytesWritten));
            Assert.That(deserializer.TryReadSingle(out float readValue), "Could not perform deserialization.");
            Assert.That(BinarySerializer.SizeOf<float>(), Is.EqualTo(deserializer.Offset));

            Assert.That(value, Is.EqualTo(readValue));
        }

        #endregion
    }
}
