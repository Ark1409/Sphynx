// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers.Binary;
using System.Collections;
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
        private int _offset;

        /// <summary>
        /// Returns the number of bytes written to the underlying span.
        /// </summary>
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _offset;
        }

        public BinarySerializer(Memory<byte> memory) : this(memory.Span)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BinarySerializer(Span<byte> span)
        {
            _span = span;
            _offset = 0;
        }

        #region Maximum and Exact sizes

        /// <summary>
        /// Returns the exact serialization size (in bytes) of the specified <paramref name="dateTime"/>.
        /// For a <see cref="DateTime"/>, the maximum size is equal to the exact size.
        /// </summary>
        /// <param name="dateTime">The <see cref="DateTime"/> to serialize.</param>
        /// <returns>The exact serialization size of specified <paramref name="dateTime"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SizeOf(DateTime dateTime)
        {
            return sizeof(long);
        }

        /// <summary>
        /// Returns the maximum serialization size (in bytes) of the specified <paramref name="dateTime"/>.
        /// For a <see cref="DateTime"/>, the maximum size is equal to the exact size.
        /// </summary>
        /// <param name="dateTime">The <see cref="DateTime"/> to serialize.</param>
        /// <returns>The maximum serialization size of specified <paramref name="dateTime"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MaxSizeOf(DateTime dateTime)
        {
            return SizeOf(dateTime);
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

        public static int SizeOf<T>(T[] array) where T : unmanaged
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
                    return sizeof(int) + array.Length * Unsafe.SizeOf<T>();

                default:
                    throw new ArgumentException($"The retrieval of the size of {typeof(T)} is unsupported");
            }
        }

        public static int MaxSizeOf<T>(T[] array) where T : unmanaged
        {
            return SizeOf(array);
        }

        public static int SizeOf<T>(ICollection<T> collection) where T : unmanaged
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
                    return sizeof(int) + collection.Count * Unsafe.SizeOf<T>();

                default:
                    throw new ArgumentException($"The retrieval of the size of {typeof(T)} is unsupported");
            }
        }

        public static int MaxSizeOf<T>(ICollection<T> collection) where T : unmanaged
        {
            return SizeOf(collection);
        }

        public static int SizeOf(ICollection<DateTime> collection)
        {
            return sizeof(int) + collection.Count * SizeOf(default(DateTime));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MaxSizeOf(ICollection<DateTime> collection)
        {
            return SizeOf(collection);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SizeOf(ICollection<Guid> collection)
        {
            return sizeof(int) + collection.Count * Unsafe.SizeOf<Guid>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MaxSizeOf(ICollection<Guid> collection)
        {
            return SizeOf(collection);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SizeOf(ICollection<SnowflakeId> collection)
        {
            return sizeof(int) + collection.Count * SnowflakeId.SIZE;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MaxSizeOf(ICollection<SnowflakeId> collection)
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

        #endregion

        #region Arrays & Span

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWriteArray<T>(T[] array) where T : unmanaged
        {
            return TryWriteSpan(new ReadOnlySpan<T>(array));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteArray<T>(T[] array) where T : unmanaged
        {
            WriteSpan(new ReadOnlySpan<T>(array));
        }

        public bool TryWriteSpan<T>(ReadOnlySpan<T> span) where T : unmanaged
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
                    if (!CanWrite(sizeof(int) + span.Length * Unsafe.SizeOf<T>()))
                        return false;

                    WriteInt32(span.Length);

                    for (int i = 0; i < span.Length; i++)
                    {
                        WritePrimitive(span[i]);
                    }

                    return true;
                }

                default:
                    throw new ArgumentException($"Serialization of {typeof(T)} is unsupported");
            }
        }

        public void WriteSpan<T>(ReadOnlySpan<T> span) where T : unmanaged
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
                    WriteInt32(span.Length);

                    for (int i = 0; i < span.Length; i++)
                    {
                        WritePrimitive(span[i]);
                    }

                    break;
                }

                default:
                    throw new ArgumentException($"Serialization of {typeof(T)} is unsupported");
            }
        }

        #endregion

        #region Collections

        public bool TryWriteCollection(ICollection<DateTime> collection)
        {
            if (!CanWrite(SizeOf(collection)))
                return false;

            WriteCollection(collection);
            return true;
        }

        public void WriteCollection(ICollection<DateTime> collection)
        {
            WriteInt32(collection.Count);

            foreach (var item in collection)
            {
                WriteDateTime(item);
            }
        }

        public bool TryWriteCollection(ICollection<Guid> collection)
        {
            if (!CanWrite(SizeOf(collection)))
                return false;

            WriteCollection(collection);
            return true;
        }

        public void WriteCollection(ICollection<Guid> collection)
        {
            WriteInt32(collection.Count);

            foreach (var item in collection)
            {
                WriteGuid(item);
            }
        }

        public bool TryWriteCollection(ICollection<string?> collection)
        {
            if (!CanWrite(SizeOf(collection)))
                return false;

            WriteCollection(collection);
            return true;
        }

        public void WriteCollection(ICollection<string?> collection)
        {
            WriteInt32(collection.Count);

            foreach (string? item in collection)
            {
                WriteString(item);
            }
        }

        public bool TryWriteCollection<T>(ICollection<T> collection) where T : unmanaged
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
                    if (!CanWrite(SizeOf(collection)))
                        return false;

                    WriteCollection(collection);
                    return true;
                }

                default:
                    return false;
            }
        }

        public void WriteCollection<T>(ICollection<T> collection) where T : unmanaged
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
                    WriteInt32(collection.Count);

                    foreach (var item in collection)
                    {
                        WritePrimitive(item);
                    }

                    break;
                }

                default:
                    throw new ArgumentException($"Serialization of {typeof(T)} type is unsupported");
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
            bool written = id.TryWriteBytes(_span[_offset..]);
            Debug.Assert(written);

            _offset += SnowflakeId.SIZE;
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
            bool written = id.TryWriteBytes(_span[_offset..]);
            Debug.Assert(written);

            _offset += Unsafe.SizeOf<Guid>();
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
            int strOffset = _offset + sizeof(int);

            int strSize = StringEncoding.GetBytes(str, _span[strOffset..]);
            WriteInt32(strSize);

            _offset += sizeof(int) + strSize;
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
            int strOffset = _offset + sizeof(int);

            int strSize = StringEncoding.GetBytes(str ?? string.Empty, _span[strOffset..]);
            WriteInt32(strSize);

            _offset += sizeof(int) + strSize;
        }

        public bool TryWriteEnum<T>(T value) where T : struct, Enum
        {
            if (!CanWrite(Unsafe.SizeOf<T>()))
                return false;

            WriteEnum(value);
            return true;
        }

        public void WriteEnum<T>(T value) where T : struct, Enum
        {
            var underlyingType = Enum.GetUnderlyingType(typeof(T));
            switch (Type.GetTypeCode(underlyingType))
            {
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

                default:
                    throw new ArgumentException($"Cannot serialize enum of type {underlyingType}");
            }
        }

        public bool TryWriteDateTime(DateTime dateTime)
        {
            if (!CanWrite(SizeOf(dateTime)))
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

        #region Primitive Types

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
            _span[_offset++] = value;
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
            BinaryPrimitives.WriteUInt16LittleEndian(_span[_offset..], value);
            _offset += sizeof(ushort);
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
            BinaryPrimitives.WriteInt16LittleEndian(_span[_offset..], value);
            _offset += sizeof(short);
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
            BinaryPrimitives.WriteUInt32LittleEndian(_span[_offset..], value);
            _offset += sizeof(uint);
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
            BinaryPrimitives.WriteInt32LittleEndian(_span[_offset..], value);
            _offset += sizeof(int);
        }

        public bool TrySerializeUInt64(ulong value)
        {
            if (!CanWrite(sizeof(ulong)))
                return false;

            WriteUInt64(value);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt64(ulong value)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(_span[_offset..], value);
            _offset += sizeof(ulong);
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
            BinaryPrimitives.WriteInt64LittleEndian(_span[_offset..], value);
            _offset += sizeof(long);
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
            BinaryPrimitives.WriteSingleLittleEndian(_span[_offset..], value);
            _offset += sizeof(float);
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
            BinaryPrimitives.WriteDoubleLittleEndian(_span[_offset..], value);
            _offset += sizeof(double);
        }

        private void WritePrimitive<T>(T value) where T : unmanaged
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

                default:
                    throw new ArgumentException($"Serialization of {typeof(T)} type is unsupported");
            }
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanWrite(int size)
        {
            return _offset <= _span.Length - size;
        }
    }
}
