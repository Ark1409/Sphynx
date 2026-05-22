using System.Buffers;
using System.Runtime.CompilerServices;

namespace Sphynx.Utils
{
    public static class MemoryUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryCopyTo<T>(this in ReadOnlySequence<T> seq, Span<T> span)
        {
            if (span.Length < seq.Length)
                return false;

            seq.CopyTo(span);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Memory<T> ShiftLeft<T>(this Memory<T> memory, int amount)
        {
            ShiftLeft(memory.Span, amount);
            return memory;
        }

        public static Span<T> ShiftLeft<T>(this Span<T> span, int amount)
        {
            if (amount == 0)
                return span;

            if (amount < 0)
            {
                if (amount == int.MinValue)
                {
                    span.Clear();
                    return span;
                }

                return ShiftRight(span, -amount);
            }

            span[amount..].CopyTo(span);
            span[^amount..].Clear();

            return span;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> ShiftRight<T>(this Memory<T> memory, int amount) => ShiftRight(memory.Span, amount);

        public static Span<T> ShiftRight<T>(this Span<T> span, int amount)
        {
            if (amount == 0)
                return span;

            if (amount < 0)
            {
                if (amount == int.MinValue)
                {
                    span.Clear();
                    return span;
                }

                return ShiftLeft(span, -amount);
            }

            span[^amount..].CopyTo(span[amount..]);
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
