// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using Sphynx.Core;

namespace Sphynx.Network.Serialization
{
    /// <summary>
    /// Deserializes primitive types from a <see cref="ReadOnlySpan{T}"/>, while also tracking the number of
    /// bytes currently written.
    /// </summary>
    public ref struct BinaryDeserializer
    {
        private readonly ReadOnlySpan<byte> _span;
        private int _offset;

        /// <summary>
        /// Returns the number of bytes read from the underlying span.
        /// </summary>
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _offset;
        }

        public BinaryDeserializer(ReadOnlyMemory<byte> memory) : this(memory.Span)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BinaryDeserializer(ReadOnlySpan<byte> span)
        {
            _span = span;
            _offset = 0;
        }

        public bool TryReadSnowflakeId([NotNullWhen(true)] out SnowflakeId? id)
        {
            if (!CanRead(SnowflakeId.SIZE))
            {
                id = null;
                return false;
            }

            id = ReadSnowflakeId();
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SnowflakeId ReadSnowflakeId()
        {
            var id = new SnowflakeId(_span.Slice(_offset, SnowflakeId.SIZE));
            _offset += SnowflakeId.SIZE;
            return id;
        }

        public bool TryReadString(Span<char> dest)
        {
            if (!CanRead(BinarySerializer.SizeOf(string.Empty)))
                return false;

            ReadString(dest);
            return true;
        }

        public void ReadString(Span<char> dest)
        {
            int size = ReadInt32();
            int decoded = BinarySerializer.StringEncoding.GetChars(_span.Slice(_offset, size), dest);

            Debug.Assert(size == decoded);

            _offset += sizeof(int) + size;
        }

        public bool TryReadString([NotNullWhen(true)] out string? str)
        {
            if (!CanRead(BinarySerializer.SizeOf(string.Empty)))
            {
                str = null;
                return false;
            }

            str = ReadString();
            return true;
        }

        public string ReadString()
        {
            int size = ReadInt32();
            string str = BinarySerializer.StringEncoding.GetString(_span.Slice(_offset, size));
            _offset += sizeof(int) + size;

            return str;
        }

        public bool TryReadEnum<T>(out T? value) where T : struct, Enum
        {
            if (!CanRead(Unsafe.SizeOf<T>()))
            {
                value = null;
                return false;
            }

            value = ReadEnum<T>();
            return true;
        }

        public T ReadEnum<T>() where T : struct, Enum
        {
            var underlyingType = Enum.GetUnderlyingType(typeof(T));
            switch (Type.GetTypeCode(underlyingType))
            {
                case TypeCode.Byte:
                {
                    byte value = ReadByte();
                    return Unsafe.As<byte, T>(ref value);
                }
                case TypeCode.Int16:
                {
                    short value = ReadInt16();
                    return Unsafe.As<short, T>(ref value);
                }
                case TypeCode.UInt16:
                {
                    ushort value = ReadUInt16();
                    return Unsafe.As<ushort, T>(ref value);
                }
                case TypeCode.Int32:
                {
                    int value = ReadInt32();
                    return Unsafe.As<int, T>(ref value);
                }
                case TypeCode.UInt32:
                {
                    uint value = ReadUInt32();
                    return Unsafe.As<uint, T>(ref value);
                }
                case TypeCode.Int64:
                {
                    long value = ReadInt64();
                    return Unsafe.As<long, T>(ref value);
                }
                case TypeCode.UInt64:
                {
                    ulong value = ReadUInt64();
                    return Unsafe.As<ulong, T>(ref value);
                }

                default:
                    throw new ArgumentException($"Unsupported underlying type: {underlyingType}");
            }
        }

        public bool TryReadDateTime([NotNullWhen(true)] out DateTime? dateTime)
        {
            if (!CanRead(BinarySerializer.SizeOf(default(DateTime))))
            {
                dateTime = null;
                return false;
            }

            dateTime = ReadDateTime();
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DateTime ReadDateTime()
        {
            return new DateTime(ReadInt64(), DateTimeKind.Utc);
        }

        public bool TryReadBool([NotNullWhen(true)] out bool? value)
        {
            if (!CanRead(sizeof(bool)))
            {
                value = null;
                return false;
            }

            value = ReadBool();
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadBool()
        {
            return ReadByte() != 0;
        }

        public bool TryReadByte([NotNullWhen(true)] out byte? value)
        {
            if (!CanRead(sizeof(byte)))
            {
                value = null;
                return false;
            }

            value = ReadByte();
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte()
        {
            return _span[_offset++];
        }

        public bool TryReadUInt16([NotNullWhen(true)] out ushort? value)
        {
            if (!CanRead(sizeof(ushort)))
            {
                value = null;
                return false;
            }

            value = ReadUInt16();
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadUInt16()
        {
            ushort value = BinaryPrimitives.ReadUInt16LittleEndian(_span[_offset..]);
            _offset += sizeof(ushort);
            return value;
        }

        public bool TryReadInt16([NotNullWhen(true)] out short? value)
        {
            if (!CanRead(sizeof(short)))
            {
                value = null;
                return false;
            }

            value = ReadInt16();
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private short ReadInt16()
        {
            short value = BinaryPrimitives.ReadInt16LittleEndian(_span[_offset..]);
            _offset += sizeof(short);
            return value;
        }

        public bool TryReadUInt32([NotNullWhen(true)] out uint? value)
        {
            if (!CanRead(sizeof(uint)))
            {
                value = null;
                return false;
            }

            value = ReadUInt32();
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUInt32()
        {
            uint value = BinaryPrimitives.ReadUInt32LittleEndian(_span[_offset..]);
            _offset += sizeof(uint);
            return value;
        }

        public bool TryReadInt32([NotNullWhen(true)] out int? value)
        {
            if (!CanRead(sizeof(int)))
            {
                value = null;
                return false;
            }

            value = ReadInt32();
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt32()
        {
            int value = BinaryPrimitives.ReadInt32LittleEndian(_span[_offset..]);
            _offset += sizeof(int);
            return value;
        }

        public bool TrySerializeUInt64([NotNullWhen(true)] out ulong? value)
        {
            if (!CanRead(sizeof(ulong)))
            {
                value = null;
                return false;
            }

            value = ReadUInt64();
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadUInt64()
        {
            ulong value = BinaryPrimitives.ReadUInt64LittleEndian(_span[_offset..]);
            _offset += sizeof(ulong);
            return value;
        }

        public bool TryReadInt64([NotNullWhen(true)] out long? value)
        {
            if (!CanRead(sizeof(long)))
            {
                value = null;
                return false;
            }

            value = ReadInt64();
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadInt64()
        {
            long value = BinaryPrimitives.ReadInt64LittleEndian(_span[_offset..]);
            _offset += sizeof(long);
            return value;
        }

        public bool TryReadFloat([NotNullWhen(true)] out float? value)
        {
            if (!CanRead(sizeof(float)))
            {
                value = null;
                return false;
            }

            value = ReadFloat();
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float ReadFloat()
        {
            float value = BinaryPrimitives.ReadSingleLittleEndian(_span[_offset..]);
            _offset += sizeof(float);
            return value;
        }

        public bool TryReadDouble([NotNullWhen(true)] out double? value)
        {
            if (!CanRead(sizeof(double)))
            {
                value = null;
                return false;
            }

            value = ReadDouble();
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double ReadDouble()
        {
            double value = BinaryPrimitives.ReadDoubleLittleEndian(_span[_offset..]);
            _offset += sizeof(double);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanRead(int size)
        {
            return _offset <= _span.Length - size;
        }
    }
}
