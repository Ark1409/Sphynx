using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Sphynx.Utils
{
    internal static class MemoryUtils
    {
        internal static void WriteBytes(this int src, Span<byte> buffer, int offset = 0)
        {
            BinaryPrimitives.WriteInt32LittleEndian(buffer[offset..], src);

            // buffer[offset] = (byte)(src & 0xFF);
            // buffer[offset + 1] = (byte)((src >> 8) & 0xFF);
            // buffer[offset + 2] = (byte)((src >> 16) & 0xFF);
            // buffer[offset + 3] = (byte)((src >> 24) & 0xFF);
        }

        internal static void WriteBytes(this uint src, Span<byte> buffer, int offset = 0)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], src);

            // buffer[offset] = (byte)(src & 0xFF);
            // buffer[offset + 1] = (byte)((src >> 8) & 0xFF);
            // buffer[offset + 2] = (byte)((src >> 16) & 0xFF);
            // buffer[offset + 3] = (byte)((src >> 24) & 0xFF);
        }

        internal static void WriteBytes(this ushort src, Span<byte> buffer, int offset = 0)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(buffer[offset..], src);

            // buffer[offset] = (byte)(src & 0xFF);
            // buffer[offset + 1] = (byte)((src >> 8) & 0xFF);
        }

        internal static int ReadInt32(this ReadOnlySpan<byte> buffer, int offset = 0)
        {
            return BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(offset, sizeof(int)));

            // int number = buffer[offset + 3] << 24;
            // number |= buffer[offset + 2] << 16;
            // number |= buffer[offset + 1] << 8;
            // number |= buffer[offset];
            // return number;
        }

        internal static uint ReadUInt32(this ReadOnlySpan<byte> buffer, int offset = 0)
        {
            return BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(offset, sizeof(uint)));

            // uint number = (uint)(buffer[offset + 3] << 24);
            // number |= (uint)(buffer[offset + 2] << 16);
            // number |= (uint)(buffer[offset + 1] << 8);
            // number |= buffer[offset];
            // return number;
        }

        internal static ushort ReadUInt16(this ReadOnlySpan<byte> buffer, int offset = 0)
        {
            return BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(offset, sizeof(ushort)));

            // ushort number = (ushort)(buffer[offset + 1] << 8);
            // number |= buffer[offset];
            // return number;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool SequenceEqual(this byte[] first, ReadOnlySpan<byte> second) => new ReadOnlySpan<byte>(first).SequenceEqual(second);
    }
}