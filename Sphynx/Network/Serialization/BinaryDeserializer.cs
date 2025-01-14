// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

        #region Dictionaries

        public bool TryReadDictionary([NotNullWhen(true)] out IDictionary<string, string?>? dictionary)
        {
            if (!CanRead(BinarySerializer.MaxSizeOf(ImmutableDictionary<string, string?>.Empty)) &&
                !CanRead(BinarySerializer.SizeOf(ImmutableDictionary<string, string?>.Empty)))
            {
                dictionary = null;
                return false;
            }

            int fallbackOffset = _offset;

            // Guaranteed to succeed due to size check
            int size = ReadInt32();
            dictionary = CreateDictionary<string, string?>(size);

            for (int i = 0; i < size; i++)
            {
                if (!TryReadString(out string? key) || !TryReadString(out string? value))
                {
                    _offset = fallbackOffset;
                    dictionary = null;
                    return false;
                }

                dictionary.Add(key, value);
            }

            return true;
        }

        public void ReadDictionary(out IDictionary<string, string?> dictionary)
        {
            int size = ReadInt32();
            dictionary = CreateDictionary<string, string?, Dictionary<string, string?>>(size);

            for (int i = 0; i < size; i++)
            {
                string key = ReadString();
                string value = ReadString();
                dictionary.Add(key, value);
            }
        }

        public bool TryReadDictionary<TKey>([NotNullWhen(true)] out IDictionary<TKey, string?>? dictionary)
            where TKey : unmanaged
        {
            if (!CanRead(BinarySerializer.MaxSizeOf(ImmutableDictionary<TKey, string?>.Empty)) &&
                !CanRead(BinarySerializer.SizeOf(ImmutableDictionary<TKey, string?>.Empty)))
            {
                dictionary = null;
                return false;
            }

            int fallbackOffset = _offset;

            // Guaranteed to succeed due to size check
            int size = ReadInt32();
            dictionary = CreateDictionary<TKey, string?>(size);

            for (int i = 0; i < size; i++)
            {
                if (!TryReadUnmanaged<TKey>(out var key) || !TryReadString(out string? value))
                {
                    _offset = fallbackOffset;
                    dictionary = null;
                    return false;
                }

                dictionary.Add(key.Value, value);
            }

            return true;
        }

        public void ReadDictionary<TKey>(out IDictionary<TKey, string?> dictionary)
            where TKey : unmanaged
        {
            int size = ReadInt32();
            dictionary = CreateDictionary<TKey, string?, Dictionary<TKey, string?>>(size);

            for (int i = 0; i < size; i++)
            {
                var key = ReadUnmanaged<TKey>();
                string value = ReadString();
                dictionary.Add(key, value);
            }
        }

        public bool TryReadDictionary<TValue>([NotNullWhen(true)] out IDictionary<string, TValue>? dictionary)
            where TValue : unmanaged
        {
            if (!CanRead(BinarySerializer.MaxSizeOf(ImmutableDictionary<string, TValue>.Empty)) &&
                !CanRead(BinarySerializer.SizeOf(ImmutableDictionary<string, TValue>.Empty)))
            {
                dictionary = null;
                return false;
            }

            int fallbackOffset = _offset;

            // Guaranteed to succeed due to size check
            int size = ReadInt32();
            dictionary = CreateDictionary<string, TValue>(size);

            for (int i = 0; i < size; i++)
            {
                if (!TryReadString(out string? key) || !TryReadUnmanaged<TValue>(out var value))
                {
                    _offset = fallbackOffset;
                    dictionary = null;
                    return false;
                }

                dictionary.Add(key, value.Value);
            }

            return true;
        }

        public void ReadDictionary<TValue>(out IDictionary<string, TValue> dictionary)
            where TValue : unmanaged
        {
            int size = ReadInt32();
            dictionary = CreateDictionary<string, TValue>(size);

            for (int i = 0; i < size; i++)
            {
                string key = ReadString();
                var value = ReadUnmanaged<TValue>();
                dictionary.Add(key, value);
            }
        }

        public bool TryReadDictionary<TKey, TValue, TDictionary>([NotNullWhen(true)] out TDictionary? dictionary)
            where TKey : unmanaged
            where TValue : unmanaged
            where TDictionary : IDictionary<TKey, TValue>, new()
        {
            if (!CanRead(BinarySerializer.MaxSizeOf(ImmutableDictionary<TKey, TValue>.Empty)) &&
                !CanRead(BinarySerializer.SizeOf(ImmutableDictionary<TKey, TValue>.Empty)))
            {
                dictionary = default;
                return false;
            }

            int fallbackOffset = _offset;

            // Guaranteed to succeed due to size check
            int size = ReadInt32();
            dictionary = CreateDictionary<TKey, TValue, TDictionary>(size);

            for (int i = 0; i < size; i++)
            {
                if (!TryReadUnmanaged<TKey>(out var key) || !TryReadUnmanaged<TValue>(out var value))
                {
                    _offset = fallbackOffset;
                    dictionary = default;
                    return false;
                }

                dictionary.Add(key.Value, value.Value);
            }

            return true;
        }

        public TDictionary ReadDictionary<TKey, TValue, TDictionary>()
            where TKey : unmanaged
            where TValue : unmanaged
            where TDictionary : IDictionary<TKey, TValue>, new()
        {
            int size = ReadInt32();
            var dictionary = CreateDictionary<TKey, TValue, TDictionary>(size);

            for (int i = 0; i < size; i++)
            {
                var key = ReadUnmanaged<TKey>();
                var value = ReadUnmanaged<TValue>();
                dictionary.Add(key, value);
            }

            return dictionary;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Dictionary<TKey, TValue> CreateDictionary<TKey, TValue>(int size) where TKey : notnull =>
            CreateDictionary<TKey, TValue, Dictionary<TKey, TValue>>(size);

        private TDictionary CreateDictionary<TKey, TValue, TDictionary>(int size)
            where TKey : notnull
            where TDictionary : IDictionary<TKey, TValue>, new()
        {
            TDictionary dictionary;

            if (typeof(TDictionary) == typeof(Dictionary<TKey, TValue>))
            {
                var dict = new Dictionary<TKey, TValue>(size);
                dictionary = Unsafe.As<Dictionary<TKey, TValue>, TDictionary>(ref dict);
            }
            else
            {
                dictionary = new TDictionary();
            }

            return dictionary;
        }

        #endregion

        #region Arrays

        public bool TryReadArray(out string[]? array)
        {
            if (!TryReadInt32(out int? size))
            {
                array = null;
                return false;
            }

            array = size.Value == 0 ? Array.Empty<string>() : new string[size.Value];

            for (int i = 0; i < size.Value; i++)
            {
                if (TryReadString(out string? str))
                {
                    array[i] = str;
                }
                else
                {
                    array = null;
                    return false;
                }
            }

            return true;
        }

        public string[] ReadArray()
        {
            int size = ReadInt32();
            string[] array = size == 0 ? Array.Empty<string>() : new string[size];

            for (int i = 0; i < size; i++)
            {
                array[i] = ReadString();
            }

            return array;
        }

        public bool TryReadArray<T>([NotNullWhen(true)] out T[]? array) where T : unmanaged
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

            array = size.Value == 0 ? Array.Empty<T>() : new T[size.Value];

            for (int i = 0; i < size.Value; i++)
            {
                array[i] = ReadUnmanaged<T>();
            }

            return true;
        }

        public T[] ReadArray<T>() where T : unmanaged
        {
            int size = ReadInt32();
            var array = size == 0 ? Array.Empty<T>() : new T[size];

            for (int i = 0; i < size; i++)
            {
                array[i] = ReadUnmanaged<T>();
            }

            return array;
        }

        #endregion

        #region Collections

        public bool TryReadCollection<TCollection>([NotNullWhen(true)] out TCollection? collection)
            where TCollection : ICollection<string>, new()
        {
            if (!CanRead(BinarySerializer.MaxSizeOf(ImmutableList<string?>.Empty)) &&
                !CanRead(BinarySerializer.SizeOf(ImmutableList<string?>.Empty)))
            {
                collection = default;
                return false;
            }

            int fallbackOffset = _offset;

            // Guaranteed to succeed due to size check
            int size = ReadInt32();

            collection = CreateCollection<string, TCollection>(size);

            for (int i = 0; i < size; i++)
            {
                if (TryReadString(out string? str))
                {
                    collection.Add(str);
                }
                else
                {
                    _offset = fallbackOffset;
                    collection = default;
                    return false;
                }
            }

            return true;
        }

        public TCollection ReadCollection<TCollection>() where TCollection : ICollection<string>, new()
        {
            int size = ReadInt32();
            var collection = CreateCollection<string, TCollection>(size);

            for (int i = 0; i < size; i++)
                collection.Add(ReadString());

            return collection;
        }

        public bool TryReadCollection<T, TCollection>([NotNullWhen(true)] out TCollection? collection)
            where T : unmanaged
            where TCollection : ICollection<T>, new()
        {
            if (!CanRead(BinarySerializer.MaxSizeOf(Array.Empty<T>())) &&
                !CanRead(BinarySerializer.SizeOf(Array.Empty<T>())))
            {
                collection = default;
                return false;
            }

            // Guaranteed to succeed due to size check
            int size = ReadInt32();

            // Try and catch it early
            if (!CanRead(size * Unsafe.SizeOf<T>()))
            {
                _offset -= sizeof(int);
                collection = default;
                return false;
            }

            collection = CreateCollection<T, TCollection>(size);

            for (int i = 0; i < size; i++)
                collection.Add(ReadUnmanaged<T>());

            return true;
        }

        public TCollection ReadCollection<T, TCollection>()
            where T : unmanaged
            where TCollection : ICollection<T>, new()
        {
            int size = ReadInt32();
            var collection = CreateCollection<T, TCollection>(size);

            for (int i = 0; i < size; i++)
                collection.Add(ReadUnmanaged<T>());

            return collection;
        }

        private TCollection CreateCollection<T, TCollection>(int size)
            where TCollection : ICollection<T>, new()
        {
            TCollection collection;
            if (typeof(TCollection) == typeof(List<T>))
            {
                var list = new List<T>(size);
                collection = Unsafe.As<List<T>, TCollection>(ref list);
            }
            else if (typeof(TCollection) == typeof(HashSet<T>))
            {
                var set = new HashSet<T>(size);
                collection = Unsafe.As<HashSet<T>, TCollection>(ref set);
            }
            else
            {
                collection = new TCollection();
            }

            return collection;
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

        public bool TryReadEnum<T>([NotNullWhen(true)] out T? value) where T : struct, Enum
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

        /// <summary>
        /// Attempts to deserialize unmanaged types from bytes.
        /// </summary>
        /// <param name="value">The unmanaged type to deserialize.</param>
        /// <typeparam name="T">The type of unmanaged type.</typeparam>
        /// <returns>true if the <paramref name="value"/> could be deserialized; false otherwise.</returns>
        public bool TryReadUnmanaged<T>([NotNullWhen(true)] out T? value) where T : unmanaged
        {
            var typeCode = Type.GetTypeCode(typeof(T));

            switch (typeCode)
            {
                case TypeCode.Boolean:
                {
                    bool success = TryReadBool(out bool? val);
                    value = Unsafe.As<bool?, T?>(ref val);
                    return success;
                }
                case TypeCode.Byte:
                case TypeCode.Object when Unsafe.SizeOf<T>() == sizeof(byte):
                {
                    bool success = TryReadByte(out byte? val);
                    value = Unsafe.As<byte?, T?>(ref val);
                    return success;
                }
                case TypeCode.Int16:
                case TypeCode.Object when Unsafe.SizeOf<T>() == sizeof(short):
                {
                    bool success = TryReadInt16(out short? val);
                    value = Unsafe.As<short?, T?>(ref val);
                    return success;
                }
                case TypeCode.UInt16:
                {
                    bool success = TryReadUInt16(out ushort? val);
                    value = Unsafe.As<ushort?, T?>(ref val);
                    return success;
                }
                case TypeCode.Int32:
                case TypeCode.Object when Unsafe.SizeOf<T>() == sizeof(int):
                {
                    bool success = TryReadInt32(out int? val);
                    value = Unsafe.As<int?, T?>(ref val);
                    return success;
                }
                case TypeCode.UInt32:
                {
                    bool success = TryReadUInt32(out uint? val);
                    value = Unsafe.As<uint?, T?>(ref val);
                    return success;
                }
                case TypeCode.Int64:
                case TypeCode.Object when Unsafe.SizeOf<T>() == sizeof(long):
                {
                    bool success = TryReadInt64(out long? val);
                    value = Unsafe.As<long?, T?>(ref val);
                    return success;
                }
                case TypeCode.UInt64:
                {
                    bool success = TryReadUInt64(out ulong? val);
                    value = Unsafe.As<ulong?, T?>(ref val);
                    return success;
                }
                case TypeCode.Single:
                {
                    bool success = TryReadFloat(out float? val);
                    value = Unsafe.As<float?, T?>(ref val);
                    return success;
                }
                case TypeCode.Double:
                {
                    bool success = TryReadDouble(out double? val);
                    value = Unsafe.As<double?, T?>(ref val);
                    return success;
                }
                case TypeCode.DateTime:
                {
                    bool success = TryReadDateTime(out var val);
                    value = Unsafe.As<DateTime?, T?>(ref val);
                    return success;
                }
                case TypeCode.Object when typeof(T) == typeof(SnowflakeId):
                {
                    bool success = TryReadSnowflakeId(out var val);
                    value = Unsafe.As<SnowflakeId?, T?>(ref val);
                    return success;
                }
                case TypeCode.Object when typeof(T) == typeof(Guid):
                {
                    bool success = TryReadGuid(out var val);
                    value = Unsafe.As<Guid?, T?>(ref val);
                    return success;
                }

                case TypeCode.Object:
                {
                    if (BitConverter.IsLittleEndian)
                    {
                        if (MemoryMarshal.TryRead<T>(_span[_offset..], out var val))
                        {
                            _offset += Unsafe.SizeOf<T>();
                            value = val;
                            return true;
                        }
                    }

                    value = null;
                    return false;
                }

                default:
                    value = null;
                    return false;
            }
        }

        /// <summary>
        /// Deserializes unmanaged types from bytes.
        /// </summary>
        /// <typeparam name="T">The type of unmanaged type.</typeparam>
        /// <returns>The deserialized unmanaged type.</returns>
        /// <exception cref="ArgumentException">If <typeparamref name="T"/> is a user-defined struct
        /// which is not of blittable size, we are not on a little-endian machine, and <typeparamref name="T"/>
        /// is not a type for which serialization is already supported.</exception>
        public T ReadUnmanaged<T>() where T : unmanaged
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
                case TypeCode.Object when Unsafe.SizeOf<T>() == sizeof(byte):
                {
                    byte value = ReadByte();
                    return Unsafe.As<byte, T>(ref value);
                }
                case TypeCode.Int16:
                case TypeCode.Object when Unsafe.SizeOf<T>() == sizeof(short):
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
                case TypeCode.Object when Unsafe.SizeOf<T>() == sizeof(int):
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
                case TypeCode.Object when Unsafe.SizeOf<T>() == sizeof(long):
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
                case TypeCode.DateTime:
                {
                    var value = ReadDateTime();
                    return Unsafe.As<DateTime, T>(ref value);
                }
                case TypeCode.Object when typeof(T) == typeof(SnowflakeId):
                {
                    var value = ReadSnowflakeId();
                    return Unsafe.As<SnowflakeId, T>(ref value);
                }
                case TypeCode.Object when typeof(T) == typeof(Guid):
                {
                    var value = ReadGuid();
                    return Unsafe.As<Guid, T>(ref value);
                }

                case TypeCode.Object:
                {
                    if (BitConverter.IsLittleEndian)
                    {
                        var value = MemoryMarshal.Read<T>(_span[_offset..]);
                        _offset += Unsafe.SizeOf<T>();
                        return value;
                    }

                    throw new ArgumentException(
                        $"Deserialization of {typeof(T)} type is unsupported on this machine");
                }

                default:
                    throw new ArgumentException($"Deserialization of {typeof(T)} type is unsupported");
            }
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

        public bool TryReadUInt64([NotNullWhen(true)] out ulong? value)
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

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanRead(int size)
        {
            return size > 0 && _offset <= _span.Length - size;
        }
    }
}
