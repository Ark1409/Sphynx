// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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

        /// <summary>
        /// Returns the exact serialization size (in bytes) of the specified <paramref name="dateTime"/>.
        /// </summary>
        /// <param name="dateTime">The <see cref="DateTime"/> to serialize.</param>
        /// <returns>The exact serialization size of specified <paramref name="dateTime"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SizeOf(DateTime dateTime)
        {
            return sizeof(long);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanWrite(int size)
        {
            return _offset <= _span.Length - size;
        }
    }
}
