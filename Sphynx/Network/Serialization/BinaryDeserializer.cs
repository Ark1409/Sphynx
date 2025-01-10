// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
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

        #region Arrays

        public bool TryReadArray<T>(out T[]? array) where T : unmanaged
        {
            var typeCode = Type.GetTypeCode(typeof(T));
            switch (typeCode)
            {
                case TypeCode.Boolean:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                {
                    if (!TryReadInt32(out int? size))
                    {
                        array = null;
                        return false;
                    }

                    // Try and catch it early
                    if (!CanRead(size.Value * Unsafe.SizeOf<T>()))
                    {
                        _offset -= sizeof(int);
                        array = null;
                        return false;
                    }

                    int sizeValue = size.Value;
                    array = sizeValue == 0 ? Array.Empty<T>() : new T[sizeValue];

                    for (int i = 0; i < sizeValue; i++)
                    {
                        array[i] = ReadPrimitive<T>();
                    }

                    return true;
                }

                default:
                    array = null;
                    return false;
            }
        }

        public T[] ReadArray<T>() where T : unmanaged
        {
            var typeCode = Type.GetTypeCode(typeof(T));
            switch (typeCode)
            {
                case TypeCode.Boolean:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                {
                    int size = ReadInt32();
                    var array = size == 0 ? Array.Empty<T>() : new T[size];

                    for (int i = 0; i < size; i++)
                    {
                        array[i] = ReadPrimitive<T>();
                    }

                    return array;
                }

                default:
                    throw new ArgumentException($"Deserialization of {typeof(T)} is unsupported");
            }
        }

        #endregion

        #region Collections

        public bool TryReadDateTimeCollection<TCollection>([NotNullWhen(true)] out TCollection? collection)
            where TCollection : ICollection<DateTime>, new()
        {
            int fallbackOffset = _offset;

            if (!TryReadInt32(out int? size))
            {
                collection = default;
                return false;
            }

            // Try and catch it early
            if (!CanRead(size.Value * BinarySerializer.MaxSizeOf<DateTime>()))
            {
                _offset = fallbackOffset;
                collection = default;
                return false;
            }

            if (typeof(TCollection) == typeof(List<DateTime>))
            {
                var list = new List<DateTime>(size.Value);
                collection = Unsafe.As<List<DateTime>, TCollection>(ref list);
            }
            else if (typeof(TCollection) == typeof(HashSet<DateTime>))
            {
                var set = new HashSet<DateTime>(size.Value);
                collection = Unsafe.As<HashSet<DateTime>, TCollection>(ref set);
            }
            else
            {
                collection = new TCollection();
            }

            for (int i = 0; i < size; i++)
            {
                if (!TryReadDateTime(out var dateTime))
                {
                    _offset = fallbackOffset;
                    collection = default;
                    return false;
                }

                collection.Add(dateTime.Value);
            }

            return true;
        }

        public TCollection ReadDateTimeCollection<TCollection>()
            where TCollection : ICollection<DateTime>, new()
        {
            int size = ReadInt32();
            TCollection collection;

            if (typeof(TCollection) == typeof(List<DateTime>))
            {
                var list = new List<DateTime>(size);
                collection = Unsafe.As<List<DateTime>, TCollection>(ref list);
            }
            else if (typeof(TCollection) == typeof(HashSet<DateTime>))
            {
                var set = new HashSet<DateTime>(size);
                collection = Unsafe.As<HashSet<DateTime>, TCollection>(ref set);
            }
            else
            {
                collection = new TCollection();
            }

            for (int i = 0; i < size; i++)
            {
                collection.Add(ReadDateTime());
            }

            return collection;
        }

        public bool TryReadSnowflakeIdCollection<TCollection>([NotNullWhen(true)] out TCollection? collection)
            where TCollection : ICollection<SnowflakeId>, new()
        {
            if (!TryReadInt32(out int? size))
            {
                collection = default;
                return false;
            }

            _offset -= sizeof(int);

            // Try and catch it early
            if (!CanRead(size.Value * SnowflakeId.SIZE))
            {
                collection = default;
                return false;
            }

            collection = ReadSnowflakeIdCollection<TCollection>();
            return true;
        }

        public TCollection ReadSnowflakeIdCollection<TCollection>()
            where TCollection : ICollection<SnowflakeId>, new()
        {
            int size = ReadInt32();
            TCollection collection;

            if (typeof(TCollection) == typeof(List<SnowflakeId>))
            {
                var list = new List<SnowflakeId>(size);
                collection = Unsafe.As<List<SnowflakeId>, TCollection>(ref list);
            }
            else if (typeof(TCollection) == typeof(HashSet<SnowflakeId>))
            {
                var set = new HashSet<SnowflakeId>(size);
                collection = Unsafe.As<HashSet<SnowflakeId>, TCollection>(ref set);
            }
            else
            {
                collection = new TCollection();
            }

            for (int i = 0; i < size; i++)
            {
                collection.Add(ReadSnowflakeId());
            }

            return collection;
        }

        public bool TryReadGuidCollection<TCollection>([NotNullWhen(true)] out TCollection? collection)
            where TCollection : ICollection<Guid>, new()
        {
            if (!TryReadInt32(out int? size))
            {
                collection = default;
                return false;
            }

            _offset -= sizeof(int);

            // Try and catch it early
            if (!CanRead(size.Value * Unsafe.SizeOf<Guid>()))
            {
                collection = default;
                return false;
            }

            collection = ReadGuidCollection<TCollection>();
            return true;
        }

        public TCollection ReadGuidCollection<TCollection>()
            where TCollection : ICollection<Guid>, new()
        {
            int size = ReadInt32();
            TCollection collection;

            if (typeof(TCollection) == typeof(List<Guid>))
            {
                var list = new List<Guid>(size);
                collection = Unsafe.As<List<Guid>, TCollection>(ref list);
            }
            else if (typeof(TCollection) == typeof(HashSet<Guid>))
            {
                var set = new HashSet<Guid>(size);
                collection = Unsafe.As<HashSet<Guid>, TCollection>(ref set);
            }
            else
            {
                collection = new TCollection();
            }

            for (int i = 0; i < size; i++)
            {
                collection.Add(ReadGuid());
            }

            return collection;
        }

        public bool TryReadStringCollection<TCollection>([NotNullWhen(true)] out TCollection? collection)
            where TCollection : ICollection<string>, new()
        {
            int fallbackOffset = _offset;

            if (!TryReadInt32(out int? size))
            {
                collection = default;
                return false;
            }

            // Try and catch it early
            if (!CanRead(size.Value * BinarySerializer.MaxSizeOf(string.Empty)))
            {
                _offset = fallbackOffset;
                collection = default;
                return false;
            }

            if (typeof(TCollection) == typeof(List<string>))
            {
                var list = new List<string>(size.Value);
                collection = Unsafe.As<List<string>, TCollection>(ref list);
            }
            else if (typeof(TCollection) == typeof(HashSet<string>))
            {
                var set = new HashSet<string>(size.Value);
                collection = Unsafe.As<HashSet<string>, TCollection>(ref set);
            }
            else
            {
                collection = new TCollection();
            }

            for (int i = 0; i < size; i++)
            {
                if (!TryReadString(out string? value))
                {
                    _offset = fallbackOffset;
                    collection = default;
                    return false;
                }

                collection.Add(value);
            }

            return true;
        }

        public TCollection ReadStringCollection<TCollection>()
            where TCollection : ICollection<string>, new()
        {
            int size = ReadInt32();
            TCollection collection;

            if (typeof(TCollection) == typeof(List<string>))
            {
                var list = new List<string>(size);
                collection = Unsafe.As<List<string>, TCollection>(ref list);
            }
            else if (typeof(TCollection) == typeof(HashSet<string>))
            {
                var set = new HashSet<string>(size);
                collection = Unsafe.As<HashSet<string>, TCollection>(ref set);
            }
            else
            {
                collection = new TCollection();
            }

            for (int i = 0; i < size; i++)
            {
                collection.Add(ReadString());
            }

            return collection;
        }

        public bool TryReadCollection<T, TCollection>([NotNullWhen(true)] out TCollection? collection)
            where T : unmanaged
            where TCollection : ICollection<T>, new()
        {
            var typeCode = Type.GetTypeCode(typeof(T));
            switch (typeCode)
            {
                case TypeCode.Boolean:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                {
                    if (!TryReadInt32(out int? size))
                    {
                        collection = default;
                        return false;
                    }

                    if (!CanRead(size.Value) || !CanRead(Unsafe.SizeOf<T>() * size.Value))
                    {
                        _offset -= sizeof(int);
                        collection = default;
                        return false;
                    }

                    collection = new TCollection();

                    for (int i = 0; i < size; i++)
                    {
                        collection.Add(ReadPrimitive<T>());
                    }

                    return true;
                }

                default:
                    collection = default;
                    return false;
            }
        }

        public TCollection ReadCollection<T, TCollection>() where T : unmanaged
            where TCollection : ICollection<T>, new()
        {
            var typeCode = Type.GetTypeCode(typeof(T));
            switch (typeCode)
            {
                case TypeCode.Boolean:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                {
                    int size = ReadInt32();
                    var collection = new TCollection();

                    for (int i = 0; i < size; i++)
                    {
                        collection.Add(ReadPrimitive<T>());
                    }

                    return collection;
                }

                default:
                    throw new ArgumentException($"Deserialization of {typeof(T)} type is unsupported");
            }
        }

        #endregion

        #region Common Types

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

        public bool TryReadGuid([NotNullWhen(true)] out Guid? id)
        {
            if (!CanRead(Unsafe.SizeOf<Guid>()))
            {
                id = null;
                return false;
            }

            id = ReadGuid();
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Guid ReadGuid()
        {
            var id = new Guid(_span.Slice(_offset, Unsafe.SizeOf<Guid>()));
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
            // Try and catch it early
            if (!TryReadInt32(out int? size))
            {
                str = null;
                return false;
            }

            if (!CanRead(size.Value))
            {
                _offset -= sizeof(int);
                str = null;
                return false;
            }

            str = BinarySerializer.StringEncoding.GetString(_span.Slice(_offset, size.Value));
            _offset += sizeof(int) + size.Value;

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
            if (!CanRead(BinarySerializer.SizeOf<DateTime>()))
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

        #endregion

        #region Primitive Types

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

        private T ReadPrimitive<T>() where T : unmanaged
        {
            var typeCode = Type.GetTypeCode(typeof(T));
            switch (typeCode)
            {
                case TypeCode.Boolean:
                {
                    bool value = ReadBool();
                    return Unsafe.As<bool, T>(ref value);
                }
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
                case TypeCode.Single:
                {
                    float value = ReadFloat();
                    return Unsafe.As<float, T>(ref value);
                }
                case TypeCode.Double:
                {
                    double value = ReadDouble();
                    return Unsafe.As<double, T>(ref value);
                }

                default:
                    throw new ArgumentException($"Serialization of {typeof(T)} type is unsupported");
            }
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanRead(int size)
        {
            return size > 0 && _offset <= _span.Length - size;
        }
    }
}
