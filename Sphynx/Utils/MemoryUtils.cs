using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Sphynx.Utils
{
    internal static class MemoryUtils
    {
        #region Memory writing

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBytes(this int src, Span<byte> buffer)
        {
            BinaryPrimitives.WriteInt32LittleEndian(buffer, src);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBytes(this uint src, Span<byte> buffer)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(buffer, src);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBytes(this ushort src, Span<byte> buffer)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(buffer, src);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBytes(this long src, Span<byte> buffer)
        {
            BinaryPrimitives.WriteInt64LittleEndian(buffer, src);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBytes(this ulong src, Span<byte> buffer)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(buffer, src);
        }

        public static unsafe int WriteGuidCollection<TCollection>(this TCollection? guids, Span<byte> output)
            where TCollection : class, ICollection<Guid>?
        {
            int guidCount = guids?.Count ?? 0;

            guidCount.WriteBytes(output);
            int bytesWritten = sizeof(int);

            if (guidCount <= 0)
            {
                return bytesWritten;
            }

            // Prefer normal iteration over enumerator
            if (guids is IList<Guid> guidList)
            {
                for (int i = 0; i < guidCount; i++)
                {
                    if (guidList[i].TryWriteBytes(output.Slice(i * sizeof(Guid), sizeof(Guid))))
                    {
                        bytesWritten += sizeof(Guid);
                    }
                }
            }
            else
            {
                int index = 0;
                foreach (var guid in guids!)
                {
                    if (guid.TryWriteBytes(output.Slice(index++ * sizeof(Guid), sizeof(Guid))))
                    {
                        bytesWritten += sizeof(Guid);
                    }
                }
            }

            return bytesWritten;
        }

        #endregion

        #region Memory reading

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadInt32(this ReadOnlySpan<byte> buffer)
        {
            return BinaryPrimitives.ReadInt32LittleEndian(buffer[..sizeof(int)]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadUInt32(this ReadOnlySpan<byte> buffer)
        {
            return BinaryPrimitives.ReadUInt32LittleEndian(buffer[..sizeof(uint)]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ushort ReadUInt16(this ReadOnlySpan<byte> buffer)
        {
            return BinaryPrimitives.ReadUInt16LittleEndian(buffer[..sizeof(ushort)]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ReadInt64(this ReadOnlySpan<byte> buffer)
        {
            return BinaryPrimitives.ReadInt64LittleEndian(buffer[..sizeof(long)]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ReadUInt64(this ReadOnlySpan<byte> buffer)
        {
            return BinaryPrimitives.ReadUInt64LittleEndian(buffer[..sizeof(ulong)]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadGuidSet(this ReadOnlySpan<byte> guidCountAndBytes, out ISet<Guid>? output)
        {
            int guidCount = guidCountAndBytes.ReadInt32();
            int bytesRead = sizeof(int);

            if (guidCount <= 0)
            {
                output = null;
                return sizeof(int);
            }

            bytesRead += ReadGuidCollection(guidCountAndBytes[bytesRead..], guidCount,
                output = new HashSet<Guid>(guidCount));
            return bytesRead;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadGuidList(this ReadOnlySpan<byte> guidCountAndBytes, out IList<Guid>? output)
        {
            int guidCount = guidCountAndBytes.ReadInt32();
            int bytesRead = sizeof(int);

            if (guidCount <= 0)
            {
                output = null;
                return sizeof(int);
            }

            bytesRead += ReadGuidCollection(guidCountAndBytes[bytesRead..], guidCount,
                output = new List<Guid>(guidCount));
            return bytesRead;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadGuidCollection<TCollection>(this ReadOnlySpan<byte> guidCountAndBytes, TCollection output)
            where TCollection : ICollection<Guid>
        {
            int guidCount = guidCountAndBytes.ReadInt32();
            int bytesRead = sizeof(int);

            bytesRead += ReadGuidCollection(guidCountAndBytes[bytesRead..], guidCount, output);
            return bytesRead;
        }

        public static unsafe int ReadGuidCollection<TCollection>(
            this ReadOnlySpan<byte> guidBytes,
            int guidCount,
            TCollection output)
            where TCollection : ICollection<Guid>
        {
            for (int i = 0; i < guidCount; i++)
            {
                output.Add(new Guid(guidBytes.Slice(sizeof(Guid) * i, sizeof(Guid))));
            }

            int bytesRead = guidCount * sizeof(Guid);
            return bytesRead;
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ShiftLeft<T>(this Memory<T> memory, int amount) => ShiftLeft(memory.Span, amount);

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static Span<T> ShiftLeft<T>(this Span<T> span, int amount)
        {
            if (amount == 0)
                return span;

            if (amount < 0)
                return ShiftRight(span, -amount);

            span[amount..].CopyTo(span);
            span[..(span.Length - amount)].Clear();

            return span;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> ShiftRight<T>(this Memory<T> memory, int amount) => ShiftRight(memory.Span, amount);

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static Span<T> ShiftRight<T>(this Span<T> span, int amount)
        {
            if (amount == 0)
                return span;

            if (amount < 0)
                return ShiftLeft(span, -amount);

            span[(span.Length - amount)..].CopyTo(span[amount..]);
            span[..amount].Clear();

            return span;
        }

        public static bool SequenceEqual<T>(T[]? first, T[]? second)
        {
            if (first is null && second is null) return true;
            if (first is null || second is null) return false;

            return SequenceEqual(first!, new ReadOnlySpan<T>(second));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SequenceEqual<T>(T[] first, ReadOnlySpan<T> second) => new ReadOnlySpan<T>(first).SequenceEqual(second);
    }
}
