using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Sphynx.Utils
{
    internal static class MemoryUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void WriteBytes(this long src, Span<byte> buffer, int offset = 0)
        {
            BinaryPrimitives.WriteInt64LittleEndian(buffer[offset..], src);

            // buffer[offset] = (byte)(src & 0xFF);
            // buffer[offset + 1] = (byte)((src >> 8) & 0xFF);
            // buffer[offset + 2] = (byte)((src >> 16) & 0xFF);
            // buffer[offset + 3] = (byte)((src >> 24) & 0xFF);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void WriteBytes(this ulong src, Span<byte> buffer, int offset = 0)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(buffer[offset..], src);

            // buffer[offset] = (byte)(src & 0xFF);
            // buffer[offset + 1] = (byte)((src >> 8) & 0xFF);
            // buffer[offset + 2] = (byte)((src >> 16) & 0xFF);
            // buffer[offset + 3] = (byte)((src >> 24) & 0xFF);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void WriteBytes(this int src, Span<byte> buffer, int offset = 0)
        {
            BinaryPrimitives.WriteInt32LittleEndian(buffer[offset..], src);

            // buffer[offset] = (byte)(src & 0xFF);
            // buffer[offset + 1] = (byte)((src >> 8) & 0xFF);
            // buffer[offset + 2] = (byte)((src >> 16) & 0xFF);
            // buffer[offset + 3] = (byte)((src >> 24) & 0xFF);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void WriteBytes(this uint src, Span<byte> buffer, int offset = 0)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], src);

            // buffer[offset] = (byte)(src & 0xFF);
            // buffer[offset + 1] = (byte)((src >> 8) & 0xFF);
            // buffer[offset + 2] = (byte)((src >> 16) & 0xFF);
            // buffer[offset + 3] = (byte)((src >> 24) & 0xFF);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void WriteBytes(this ushort src, Span<byte> buffer, int offset = 0)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(buffer[offset..], src);

            // buffer[offset] = (byte)(src & 0xFF);
            // buffer[offset + 1] = (byte)((src >> 8) & 0xFF);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int ReadInt32(this ReadOnlySpan<byte> buffer, int offset = 0)
        {
            return BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(offset, sizeof(int)));

            // int number = buffer[offset + 3] << 24;
            // number |= buffer[offset + 2] << 16;
            // number |= buffer[offset + 1] << 8;
            // number |= buffer[offset];
            // return number;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint ReadUInt32(this ReadOnlySpan<byte> buffer, int offset = 0)
        {
            return BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(offset, sizeof(uint)));

            // uint number = (uint)(buffer[offset + 3] << 24);
            // number |= (uint)(buffer[offset + 2] << 16);
            // number |= (uint)(buffer[offset + 1] << 8);
            // number |= buffer[offset];
            // return number;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static long ReadInt64(this ReadOnlySpan<byte> buffer, int offset = 0)
        {
            return BinaryPrimitives.ReadInt64LittleEndian(buffer.Slice(offset, sizeof(long)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong ReadUInt64(this ReadOnlySpan<byte> buffer, int offset = 0)
        {
            return BinaryPrimitives.ReadUInt64LittleEndian(buffer.Slice(offset, sizeof(ulong)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ushort ReadUInt16(this ReadOnlySpan<byte> buffer, int offset = 0)
        {
            return BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(offset, sizeof(ushort)));

            // ushort number = (ushort)(buffer[offset + 1] << 8);
            // number |= buffer[offset];
            // return number;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool SequenceEqual<T>(T[]? first, T[]? second)
        {
            if (first is null && second is null) return true;
            if (first is null || second is null) return false;

            return SequenceEqual(first!, new ReadOnlySpan<T>(second));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool SequenceEqual<T>(T[] first, ReadOnlySpan<T> second) => new ReadOnlySpan<T>(first).SequenceEqual(second);
    }
}