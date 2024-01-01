namespace Sphynx.Utils
{
    internal static class MemoryUtils
    {
        internal static void WriteBytes(this int src, Span<byte> buffer, int offset = 0)
        {
            buffer[offset] = (byte)(src & 0xFF);
            buffer[offset + 1] = (byte)((src >> 8) & 0xFF);
            buffer[offset + 2] = (byte)((src >> 16 & 0xFF));
            buffer[offset + 3] = (byte)((src >> 24) & 0xFF);
        }

        internal static void WriteBytes(this uint src, Span<byte> buffer, int offset = 0)
        {
            buffer[offset] = (byte)(src & 0xFF);
            buffer[offset + 1] = (byte)((src >> 8) & 0xFF);
            buffer[offset + 2] = (byte)((src >> 16 & 0xFF));
            buffer[offset + 3] = (byte)((src >> 24) & 0xFF);
        }

        internal static void WriteBytes(this ushort src, Span<byte> buffer, int offset = 0)
        {
            buffer[offset] = (byte)(src & 0xFF);
            buffer[offset + 1] = (byte)((src >> 8) & 0xFF);
        }

        internal static int ReadInt32(this ReadOnlySpan<byte> buffer, int offset = 0)
        {
            int number = buffer[offset + 3] << 24;
            number |= buffer[offset + 2] << 16;
            number |= buffer[offset + 1] << 8;
            number |= buffer[offset];
            return number;
        }

        internal static uint ReadUInt32(this ReadOnlySpan<byte> buffer, int offset = 0)
        {
            uint number = (uint)(buffer[offset + 3] << 24);
            number |= (uint)(buffer[offset + 2] << 16);
            number |= (uint)(buffer[offset + 1] << 8);
            number |= buffer[offset];
            return number;
        }

        internal static ushort ReadUInt16(this ReadOnlySpan<byte> buffer, int offset = 0)
        {
            ushort number = (ushort)(buffer[offset + 1] << 8);
            number |= buffer[offset];
            return number;
        }
    }
}
