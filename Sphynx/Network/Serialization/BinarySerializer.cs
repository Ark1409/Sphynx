// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using Sphynx.Core;
using Sphynx.Storage;
using Version = Sphynx.Core.Version;

namespace Sphynx.Network.Serialization
{
    /// <summary>
    /// Serializes primitive types into a <see cref="Span{T}"/>, while also tracking the number of
    /// bytes currently written.
    /// </summary>
    public ref struct BinarySerializer
    {
        // Store "local" reference to default text encoding
        internal static readonly Encoding StringEncoding = new UTF8Encoding(false);

        private readonly IBufferWriter<byte> _buffer;

        private readonly Span<byte> _span;
        private long _bytesWritten;

        public readonly long BytesWritten
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _bytesWritten;
        }

        public readonly bool HasSpan
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _buffer == null;
        }

        public readonly bool HasBuffer
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _buffer != null;
        }

        /// <summary>
        /// Returns the underlying <see cref="IBufferWriter{T}"/>, if it exists.
        /// </summary>
        public readonly IBufferWriter<byte>? Buffer
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _buffer;
        }

        public BinarySerializer(IBufferWriter<byte> buffer)
        {
            _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            _span = default;
            _bytesWritten = 0;
        }

        public BinarySerializer(Span<byte> span)
        {
            _buffer = null!;
            _span = span;
            _bytesWritten = 0;
        }

        #region Sizing

        /// <summary>
        /// Returns the exact serialization size (in bytes) of the specified unmanaged <typeparamref name="T"/>.
        /// For an unmanaged type, the maximum size is equal to the exact size.
        /// </summary>
        /// <typeparam name="T">The unmanaged type.</typeparam>
        /// <returns>The exact serialization size of specified <typeparamref name="T"/>.</returns>
        public static int SizeOf<T>() where T : unmanaged
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
                    return Unsafe.SizeOf<T>();
                case TypeCode.DateTime:
                    return SizeOf<long>();
                case TypeCode.Object when typeof(T) == typeof(Guid):
                    return Unsafe.SizeOf<T>();
                case TypeCode.Object when typeof(T) == typeof(DateTimeOffset):
                    return 2 * SizeOf<long>();
                case TypeCode.Object when typeof(T) == typeof(SnowflakeId):
                    return SnowflakeId.SIZE;
                case TypeCode.Object when typeof(T) == typeof(Version):
                    return SizeOf<int>();

                default:
                {
                    if (Unsafe.SizeOf<T>() == sizeof(byte))
                        goto case TypeCode.Byte;

                    throw new ArgumentException($"The retrieval of the size of {typeof(T)} is unsupported");
                }
            }
        }

        /// <summary>
        /// Returns the maximum serialization size (in bytes) of the specified unmanaged <typeparamref name="T"/>.
        /// For an unmanaged type, the maximum size is equal to the exact size.
        /// </summary>
        /// <typeparam name="T">The unmanaged type.</typeparam>
        /// <returns>The maximum serialization size of specified <typeparamref name="T"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MaxSizeOf<T>() where T : unmanaged
        {
            return SizeOf<T>();
        }

        /// <summary>
        /// Returns the exact serialization size (in bytes) of the specified <paramref name="str"/>.
        /// </summary>
        /// <remarks>
        /// This performs double-pass on the data and should be avoided.
        /// </remarks>
        /// <param name="str">The string to serialize.</param>
        /// <returns>The exact serialization size of specified <paramref name="str"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SizeOf(string? str)
        {
            int charCount = str?.Length ?? 0;
            int byteCount = (charCount == 0 ? charCount : StringEncoding.GetByteCount(str!));
            return SizeOf<int>() + byteCount;
        }

        /// <summary>
        /// Returns the maximum serialization size (in bytes) of the specified <paramref name="str"/>.
        /// </summary>
        /// <param name="str">The string to check the serialization size for.</param>
        /// <returns>The maximum serialization size of specified <paramref name="str"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MaxSizeOf(string? str)
        {
            int charCount = str?.Length ?? 0;
            return SizeOf<int>() + StringEncoding.GetMaxByteCount(charCount);
        }

        /// <inheritdoc cref="SizeOf(string?)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SizeOf(ReadOnlySpan<char> str)
        {
            int byteCount = (str.Length == 0 ? str.Length : StringEncoding.GetByteCount(str));
            return SizeOf<int>() + byteCount;
        }

        /// <inheritdoc cref="MaxSizeOf(string?)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MaxSizeOf(ReadOnlySpan<char> str)
        {
            return SizeOf<int>() + StringEncoding.GetMaxByteCount(str.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SizeOf<T>(ICollection<T> collection) where T : unmanaged
        {
            return SizeOf<int>() + collection.Count * SizeOf<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MaxSizeOf<T>(ICollection<T> collection) where T : unmanaged
        {
            return SizeOf(collection);
        }

        public static int SizeOf(ICollection<string?> collection)
        {
            int size = SizeOf<int>();

            foreach (string? item in collection)
            {
                size += SizeOf(item);
            }

            return size;
        }

        public static int MaxSizeOf(ICollection<string?> collection)
        {
            int size = SizeOf<int>();

            foreach (string? item in collection)
            {
                size += MaxSizeOf(item);
            }

            return size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SizeOf<TKey, TValue>(IDictionary<TKey, TValue> dictionary)
            where TKey : unmanaged
            where TValue : unmanaged
        {
            return SizeOf<int>() + dictionary.Count * (SizeOf<TKey>() + SizeOf<TValue>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MaxSizeOf<TKey, TValue>(IDictionary<TKey, TValue> dictionary)
            where TKey : unmanaged
            where TValue : unmanaged
        {
            return SizeOf(dictionary);
        }

        public static int SizeOf<TValue>(IDictionary<string, TValue> dictionary)
            where TValue : unmanaged
        {
            int size = SizeOf<int>() + dictionary.Count * SizeOf<TValue>();

            foreach (var kvp in dictionary)
            {
                size += SizeOf(kvp.Key);
            }

            return size;
        }

        public static int MaxSizeOf<TValue>(IDictionary<string, TValue> dictionary)
            where TValue : unmanaged
        {
            int size = SizeOf<int>() + dictionary.Count * MaxSizeOf<TValue>();

            foreach (var kvp in dictionary)
            {
                size += MaxSizeOf(kvp.Key);
            }

            return size;
        }

        public static int SizeOf<TKey>(IDictionary<TKey, string?> dictionary)
            where TKey : unmanaged
        {
            int size = SizeOf<int>() + dictionary.Count * SizeOf<TKey>();

            foreach (var kvp in dictionary)
            {
                size += SizeOf(kvp.Value);
            }

            return size;
        }

        public static int MaxSizeOf<TKey>(IDictionary<TKey, string?> dictionary)
            where TKey : unmanaged
        {
            int size = SizeOf<int>() + dictionary.Count * MaxSizeOf<TKey>();

            foreach (var kvp in dictionary)
            {
                size += MaxSizeOf(kvp.Value);
            }

            return size;
        }

        public static int SizeOf(IDictionary<string, string?> dictionary)
        {
            int size = SizeOf<int>();

            foreach (var kvp in dictionary)
            {
                size += SizeOf(kvp.Key);
                size += SizeOf(kvp.Value);
            }

            return size;
        }

        public static int MaxSizeOf(IDictionary<string, string?> dictionary)
        {
            int size = SizeOf<int>();

            foreach (var kvp in dictionary)
            {
                size += MaxSizeOf(kvp.Key);
                size += MaxSizeOf(kvp.Value);
            }

            return size;
        }

        #endregion

        #region Collections

        public void WriteDictionary(IDictionary<string, string?> dictionary)
        {
            WriteInt32(dictionary.Count);

            foreach (var kvp in dictionary)
            {
                WriteString(kvp.Key);
                WriteString(kvp.Value);
            }
        }

        public void WriteDictionary<TKey>(IDictionary<TKey, string?> dictionary)
            where TKey : unmanaged
        {
            WriteInt32(dictionary.Count);

            foreach (var kvp in dictionary)
            {
                WriteUnmanaged(kvp.Key);
                WriteString(kvp.Value);
            }
        }

        public void WriteDictionary<TValue>(IDictionary<string, TValue> dictionary)
            where TValue : unmanaged
        {
            WriteInt32(dictionary.Count);

            foreach (var kvp in dictionary)
            {
                WriteString(kvp.Key);
                WriteUnmanaged(kvp.Value);
            }
        }

        public void WriteDictionary<TKey, TValue>(IDictionary<TKey, TValue> dictionary)
            where TKey : unmanaged
            where TValue : unmanaged
        {
            WriteInt32(dictionary.Count);

            foreach (var kvp in dictionary)
            {
                WriteUnmanaged(kvp.Key);
                WriteUnmanaged(kvp.Value);
            }
        }

        public void WriteCollection(ICollection<string?> collection)
        {
            WriteInt32(collection.Count);

            if (collection is string?[] array)
            {
                foreach (string? item in array.AsSpan())
                    WriteString(item);
            }
            else if (collection is List<string?> list)
            {
                foreach (string? str in CollectionsMarshal.AsSpan(list))
                    WriteString(str);
            }
            else if (collection is IList<string?> iList)
            {
                for (int i = 0; i < iList.Count; i++)
                    WriteString(iList[i]);
            }
            else
            {
                foreach (string? item in collection)
                    WriteString(item);
            }
        }

        public void WriteCollection<T>(ICollection<T> collection) where T : unmanaged
        {
            WriteInt32(collection.Count);

            if (collection is T[] array)
            {
                foreach (var item in array.AsSpan())
                    WriteUnmanaged(item);
            }
            else if (collection is List<T> list)
            {
                foreach (var item in CollectionsMarshal.AsSpan(list))
                    WriteUnmanaged(item);
            }
            else if (collection is IList<T> iList)
            {
                for (int i = 0; i < iList.Count; i++)
                    WriteUnmanaged(iList[i]);
            }
            else
            {
                foreach (var item in collection)
                    WriteUnmanaged(item);
            }
        }

        #endregion

        #region Common Types

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVersion(Version id)
        {
            WriteInt32(id.ToInt32());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSnowflakeId(SnowflakeId id)
        {
            var span = HasBuffer ? _buffer.GetSpan(SnowflakeId.SIZE) : _span[(int)_bytesWritten..];

            bool written = id.TryWriteBytes(span);
            Debug.Assert(written);

            _buffer?.Advance(SnowflakeId.SIZE);
            _bytesWritten += SnowflakeId.SIZE;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteGuid(Guid id)
        {
            int size = Unsafe.SizeOf<Guid>();
            var span = HasBuffer ? _buffer.GetSpan(size) : _span[(int)_bytesWritten..];

            bool written = id.TryWriteBytes(span);
            Debug.Assert(written);

            _buffer?.Advance(size);
            _bytesWritten += size;
        }

        public void WriteString(ReadOnlySpan<char> str)
        {
            var span = HasBuffer ? _buffer.GetSpan(sizeof(int) + StringEncoding.GetMaxByteCount(str.Length)) : _span[(int)_bytesWritten..];

            int stringSize = StringEncoding.GetBytes(str, span[sizeof(int)..]);
            BinaryPrimitives.WriteInt32LittleEndian(span[..sizeof(int)], stringSize);

            int bytesWritten = sizeof(int) + stringSize;

            _buffer?.Advance(bytesWritten);
            _bytesWritten += bytesWritten;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteString(string? str)
        {
            if (str is null)
            {
                WriteInt32(-1);
                return;
            }

            WriteString(str.AsSpan());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDateTimeOffset(DateTimeOffset dto)
        {
            WriteInt64(dto.Ticks);
            WriteInt64(dto.Offset.Ticks);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDateTime(DateTime dateTime)
        {
            WriteInt64(dateTime.ToUniversalTime().Ticks);
        }

        #endregion

        #region Primitives

        internal void WriteRaw(in ReadOnlySequence<byte> raw)
        {
            foreach (var segment in raw)
                WriteRaw(segment.Span);
        }

        internal void WriteRaw(ReadOnlySpan<byte> raw)
        {
            if (HasBuffer)
            {
                var span = _buffer.GetSpan(raw.Length);
                raw.CopyTo(span);
                _buffer.Advance(raw.Length);
            }
            else
            {
                raw.CopyTo(_span[(int)_bytesWritten..]);
            }

            _bytesWritten += raw.Length;
        }

        /// <summary>
        /// Serializes unmanaged types into bytes.
        /// </summary>
        /// <param name="value">The unmanaged type to serialize.</param>
        /// <typeparam name="T">The type of unmanaged type.</typeparam>
        /// <exception cref="ArgumentException">If <typeparamref name="T"/> is a user-defined struct of size greater than 1 byte.</exception>
        public void WriteUnmanaged<T>(T value) where T : unmanaged
        {
            var typeCode = Type.GetTypeCode(typeof(T));

            switch (typeCode)
            {
                case TypeCode.Boolean:
                    WriteBool(Unsafe.As<T, bool>(ref value));
                    break;
                case TypeCode.Byte:
                    WriteUInt8(Unsafe.As<T, byte>(ref value));
                    break;
                case TypeCode.Int16:
                    WriteInt16(Unsafe.As<T, short>(ref value));
                    break;
                case TypeCode.UInt16:
                    WriteUInt16(Unsafe.As<T, ushort>(ref value));
                    break;
                case TypeCode.Int32:
                    WriteInt32(Unsafe.As<T, int>(ref value));
                    break;
                case TypeCode.UInt32:
                    WriteUInt32(Unsafe.As<T, uint>(ref value));
                    break;
                case TypeCode.Int64:
                    WriteInt64(Unsafe.As<T, long>(ref value));
                    break;
                case TypeCode.UInt64:
                    WriteUInt64(Unsafe.As<T, ulong>(ref value));
                    break;
                case TypeCode.Single:
                    WriteSingle(Unsafe.As<T, float>(ref value));
                    break;
                case TypeCode.Double:
                    WriteDouble(Unsafe.As<T, double>(ref value));
                    break;
                case TypeCode.DateTime:
                    WriteDateTime(Unsafe.As<T, DateTime>(ref value));
                    break;
                case TypeCode.Object when typeof(T) == typeof(SnowflakeId):
                    WriteSnowflakeId(Unsafe.As<T, SnowflakeId>(ref value));
                    break;
                case TypeCode.Object when typeof(T) == typeof(Version):
                    WriteVersion(Unsafe.As<T, Version>(ref value));
                    break;
                case TypeCode.Object when typeof(T) == typeof(DateTimeOffset):
                    WriteDateTimeOffset(Unsafe.As<T, DateTimeOffset>(ref value));
                    break;
                case TypeCode.Object when typeof(T) == typeof(Guid):
                    WriteGuid(Unsafe.As<T, Guid>(ref value));
                    break;

                default:
                {
                    if (Unsafe.SizeOf<T>() == sizeof(byte))
                        goto case TypeCode.Byte;

                    throw new ArgumentException($"Serialization of {typeof(T)} type is unsupported");
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteEnum<T>(T value) where T : unmanaged, Enum
        {
            WriteUnmanaged(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBool(bool value)
        {
            WriteUInt8(value ? (byte)1 : (byte)0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt8(sbyte value)
        {
            WriteUInt8(Unsafe.As<sbyte, byte>(ref value));
        }

        public void WriteUInt8(byte value)
        {
            if (HasBuffer)
            {
                var span = _buffer.GetSpan(sizeof(byte));
                span[0] = value;
                _buffer.Advance(sizeof(byte));
            }
            else
            {
                _span[(int)_bytesWritten] = value;
            }

            _bytesWritten += sizeof(byte);
        }

        public void WriteUInt16(ushort value)
        {
            if (HasBuffer)
            {
                var span = _buffer.GetSpan(sizeof(ushort));
                BinaryPrimitives.WriteUInt16LittleEndian(span, value);
                _buffer.Advance(sizeof(ushort));
            }
            else
            {
                BinaryPrimitives.WriteUInt16LittleEndian(_span[(int)_bytesWritten..], value);
            }

            _bytesWritten += sizeof(ushort);
        }

        public void WriteInt16(short value)
        {
            if (HasBuffer)
            {
                var span = _buffer.GetSpan(sizeof(short));
                BinaryPrimitives.WriteInt16LittleEndian(span, value);
                _buffer.Advance(sizeof(short));
            }
            else
            {
                BinaryPrimitives.WriteInt16LittleEndian(_span[(int)_bytesWritten..], value);
            }

            _bytesWritten += sizeof(short);
        }

        public void WriteUInt32(uint value)
        {
            if (HasBuffer)
            {
                var span = _buffer.GetSpan(sizeof(uint));
                BinaryPrimitives.WriteUInt32LittleEndian(span, value);
                _buffer.Advance(sizeof(uint));
            }
            else
            {
                BinaryPrimitives.WriteUInt32LittleEndian(_span[(int)_bytesWritten..], value);
            }

            _bytesWritten += sizeof(uint);
        }

        public void WriteInt32(int value)
        {
            if (HasBuffer)
            {
                var span = _buffer.GetSpan(sizeof(int));
                BinaryPrimitives.WriteInt32LittleEndian(span, value);
                _buffer.Advance(sizeof(int));
            }
            else
            {
                BinaryPrimitives.WriteInt32LittleEndian(_span[(int)_bytesWritten..], value);
            }

            _bytesWritten += sizeof(int);
        }

        public void WriteUInt64(ulong value)
        {
            if (HasBuffer)
            {
                var span = _buffer.GetSpan(sizeof(ulong));
                BinaryPrimitives.WriteUInt64LittleEndian(span, value);
                _buffer.Advance(sizeof(ulong));
            }
            else
            {
                BinaryPrimitives.WriteUInt64LittleEndian(_span[(int)_bytesWritten..], value);
            }

            _bytesWritten += sizeof(ulong);
        }

        public void WriteInt64(long value)
        {
            if (HasBuffer)
            {
                var span = _buffer.GetSpan(sizeof(long));
                BinaryPrimitives.WriteInt64LittleEndian(span, value);
                _buffer.Advance(sizeof(long));
            }
            else
            {
                BinaryPrimitives.WriteInt64LittleEndian(_span[(int)_bytesWritten..], value);
            }

            _bytesWritten += sizeof(long);
        }

        public void WriteSingle(float value)
        {
            if (HasBuffer)
            {
                var span = _buffer.GetSpan(sizeof(float));
                BinaryPrimitives.WriteSingleLittleEndian(span, value);
                _buffer.Advance(sizeof(float));
            }
            else
            {
                BinaryPrimitives.WriteSingleLittleEndian(_span[(int)_bytesWritten..], value);
            }

            _bytesWritten += sizeof(float);
        }

        public void WriteDouble(double value)
        {
            if (HasBuffer)
            {
                var span = _buffer.GetSpan(sizeof(double));
                BinaryPrimitives.WriteDoubleLittleEndian(span, value);
                _buffer.Advance(sizeof(double));
            }
            else
            {
                BinaryPrimitives.WriteDoubleLittleEndian(_span[(int)_bytesWritten..], value);
            }

            _bytesWritten += sizeof(double);
        }

        #endregion
    }
}
