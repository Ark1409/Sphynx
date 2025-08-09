// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using Sphynx.Core;
using Version = Sphynx.Core.Version;

namespace Sphynx.Network.Serialization
{
    /// <summary>
    /// Deserializes primitive types from a <see cref="ReadOnlySpan{T}"/>, while also tracking the number of
    /// bytes currently read.
    /// </summary>
    public ref struct BinaryDeserializer
    {
        // Ensure this is `readonly` so the JIT has a chance at optimizing call paths
        private readonly bool _useSequence;

        // For single-segment sequences, we want to use _span, but still need to provide a <see cref="Sequence"/>
        private readonly bool _hasSequence;
        private SequenceReader<byte> _sequence;

        private readonly ReadOnlySpan<byte> _span;
        private int _spanOffset;

        /// <summary>
        /// Returns the read offset into the underlying sequence or span.
        /// </summary>
        public long Offset
        {
            readonly get => _useSequence ? _sequence.Consumed : _spanOffset;
            set
            {
                if (_useSequence)
                {
                    long offset = Offset;

                    if (offset == value)
                        return;

                    if (value > offset)
                        _sequence.Advance(value - offset);
                    else
                        _sequence.Rewind(offset - value);

                    Debug.Assert(_sequence.Consumed == value);
                }
                else
                {
                    if (value < 0 || value > _span.Length)
                        throw new ArgumentOutOfRangeException(nameof(value));

                    _spanOffset = (int)value;
                }
            }
        }

        /// <summary>
        /// Returns the total number of bytes which can be read from either the
        /// underlying span or the underlying <see cref="Sequence"/>;
        /// the maximum value of <see cref="Offset"/>.
        /// </summary>
        public long Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _useSequence ? _sequence.Length : _span.Length;
        }

