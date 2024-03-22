using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace Sphynx.Client.Utils
{
    internal static class NumberExtensions
    {
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public static bool IsPowerOfTwo(this int i) => (i & (i - 1)) == 0;
        
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public static int RoundUp(this int i, int multiple) => i == 0 ? 0 :
            IsPowerOfTwo(multiple) ? (i & (multiple - 1) + multiple) : i + multiple - i % multiple;
        
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public static int RoundDown(this int i, int multiple) =>
            IsPowerOfTwo(multiple) ? (i & (multiple - 1)) : i - i % multiple;
        
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public static int NextPowerOfTwo(this int i)
        {
            i |= i >> 1;
            i |= i >> 2;
            i |= i >> 4;
            i |= i >> 8;
            i |= i >> 16;
            return ++i;
        }
    }
}
