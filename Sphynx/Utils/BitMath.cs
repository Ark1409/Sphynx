// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Numerics;
using System.Runtime.CompilerServices;

namespace Sphynx.Utils
{
    public static class BitMath
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint NextPow2(uint value) => BitOperations.RoundUpToPowerOf2(value + 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong NextPow2(ulong value) => BitOperations.RoundUpToPowerOf2(value + 1);
    }
}
