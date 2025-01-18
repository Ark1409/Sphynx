// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Sphynx.Core;

namespace Sphynx.Network.Serialization
{
    /// <summary>
    /// Serializes primitive types into a <see cref="Span{T}"/>, while also tracking the number of
    /// bytes currently written.
    /// </summary>
    public ref struct BinarySerializer
    {
        // Store "local" reference to default text encoding
        internal static readonly Encoding StringEncoding = Encoding.UTF8;

        private readonly Span<byte> _span;

        /// <summary>
        /// Returns the write offset into the underlying span.
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        /// Returns the underlying span.
        /// </summary>
        public Span<byte> Span
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _span;
        }

        /// <summary>
        /// Returns the underlying span, starting from the <see cref="Offset"/>.
        /// </summary>
        public Span<byte> CurrentSpan
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _span[Offset..];
        }

        public BinarySerializer(Memory<byte> memory) : this(memory.Span)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BinarySerializer(Span<byte> span)
        {
            _span = span;
            Offset = 0;
        }

        #region Maximum and Exact sizes

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
                case TypeCode.Object when typeof(T) == typeof(Guid):
                case TypeCode.Object when Unsafe.SizeOf<T>() == sizeof(byte):
                case TypeCode.Object when Unsafe.SizeOf<T>() == sizeof(short):
                case TypeCode.Object when Unsafe.SizeOf<T>() == sizeof(int):
                case TypeCode.Object when Unsafe.SizeOf<T>() == sizeof(long):
                case TypeCode.Object when BitConverter.IsLittleEndian:
                    return Unsafe.SizeOf<T>();
                case TypeCode.DateTime:
                    return sizeof(long);
                case TypeCode.Object when typeof(T) == typeof(SnowflakeId):
                    return SnowflakeId.SIZE;

                case TypeCode.Object:
                    throw new ArgumentException(
                        $"The retrieval of the size of {typeof(T)} is unsupported on this machine");

                default:
                    throw new ArgumentException($"The retrieval of the size of {typeof(T)} is unsupported");
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
            return sizeof(int) + byteCount;
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
            return sizeof(int) + StringEncoding.GetMaxByteCount(charCount);
        }

        /// <inheritdoc cref="SizeOf(string?)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SizeOf(ReadOnlySpan<char> str)
        {
            int byteCount = (str.Length == 0 ? str.Length : StringEncoding.GetByteCount(str));
            return sizeof(int) + byteCount;
        }

        /// <inheritdoc cref="MaxSizeOf(string?)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MaxSizeOf(ReadOnlySpan<char> str)
        {
            return sizeof(int) + StringEncoding.GetMaxByteCount(str.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SizeOf<T>(ICollection<T> collection) where T : unmanaged
        {
            return sizeof(int) + collection.Count * SizeOf<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MaxSizeOf<T>(ICollection<T> collection) where T : unmanaged
        {
            return SizeOf(collection);
        }

        public static int SizeOf(ICollection<string?> collection)
        {
            int size = sizeof(int);

            foreach (string? item in collection)
            {
                size += SizeOf(item);
            }

            return size;
        }

        public static int MaxSizeOf(ICollection<string?> collection)
        {
            int size = sizeof(int);

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
            return sizeof(int) + dictionary.Count * (SizeOf<TKey>() + SizeOf<TValue>());
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
            int size = sizeof(int) + dictionary.Count * SizeOf<TValue>();

            foreach (var kvp in dictionary)
            {
                size += SizeOf(kvp.Key);
            }

            return size;
        }

        public static int MaxSizeOf<TValue>(IDictionary<string, TValue> dictionary)
            where TValue : unmanaged
        {
            int size = sizeof(int) + dictionary.Count * MaxSizeOf<TValue>();

            foreach (var kvp in dictionary)
            {
                size += MaxSizeOf(kvp.Key);
            }

            return size;
        }

        public static int SizeOf<TKey>(IDictionary<TKey, string?> dictionary)
            where TKey : unmanaged
        {
            int size = sizeof(int) + dictionary.Count * SizeOf<TKey>();

            foreach (var kvp in dictionary)
            {
                size += SizeOf(kvp.Value);
            }

            return size;
        }

        public static int MaxSizeOf<TKey>(IDictionary<TKey, string?> dictionary)
            where TKey : unmanaged
        {
            int size = sizeof(int) + dictionary.Count * MaxSizeOf<TKey>();

            foreach (var kvp in dictionary)
            {
                size += MaxSizeOf(kvp.Value);
            }

            return size;
        }

        public static int SizeOf(IDictionary<string, string?> dictionary)
        {
            int size = sizeof(int);

            foreach (var kvp in dictionary)
            {
                size += SizeOf(kvp.Key);
                size += SizeOf(kvp.Value);
            }

            return size;
        }

        public static int MaxSizeOf(IDictionary<string, string?> dictionary)
        {
            int size = sizeof(int);

            foreach (var kvp in dictionary)
            {
                size += MaxSizeOf(kvp.Key);
                size += MaxSizeOf(kvp.Value);
            }

            return size;
        }

        #endregion

        #region Dictionaries

        public bool TryWriteDictionary(IDictionary<string, string?> dictionary)
        {
            if (!CanWrite(MaxSizeOf(dictionary)) && !CanWrite(SizeOf(dictionary)))
                return false;

            int fallbackOffset = Offset;

            // Guaranteed to succeed due to size check
            WriteInt32(dictionary.Count);

            foreach (var kvp in dictionary)
            {
                if (!TryWriteString(kvp.Key) || !TryWriteString(kvp.Value))
                {
                    Offset = fallbackOffset;
                    return false;
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDictionary(IDictionary<string, string?> dictionary)
        {
            WriteInt32(dictionary.Count);

            foreach (var kvp in dictionary)
            {
                WriteString(kvp.Key);
                WriteString(kvp.Value);
            }
        }

        public bool TryWriteDictionary<TKey>(IDictionary<TKey, string?> dictionary)
            where TKey : unmanaged
        {
            if (!CanWrite(MaxSizeOf(dictionary)) && !CanWrite(SizeOf(dictionary)))
                return false;

            int fallbackOffset = Offset;

            // Guaranteed to succeed due to size check
            WriteInt32(dictionary.Count);

            foreach (var kvp in dictionary)
            {
                if (!TryWriteUnmanaged(kvp.Key) || !TryWriteString(kvp.Value))
                {
                    Offset = fallbackOffset;
                    return false;
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        public bool TryWriteDictionary<TValue>(IDictionary<string, TValue> dictionary)
            where TValue : unmanaged
        {
            if (!CanWrite(MaxSizeOf(dictionary)) && !CanWrite(SizeOf(dictionary)))
                return false;

            int fallbackOffset = Offset;

            // Guaranteed to succeed due to size check
            WriteInt32(dictionary.Count);

            foreach (var kvp in dictionary)
            {
                if (!TryWriteString(kvp.Key) || !TryWriteUnmanaged(kvp.Value))
                {
                    Offset = fallbackOffset;
                    return false;
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        public bool TryWriteDictionary<TKey, TValue>(IDictionary<TKey, TValue> dictionary)
            where TKey : unmanaged
            where TValue : unmanaged
        {
            if (!CanWrite(MaxSizeOf(dictionary)) && !CanWrite(SizeOf(dictionary)))
                return false;

            int fallbackOffset = Offset;

            // Guaranteed to succeed due to size check
            WriteInt32(dictionary.Count);

            foreach (var kvp in dictionary)
            {
                if (!TryWriteUnmanaged(kvp.Key) || !TryWriteUnmanaged(kvp.Value))
                {
                    Offset = fallbackOffset;
                    return false;
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        #endregion

        #region Collections

        public bool TryWriteCollection(ICollection<string?> collection)
        {
            if (!CanWrite(MaxSizeOf(collection)) && !CanWrite(SizeOf(collection)))
                return false;

            WriteCollection(collection);
            return true;
        }

        public void WriteCollection(ICollection<string?> collection)
        {
            WriteInt32(collection.Count);

            // Avoid unnecessary allocation
            if (collection is IList<string?> list)
            {
                for (int i = 0; i < list.Count; i++)
                    WriteString(list[i]);
            }
            else
            {
                foreach (string? item in collection)
                    WriteString(item);
            }
        }

        public bool TryWriteCollection<T>(ICollection<T> collection) where T : unmanaged
        {
            if (!CanWrite(MaxSizeOf(collection)) && !CanWrite(SizeOf(collection)))
                return false;

            int fallbackOffset = Offset;

            // Guaranteed to succeed due to size check
            WriteInt32(collection.Count);

            // Avoid unnecessary allocation
            if (collection is T[] array)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    if (!TryWriteUnmanaged(array[i]))
                    {
                        Offset = fallbackOffset;
                        return false;
                    }
                }
            }
            else if (collection is IList<T> list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (!TryWriteUnmanaged(list[i]))
                    {
                        Offset = fallbackOffset;
                        return false;
                    }
                }
            }
            else
            {
                foreach (var item in collection)
                {
                    if (!TryWriteUnmanaged(item))
                    {
                        Offset = fallbackOffset;
                        return false;
                    }
                }
            }

            return true;
        }

        public void WriteCollection<T>(ICollection<T> collection) where T : unmanaged
        {
            WriteInt32(collection.Count);

            // Avoid unnecessary allocation
            if (collection is T[] array)
            {
                for (int i = 0; i < array.Length; i++)
                    WriteUnmanaged(array[i]);
            }
            else if (collection is IList<T> list)
            {
                for (int i = 0; i < list.Count; i++)
                    WriteUnmanaged(list[i]);
            }
            else
            {
                foreach (var item in collection)
                    WriteUnmanaged(item);
            }
        }

        #endregion

        #region Common Types

        public bool TryWriteSnowflakeId(SnowflakeId id)
        {
            if (!CanWrite(SnowflakeId.SIZE))
                return false;

            WriteSnowflakeId(id);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSnowflakeId(SnowflakeId id)
        {
            bool written = id.TryWriteBytes(_span[Offset..]);
            Debug.Assert(written);

            Offset += SnowflakeId.SIZE;
        }

        public bool TryWriteGuid(Guid id)
        {
            if (!CanWrite(Unsafe.SizeOf<Guid>()))
                return false;

            WriteGuid(id);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteGuid(Guid id)
        {
            bool written = id.TryWriteBytes(_span[Offset..]);
            Debug.Assert(written);

            Offset += Unsafe.SizeOf<Guid>();
        }

        public bool TryWriteString(ReadOnlySpan<char> str)
        {
            if (!CanWrite(str.Length))
                return false;

            if (!CanWrite(MaxSizeOf(str)) && !CanWrite(SizeOf(str)))
                return false;

            WriteString(str);
            return true;
        }

        public void WriteString(ReadOnlySpan<char> str)
        {
            int size = StringEncoding.GetBytes(str, _span[(Offset + sizeof(int))..]);
            WriteInt32(size);

            Offset += size;
        }

        public bool TryWriteString(string? str)
        {
            if (!CanWrite(str?.Length ?? 0))
                return false;

            if (!CanWrite(MaxSizeOf(str)) && !CanWrite(SizeOf(str)))
                return false;

            WriteString(str);
            return true;
        }

        public void WriteString(string? str)
        {
            int size = StringEncoding.GetBytes(str ?? string.Empty, _span[(Offset + sizeof(int))..]);
            WriteInt32(size);

            Offset += size;
        }

        public bool TryWriteDateTime(DateTime dateTime)
        {
            if (!CanWrite(SizeOf<DateTime>()))
                return false;

            WriteDateTime(dateTime);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDateTime(DateTime dateTime)
        {
            WriteInt64(dateTime.ToUniversalTime().Ticks);
        }

        #endregion

        #region Primitives

        /// <summary>
        /// Attempts to serialize unmanaged types into bytes.
        /// </summary>
        /// <param name="value">The unmanaged type to serialize.</param>
        /// <typeparam name="T">The type of unmanaged type.</typeparam>
        /// <returns>true if the <paramref name="value"/> could be serialized; false otherwise.</returns>
        public bool TryWriteUnmanaged<T>(T value) where T : unmanaged
        {
            var typeCode = Type.GetTypeCode(typeof(T));

            switch (typeCode)
            {
                case TypeCode.Boolean:
                    return TryWriteBool(Unsafe.As<T, bool>(ref value));
                case TypeCode.Byte:
                    return TryWriteByte(Unsafe.As<T, byte>(ref value));
                case TypeCode.Int16:
                    return TryWriteInt16(Unsafe.As<T, short>(ref value));
                case TypeCode.UInt16:
                    return TryWriteUInt16(Unsafe.As<T, ushort>(ref value));
                case TypeCode.Int32:
                    return TryWriteInt32(Unsafe.As<T, int>(ref value));
                case TypeCode.UInt32:
                    return TryWriteUInt32(Unsafe.As<T, uint>(ref value));
                case TypeCode.Int64:
                    return TryWriteInt64(Unsafe.As<T, long>(ref value));
                case TypeCode.UInt64:
                    return TryWriteUInt64(Unsafe.As<T, ulong>(ref value));
                case TypeCode.Single:
                    return TryWriteFloat(Unsafe.As<T, float>(ref value));
                case TypeCode.Double:
                    return TryWriteDouble(Unsafe.As<T, double>(ref value));
                case TypeCode.DateTime:
                    return TryWriteDateTime(Unsafe.As<T, DateTime>(ref value));
                case TypeCode.Object when typeof(T) == typeof(SnowflakeId):
                    return TryWriteSnowflakeId(Unsafe.As<T, SnowflakeId>(ref value));
                case TypeCode.Object when typeof(T) == typeof(Guid):
                    return TryWriteGuid(Unsafe.As<T, Guid>(ref value));

                // For any other user-defined struct, we will try and marshal it directly. This should always
                // be possible if the struct is of blittable size, but we can also add in support for writing
                // arbitrarily-sized ones if we are in little endian.
                case TypeCode.Object:
                {
                    int size = Unsafe.SizeOf<T>();

                    switch (size)
                    {
                        case sizeof(byte):
                            return TryWriteByte(Unsafe.As<T, byte>(ref value));
                        case sizeof(short):
                            return TryWriteInt16(Unsafe.As<T, short>(ref value));
                        case sizeof(int):
                            return TryWriteInt32(Unsafe.As<T, int>(ref value));
                        case sizeof(long):
                            return TryWriteInt64(Unsafe.As<T, long>(ref value));

                        default:
                            if (BitConverter.IsLittleEndian)
                            {
                                if (MemoryMarshal.TryWrite(_span[Offset..], ref value))
                                {
                                    Offset += size;
                                    return true;
                                }
                            }

                            return false;
                    }
                }

                default:
                    return false;
            }
        }

        /// <summary>
        /// Serializes unmanaged types into bytes.
        /// </summary>
        /// <param name="value">The unmanaged type to serialize.</param>
        /// <typeparam name="T">The type of unmanaged type.</typeparam>
        /// <exception cref="ArgumentException">If <typeparamref name="T"/> is a user-defined struct
        /// which is not of blittable size, we are not on a little-endian machine, and <typeparamref name="T"/>
        /// is not a type for which serialization is already supported.</exception>
        public void WriteUnmanaged<T>(T value) where T : unmanaged
        {
            var typeCode = Type.GetTypeCode(typeof(T));

            switch (typeCode)
            {
                case TypeCode.Boolean:
                    WriteBool(Unsafe.As<T, bool>(ref value));
                    break;
                case TypeCode.Byte:
                    WriteByte(Unsafe.As<T, byte>(ref value));
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
                    WriteFloat(Unsafe.As<T, float>(ref value));
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
                case TypeCode.Object when typeof(T) == typeof(Guid):
                    WriteGuid(Unsafe.As<T, Guid>(ref value));
                    break;

                // For any other user-defined struct, we will try and marshal it directly. This should always
                // be possible if the struct is of blittable size, but we can also add in support for writing
                // arbitrarily-sized ones if we are in little endian.
                case TypeCode.Object:
                {
                    int size = Unsafe.SizeOf<T>();

                    switch (size)
                    {
                        case sizeof(byte):
                            WriteByte(Unsafe.As<T, byte>(ref value));
                            break;
                        case sizeof(short):
                            WriteInt16(Unsafe.As<T, short>(ref value));
                            break;
                        case sizeof(int):
                            WriteInt32(Unsafe.As<T, int>(ref value));
                            break;
                        case sizeof(long):
                            WriteInt64(Unsafe.As<T, long>(ref value));
                            break;

                        default:
                            if (BitConverter.IsLittleEndian)
                            {
                                MemoryMarshal.Write(_span[Offset..], ref value);
                                Offset += size;
                                break;
                            }

                            throw new ArgumentException(
                                $"Serialization of {typeof(T)} type is unsupported on this machine");
                    }

                    break;
                }

                default:
                    throw new ArgumentException($"Serialization of {typeof(T)} type is unsupported");
            }
        }

        public bool TryWriteEnum<T>(T value) where T : unmanaged, Enum
        {
            if (!CanWrite(Unsafe.SizeOf<T>()))
                return false;

            WriteEnum(value);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteEnum<T>(T value) where T : unmanaged, Enum
        {
            WriteUnmanaged(value);
        }

        public bool TryWriteBool(bool value)
        {
            if (!CanWrite(sizeof(bool)))
                return false;

            WriteBool(value);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBool(bool value)
        {
            WriteByte(value ? (byte)1 : (byte)0);
        }

        public bool TryWriteByte(byte value)
        {
            if (!CanWrite(sizeof(byte)))
                return false;

            WriteByte(value);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteByte(byte value)
        {
            _span[Offset++] = value;
        }

        public bool TryWriteUInt16(ushort value)
        {
            if (!CanWrite(sizeof(ushort)))
                return false;

            WriteUInt16(value);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt16(ushort value)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(_span[Offset..], value);
            Offset += sizeof(ushort);
        }

        public bool TryWriteInt16(short value)
        {
            if (!CanWrite(sizeof(short)))
                return false;

            WriteInt16(value);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteInt16(short value)
        {
            BinaryPrimitives.WriteInt16LittleEndian(_span[Offset..], value);
            Offset += sizeof(short);
        }

        public bool TryWriteUInt32(uint value)
        {
            if (!CanWrite(sizeof(uint)))
                return false;

            WriteUInt32(value);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt32(uint value)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(_span[Offset..], value);
            Offset += sizeof(uint);
        }

        public bool TryWriteInt32(int value)
        {
            if (!CanWrite(sizeof(int)))
                return false;

            WriteInt32(value);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt32(int value)
        {
            BinaryPrimitives.WriteInt32LittleEndian(_span[Offset..], value);
            Offset += sizeof(int);
        }

        public bool TryWriteUInt64(ulong value)
        {
            if (!CanWrite(sizeof(ulong)))
                return false;

            WriteUInt64(value);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt64(ulong value)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(_span[Offset..], value);
            Offset += sizeof(ulong);
        }

        public bool TryWriteInt64(long value)
        {
            if (!CanWrite(sizeof(long)))
                return false;

            WriteInt64(value);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt64(long value)
        {
            BinaryPrimitives.WriteInt64LittleEndian(_span[Offset..], value);
            Offset += sizeof(long);
        }

        public bool TryWriteFloat(float value)
        {
            if (!CanWrite(sizeof(float)))
                return false;

            WriteFloat(value);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteFloat(float value)
        {
            BinaryPrimitives.WriteSingleLittleEndian(_span[Offset..], value);
            Offset += sizeof(float);
        }

        public bool TryWriteDouble(double value)
        {
            if (!CanWrite(sizeof(double)))
                return false;

            WriteDouble(value);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDouble(double value)
        {
            BinaryPrimitives.WriteDoubleLittleEndian(_span[Offset..], value);
            Offset += sizeof(double);
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanWrite(int size)
        {
            return Offset <= _span.Length - size;
        }
    }
}