        /// <summary>
        /// Returns the underlying span, if it exists; else the span that contains
        /// the current segment in the <see cref="Sequence"/>.
        /// </summary>
        public readonly ReadOnlySpan<byte> CurrentSpan
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _useSequence ? _sequence.CurrentSpan : _span;
        }

        /// <summary>
        /// Returns the underlying span, starting from the <see cref="Offset"/>, if it exists; else the
        /// unread portion of the span that contains the current segment in the <see cref="Sequence"/>.
        /// </summary>
        public readonly ReadOnlySpan<byte> UnreadSpan
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _useSequence ? _sequence.UnreadSpan : _span[_spanOffset..];
        }

        /// <summary>
        /// Returns the underlying sequence, if it exists.
        /// </summary>
        public readonly ReadOnlySequence<byte> Sequence
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _sequence.Sequence;
        }

        /// <summary>
        /// Returns the unread portion of the underlying sequence, if it exists.
        /// </summary>
        public readonly ReadOnlySequence<byte> UnreadSequence
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _sequence.UnreadSequence;
        }

        /// <summary>
        /// Whether an underlying <see cref="Sequence"/> exists for this reader.
        /// </summary>
        public readonly bool HasSequence
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _hasSequence;
        }

        public BinaryDeserializer(in ReadOnlySequence<byte> sequence) : this()
        {
            // For all intents and purposes, reading directly from a span is currently faster than parsing
            // a single-segment sequence
            if (sequence.IsSingleSegment)
            {
                _useSequence = false;
                _span = sequence.FirstSpan;
                _spanOffset = 0;
            }
            else
            {
                _useSequence = true;
            }

            // In both cases, we'll still store a refence to the passed sequence
            _sequence = new SequenceReader<byte>(sequence);
            _hasSequence = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BinaryDeserializer(ReadOnlyMemory<byte> memory) : this(new ReadOnlySequence<byte>(memory))
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BinaryDeserializer(ReadOnlySpan<byte> span) : this()
        {
            _useSequence = false;
            _span = span;
            _spanOffset = 0;
        }

        #region Dictionaries

        public bool TryReadDictionary([NotNullWhen(true)] out Dictionary<string, string?>? dictionary)
        {
            long oldOffset = Offset;

            if (!TryReadInt32(out int size))
            {
                dictionary = default;
                return false;
            }

            // Try and catch it early
            if (!CanRead(size * (BinarySerializer.SizeOf(string.Empty) * 2)))
            {
                Offset = oldOffset;
                dictionary = default;
                return false;
            }

            dictionary = CreateDictionary<string, string?>(size);

            for (int i = 0; i < size; i++)
            {
                if (!TryReadString(out string? key) || !TryReadString(out string? value))
                {
                    Offset = oldOffset;
                    dictionary = null;
                    return false;
                }

                dictionary.Add(key!, value);
            }

            return true;
        }

        public void ReadDictionary(out Dictionary<string, string?> dictionary)
        {
            int size = ReadInt32();
            dictionary = CreateDictionary<string, string?>(size);

            for (int i = 0; i < size; i++)
            {
                string? key = ReadString();
                string? value = ReadString();
                dictionary.Add(key!, value);
            }
        }

        public bool TryReadDictionary<TKey>([NotNullWhen(true)] out Dictionary<TKey, string?>? dictionary)
            where TKey : unmanaged
        {
            long oldOffset = Offset;

            if (!TryReadInt32(out int size))
            {
                dictionary = default;
                return false;
            }

            // Try and catch it early
            if (!CanRead(size * (BinarySerializer.SizeOf<TKey>() + BinarySerializer.SizeOf(string.Empty))))
            {
                Offset = oldOffset;
                dictionary = default;
                return false;
            }

            dictionary = CreateDictionary<TKey, string?>(size);

            for (int i = 0; i < size; i++)
            {
                if (!TryReadUnmanaged<TKey>(out var key) || !TryReadString(out string? value))
                {
                    Offset = oldOffset;
                    dictionary = null;
                    return false;
                }

                dictionary.Add(key, value);
            }

            return true;
        }

        public void ReadDictionary<TKey>(out Dictionary<TKey, string?> dictionary)
            where TKey : unmanaged
        {
            int size = ReadInt32();
            dictionary = CreateDictionary<TKey, string?>(size);

            for (int i = 0; i < size; i++)
            {
                var key = ReadUnmanaged<TKey>();
                string? value = ReadString();
                dictionary.Add(key, value);
            }
        }

        public bool TryReadDictionary<TValue>([NotNullWhen(true)] out Dictionary<string, TValue>? dictionary)
            where TValue : unmanaged
        {
            long oldOffset = Offset;

            if (!TryReadInt32(out int size))
            {
                dictionary = default;
                return false;
            }

            // Try and catch it early
            if (!CanRead(size * (BinarySerializer.SizeOf(string.Empty) + BinarySerializer.SizeOf<TValue>())))
            {
                Offset = oldOffset;
                dictionary = default;
                return false;
            }

            dictionary = CreateDictionary<string, TValue>(size);

            for (int i = 0; i < size; i++)
            {
                if (!TryReadString(out string? key) || !TryReadUnmanaged<TValue>(out var value))
                {
                    Offset = oldOffset;
                    dictionary = null;
                    return false;
                }

                dictionary.Add(key!, value);
            }

            return true;
        }

        public void ReadDictionary<TValue>(out Dictionary<string, TValue> dictionary)
            where TValue : unmanaged
        {
            int size = ReadInt32();
            dictionary = CreateDictionary<string, TValue>(size);

            for (int i = 0; i < size; i++)
            {
                string? key = ReadString();
                var value = ReadUnmanaged<TValue>();
                dictionary.Add(key!, value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadDictionary<TKey, TValue>([NotNullWhen(true)] out Dictionary<TKey, TValue>? dictionary)
            where TKey : unmanaged
            where TValue : unmanaged
        {
            return TryReadDictionary<TKey, TValue, Dictionary<TKey, TValue>>(out dictionary!);
        }

        public bool TryReadDictionary<TKey, TValue, TDictionary>([NotNullWhen(true)] out TDictionary? dictionary)
            where TKey : unmanaged
            where TValue : unmanaged
            where TDictionary : IDictionary<TKey, TValue>, new()
        {
            long oldOffset = Offset;

            if (!TryReadInt32(out int size))
            {
                dictionary = default;
                return false;
            }

            // Try and catch it early
            if (!CanRead(size * (BinarySerializer.SizeOf<TKey>() + BinarySerializer.SizeOf<TValue>())))
            {
                Offset = oldOffset;
                dictionary = default;
                return false;
            }

            dictionary = CreateDictionary<TKey, TValue, TDictionary>(size);

            for (int i = 0; i < size; i++)
            {
                if (!TryReadUnmanaged<TKey>(out var key) || !TryReadUnmanaged<TValue>(out var value))
                {
                    Offset = oldOffset;
                    dictionary = default;
                    return false;
                }

                dictionary.Add(key, value);
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
        public Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>()
            where TKey : unmanaged
            where TValue : unmanaged
        {
            return ReadDictionary<TKey, TValue, Dictionary<TKey, TValue>>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Dictionary<TKey, TValue> CreateDictionary<TKey, TValue>(int size) where TKey : notnull =>
            CreateDictionary<TKey, TValue, Dictionary<TKey, TValue>>(size);

        private static TDictionary CreateDictionary<TKey, TValue, TDictionary>(int size)
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

        public bool TryReadArray(out string?[]? array)
        {
            long oldOffset = Offset;

            if (!TryReadInt32(out int size))
            {
                array = null;
                return false;
            }

            array = size == 0 ? Array.Empty<string>() : new string?[size];

            for (int i = 0; i < size; i++)
            {
                if (!TryReadString(out string? str))
                {
                    Offset = oldOffset;
                    array = null;
                    return false;
                }

                array[i] = str;
            }

            return true;
        }

        public string?[] ReadArray()
        {
            int size = ReadInt32();
            string?[] array = size == 0 ? Array.Empty<string>() : new string?[size];

            for (int i = 0; i < size; i++)
            {
                array[i] = ReadString();
            }

            return array;
        }

        public bool TryReadArray<T>([NotNullWhen(true)] out T[]? array) where T : unmanaged
        {
            long oldOffset = Offset;

            if (!TryReadInt32(out int size))
            {
                array = null;
                return false;
            }

            // Try and catch it early
            if (!CanRead(size * BinarySerializer.SizeOf<T>()))
            {
                Offset = oldOffset;
                array = null;
                return false;
            }

            array = size == 0 ? Array.Empty<T>() : new T[size];

            for (int i = 0; i < size; i++)
            {
                if (!TryReadUnmanaged<T>(out var item))
                {
                    Offset = oldOffset;
                    array = null;
                    return false;
                }

                array[i] = item;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadStringList([NotNullWhen(true)] out List<string?>? list)
        {
            return TryReadCollection(out list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<string?> ReadStringList()
        {
            return ReadCollection<List<string?>>();
        }

        public bool TryReadCollection<TCollection>([NotNullWhen(true)] out TCollection? collection)
            where TCollection : ICollection<string?>, new()
        {
            long oldOffset = Offset;

            if (!TryReadInt32(out int size))
            {
                collection = default;
                return false;
            }

            collection = CreateCollection<string?, TCollection>(size);

            for (int i = 0; i < size; i++)
            {
                if (!TryReadString(out string? str))
                {
                    Offset = oldOffset;
                    collection = default;
                    return false;
                }

                collection.Add(str);
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TCollection ReadCollection<TCollection>() where TCollection : ICollection<string?>, new()
        {
            int size = ReadInt32();
            var collection = CreateCollection<string?, TCollection>(size);

            for (int i = 0; i < size; i++)
                collection.Add(ReadString());

            return collection;
        }

        public bool TryReadCollection<T, TCollection>([NotNullWhen(true)] out TCollection? collection)
            where T : unmanaged
            where TCollection : ICollection<T>, new()
        {
            long oldOffset = Offset;

            if (!TryReadInt32(out int size))
            {
                collection = default;
                return false;
            }

            // Try and catch it early
            if (!CanRead(size * BinarySerializer.SizeOf<T>()))
            {
                Offset = oldOffset;
                collection = default;
                return false;
            }

            collection = CreateCollection<T, TCollection>(size);

            for (int i = 0; i < size; i++)
            {
                if (!TryReadUnmanaged<T>(out var item))
                {
                    Offset = oldOffset;
                    collection = default;
                    return false;
                }

                collection.Add(item);
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadList<T>([NotNullWhen(true)] out List<T>? list) where T : unmanaged
        {
            return TryReadCollection<T, List<T>>(out list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<T> ReadList<T>() where T : unmanaged
        {
            return ReadCollection<T, List<T>>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        private static TCollection CreateCollection<T, TCollection>(int size)
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

        // ReSharper disable once InconsistentNaming
        private static readonly int GuidSize = Unsafe.SizeOf<Guid>();

        public bool TryReadVersion(out Version version)
        {
            if (!TryReadInt32(out int value))
            {
                version = default;
                return false;
            }

            version = Version.FromInt32(value);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Version ReadVersion()
        {
            return Version.FromInt32(ReadInt32());
        }

        public bool TryReadSnowflakeId(out SnowflakeId id)
        {
            if (!CanRead(SnowflakeId.SIZE))
            {
                id = default;
                return false;
            }

            if (_useSequence)
            {
                Span<byte> bytes = stackalloc byte[SnowflakeId.SIZE];
                _sequence.TryCopyTo(bytes);

                id = new SnowflakeId(_span.Slice(_spanOffset, SnowflakeId.SIZE));
                _sequence.Advance(SnowflakeId.SIZE);
            }
            else
            {
                id = new SnowflakeId(_span.Slice(_spanOffset, SnowflakeId.SIZE));
                _spanOffset += SnowflakeId.SIZE;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SnowflakeId ReadSnowflakeId()
        {
            if (!TryReadSnowflakeId(out var id))
                throw ThrowReadException(typeof(SnowflakeId));

            return id;
        }

        public bool TryReadGuid(out Guid guid)
        {
            if (!CanRead(GuidSize))
            {
                guid = default;
                return false;
            }

            if (_useSequence)
            {
                Debug.Assert(GuidSize <= 16);

                Span<byte> bytes = stackalloc byte[GuidSize];
                _sequence.TryCopyTo(bytes);

                guid = new Guid(bytes.Slice(_spanOffset, GuidSize));
                _sequence.Advance(GuidSize);
            }
            else
            {
                guid = new Guid(_span.Slice(_spanOffset, GuidSize));
                _spanOffset += GuidSize;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Guid ReadGuid()
        {
            if (!TryReadGuid(out var guid))
                throw ThrowReadException(typeof(Guid));

            return guid;
        }

        public bool TryReadString(Span<char> dest)
        {
            long oldOffset = Offset;

            if (!TryReadInt32(out int size))
                return false;

            if (size == -1)
            {
                if (!dest.IsEmpty)
                {
                    Offset = oldOffset;
                    return false;
                }

                return true;
            }

            // Sanity check
            if (size < -1 || !CanRead(size))
            {
                Offset = oldOffset;
                return false;
            }

            // dest size check
            if (size > BinarySerializer.StringEncoding.GetMaxByteCount(dest.Length))
            {
                Offset = oldOffset;
                return false;
            }

            try
            {
                if (_useSequence)
                {
                    var start = _sequence.Position;

                    int decoded = BinarySerializer.StringEncoding.GetChars(_sequence.Sequence.Slice(start, size), dest);
                    Debug.Assert(decoded == size);

                    _sequence.Advance(size);
                }
                else
                {
                    int decoded = BinarySerializer.StringEncoding.GetChars(_span.Slice(_spanOffset, size), dest);
                    Debug.Assert(decoded == size);

                    _spanOffset += size;
                }
            }
            // Ugly but we can't really do anything else at this point
            catch
            {
                // TODO: Leave dest modified?
                Offset = oldOffset;
                return false;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReadString(Span<char> dest)
        {
            if (!TryReadString(dest))
                throw ThrowReadException(typeof(Span<char>));
        }

        public bool TryReadString(out string? str)
        {
            long oldOffset = Offset;

            if (!TryReadInt32(out int size))
            {
                str = null;
                return false;
            }

            // Indication of null string
            if (size == -1)
            {
                str = null;
                return true;
            }

            // Sanity check
            if (size < -1 || !CanRead(size))
            {
                str = null;
                Offset = oldOffset;
                return false;
            }

            try
            {
                if (_useSequence)
                {
                    var start = _sequence.Position;
                    str = BinarySerializer.StringEncoding.GetString(_sequence.Sequence.Slice(start, size));
                    _sequence.Advance(size);
                }
                else
                {
                    str = BinarySerializer.StringEncoding.GetString(_span.Slice(_spanOffset, size));
                    _spanOffset += size;
                }
            }
            // Ugly but we can't really do anything else at this point
            catch
            {
                Offset = oldOffset;
                str = null;
                return false;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string? ReadString()
        {
            if (!TryReadString(out string? str))
                throw ThrowReadException(typeof(string));

            return str;
        }

        public bool TryReadEnum<T>(out T value) where T : unmanaged, Enum
        {
            if (!CanRead(BinarySerializer.SizeOf<T>()))
            {
                value = default;
                return false;
            }

            value = ReadEnum<T>();
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T ReadEnum<T>() where T : unmanaged, Enum
        {
            return ReadUnmanaged<T>();
        }

        public bool TryReadDateTimeOffset(out DateTimeOffset dto)
        {
            if (!CanRead(BinarySerializer.SizeOf<DateTimeOffset>()))
            {
                dto = default;
                return false;
            }

            dto = ReadDateTimeOffset();
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DateTimeOffset ReadDateTimeOffset()
        {
            return new DateTimeOffset(ReadInt64(), TimeSpan.FromTicks(ReadInt64()));
        }

        public bool TryReadDateTime(out DateTime dateTime)
        {
            if (!CanRead(BinarySerializer.SizeOf<DateTime>()))
            {
                dateTime = default;
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

        #region Primitives

        /// <summary>
        /// Attempts to deserialize unmanaged types from bytes.
        /// </summary>
        /// <param name="value">The unmanaged type to deserialize.</param>
        /// <typeparam name="T">The type of unmanaged type.</typeparam>
        /// <returns>true if the <paramref name="value"/> could be deserialized; false otherwise.</returns>
        public bool TryReadUnmanaged<T>(out T value) where T : unmanaged
        {
            var typeCode = Type.GetTypeCode(typeof(T));
            switch (typeCode)
            {
                case TypeCode.Boolean:
                {
                    bool success = TryReadBool(out bool val);
                    value = Unsafe.As<bool, T>(ref val);
                    return success;
                }
                case TypeCode.Byte:
                {
                    bool success = TryReadUInt8(out byte val);
                    value = Unsafe.As<byte, T>(ref val);
                    return success;
                }
                case TypeCode.Int16:
                case TypeCode.Object when Unsafe.SizeOf<T>() == sizeof(short):
                {
                    bool success = TryReadInt16(out short val);
                    value = Unsafe.As<short, T>(ref val);
                    return success;
                }
                case TypeCode.UInt16:
                {
                    bool success = TryReadUInt16(out ushort val);
                    value = Unsafe.As<ushort, T>(ref val);
                    return success;
                }
                case TypeCode.Int32:
                case TypeCode.Object when Unsafe.SizeOf<T>() == sizeof(int):
                {
                    bool success = TryReadInt32(out int val);
                    value = Unsafe.As<int, T>(ref val);
                    return success;
                }
                case TypeCode.UInt32:
                {
                    bool success = TryReadUInt32(out uint val);
                    value = Unsafe.As<uint, T>(ref val);
                    return success;
                }
                case TypeCode.Int64:
                case TypeCode.Object when Unsafe.SizeOf<T>() == sizeof(long):
                {
                    bool success = TryReadInt64(out long val);
                    value = Unsafe.As<long, T>(ref val);
                    return success;
                }
                case TypeCode.UInt64:
                {
                    bool success = TryReadUInt64(out ulong val);
                    value = Unsafe.As<ulong, T>(ref val);
                    return success;
                }
                case TypeCode.Single:
                {
                    bool success = TryReadSingle(out float val);
                    value = Unsafe.As<float, T>(ref val);
                    return success;
                }
                case TypeCode.Double:
                {
                    bool success = TryReadDouble(out double val);
                    value = Unsafe.As<double, T>(ref val);
                    return success;
                }
                case TypeCode.DateTime:
                {
                    bool success = TryReadDateTime(out var val);
                    value = Unsafe.As<DateTime, T>(ref val);
                    return success;
                }
                case TypeCode.Object when typeof(T) == typeof(SnowflakeId):
                {
                    bool success = TryReadSnowflakeId(out var val);
                    value = Unsafe.As<SnowflakeId, T>(ref val);
                    return success;
                }
                case TypeCode.Object when typeof(T) == typeof(Version):
                {
                    bool success = TryReadVersion(out var val);
                    value = Unsafe.As<Version, T>(ref val);
                    return success;
                }
                case TypeCode.Object when typeof(T) == typeof(DateTimeOffset):
                {
                    bool success = TryReadDateTimeOffset(out var val);
                    value = Unsafe.As<DateTimeOffset, T>(ref val);
                    return success;
                }
                case TypeCode.Object when typeof(T) == typeof(Guid):
                {
                    bool success = TryReadGuid(out var val);
                    value = Unsafe.As<Guid, T>(ref val);
                    return success;
                }

                default:
                {
                    if (Unsafe.SizeOf<T>() == sizeof(byte))
                        goto case TypeCode.Byte;

                    value = default;
                    return false;
                }
            }
        }

        /// <summary>
        /// Deserializes unmanaged types from bytes.
        /// </summary>
        /// <typeparam name="T">The type of unmanaged type.</typeparam>
        /// <returns>The deserialized unmanaged type.</returns>
        /// <exception cref="ArgumentException">If <typeparamref name="T"/> is a user-defined struct
        /// which is not of blittable size, or we are on a big-endian machine and <typeparamref name="T"/>
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
                {
                    byte value = ReadUInt8();
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
                    float value = ReadSingle();
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
                case TypeCode.Object when typeof(T) == typeof(Version):
                {
                    var value = ReadVersion();
                    return Unsafe.As<Version, T>(ref value);
                }
                case TypeCode.Object when typeof(T) == typeof(DateTimeOffset):
                {
                    var value = ReadDateTimeOffset();
                    return Unsafe.As<DateTimeOffset, T>(ref value);
                }
                case TypeCode.Object when typeof(T) == typeof(Guid):
                {
                    var value = ReadGuid();
                    return Unsafe.As<Guid, T>(ref value);
                }

                default:
                {
                    if (Unsafe.SizeOf<T>() == sizeof(byte))
                        goto case TypeCode.Byte;

                    throw new ArgumentException($"Deserialization of {typeof(T)} type is unsupported");
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadBool(out bool value)
        {
            if (!TryReadUInt8(out byte val))
            {
                value = default;
                return false;
            }

            value = val != 0;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadBool()
        {
            return ReadUInt8() != 0;
        }

        public bool TryReadUInt8(out byte value)
        {
            if (!CanRead(sizeof(byte)))
            {
                value = default;
                return false;
            }

            if (_useSequence)
            {
                bool read = _sequence.TryRead(out byte val);
                Debug.Assert(read);

                value = val;
            }
            else
            {
                value = _span[_spanOffset++];
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadUInt8()
        {
            if (!TryReadUInt8(out byte val))
                throw ThrowReadException(typeof(byte));

            return val;
        }

        public bool TryReadUInt16(out ushort value)
        {
            if (!CanRead(sizeof(ushort)))
            {
                value = default;
                return false;
            }

            if (_useSequence)
            {
                bool read = _sequence.TryReadLittleEndian(out short val);
                Debug.Assert(read);

                value = Unsafe.As<short, ushort>(ref val);
            }
            else
            {
                value = BinaryPrimitives.ReadUInt16LittleEndian(_span[_spanOffset..]);
                _spanOffset += sizeof(ushort);
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadUInt16()
        {
            if (!TryReadUInt16(out ushort val))
                throw ThrowReadException(typeof(ushort));

            return val;
        }

        public bool TryReadInt16(out short value)
        {
            if (!CanRead(sizeof(short)))
            {
                value = default;
                return false;
            }

            if (_useSequence)
            {
                bool read = _sequence.TryReadLittleEndian(out short val);
                Debug.Assert(read);

                value = val;
            }
            else
            {
                value = BinaryPrimitives.ReadInt16LittleEndian(_span[_spanOffset..]);
                _spanOffset += sizeof(short);
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private short ReadInt16()
        {
            if (!TryReadInt16(out short val))
                throw ThrowReadException(typeof(short));

            return val;
        }

        public bool TryReadUInt32(out uint value)
        {
            if (!CanRead(sizeof(uint)))
            {
                value = default;
                return false;
            }

            if (_useSequence)
            {
                bool read = _sequence.TryReadLittleEndian(out int val);
                Debug.Assert(read);

                value = Unsafe.As<int, uint>(ref val);
            }
            else
            {
                value = BinaryPrimitives.ReadUInt32LittleEndian(_span[_spanOffset..]);
                _spanOffset += sizeof(uint);
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUInt32()
        {
            if (!TryReadUInt32(out uint val))
                throw ThrowReadException(typeof(uint));

            return val;
        }

        public bool TryReadInt32(out int value)
        {
            if (!CanRead(sizeof(int)))
            {
                value = default;
                return false;
            }

            if (_useSequence)
            {
                bool read = _sequence.TryReadLittleEndian(out int val);
                Debug.Assert(read);

                value = val;
            }
            else
            {
                value = BinaryPrimitives.ReadInt32LittleEndian(_span[_spanOffset..]);
                _spanOffset += sizeof(int);
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt32()
        {
            if (!TryReadInt32(out int val))
                throw ThrowReadException(typeof(int));

            return val;
        }

        public bool TryReadUInt64(out ulong value)
        {
            if (!CanRead(sizeof(ulong)))
            {
                value = default;
                return false;
            }

            if (_useSequence)
            {
                bool read = _sequence.TryReadLittleEndian(out long val);
                Debug.Assert(read);

                value = Unsafe.As<long, ulong>(ref val);
            }
            else
            {
                value = BinaryPrimitives.ReadUInt64LittleEndian(_span[_spanOffset..]);
                _spanOffset += sizeof(ulong);
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadUInt64()
        {
            if (!TryReadUInt64(out ulong val))
                throw ThrowReadException(typeof(ulong));

            return val;
        }

        public bool TryReadInt64(out long value)
        {
            if (!CanRead(sizeof(long)))
            {
                value = default;
                return false;
            }

            if (_useSequence)
            {
                bool read = _sequence.TryReadLittleEndian(out long val);
                Debug.Assert(read);

                value = val;
            }
            else
            {
                value = BinaryPrimitives.ReadInt64LittleEndian(_span[_spanOffset..]);
                _spanOffset += sizeof(long);
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadInt64()
        {
            if (!TryReadInt64(out long val))
                throw ThrowReadException(typeof(long));

            return val;
        }

        public bool TryReadSingle(out float value)
        {
            if (!CanRead(sizeof(float)))
            {
                value = default;
                return false;
            }

            if (_useSequence)
            {
                bool read = _sequence.TryReadLittleEndian(out int val);
                Debug.Assert(read);

                value = BitConverter.Int32BitsToSingle(val);
            }
            else
            {
                value = BinaryPrimitives.ReadSingleLittleEndian(_span[_spanOffset..]);
                _spanOffset += sizeof(float);
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float ReadSingle()
        {
            if (!TryReadSingle(out float val))
                throw ThrowReadException(typeof(float));

            return val;
        }

        public bool TryReadDouble(out double value)
        {
            if (!CanRead(sizeof(double)))
            {
                value = default;
                return false;
            }

            if (_useSequence)
            {
                bool read = _sequence.TryReadLittleEndian(out long val);
                Debug.Assert(read);

                value = BitConverter.Int64BitsToDouble(val);
            }
            else
            {
                value = BinaryPrimitives.ReadDoubleLittleEndian(_span[_spanOffset..]);
                _spanOffset += sizeof(double);
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double ReadDouble()
        {
            if (!TryReadDouble(out double val))
                throw ThrowReadException(typeof(double));

            return val;
        }

        #endregion

        // TODO: Maybe remove this and simply speed-read on non-TryReadXXX methods
        [DoesNotReturn]
        private Exception ThrowReadException(Type type)
        {
            throw new InvalidOperationException($"Could not read {type}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanRead(int size)
        {
            Debug.Assert(size >= 0);

            long remaining = _useSequence ? _sequence.Remaining : _span.Length - _spanOffset;
            return remaining >= size;
        }
    }
}
