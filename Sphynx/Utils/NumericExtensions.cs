// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Sphynx.Utils
{
    public static class NumericExtensions
    {
        public static int LowInt(this long l) => BitConverter.IsLittleEndian ? (int)(l & int.MaxValue) : (int)((l >> 32) & int.MaxValue);
        public static short LowShort(this long l) => BitConverter.IsLittleEndian ? (short)(l & short.MaxValue) : (short)((l >> 48) & short.MaxValue);
        public static byte LowByte(this long l) => BitConverter.IsLittleEndian ? (byte)(l & byte.MaxValue) : (byte)((l >> 56) & byte.MaxValue);
        public static sbyte LowSByte(this long l) => BitConverter.IsLittleEndian ? (sbyte)(l & sbyte.MaxValue) : (sbyte)((l >> 56) & sbyte.MaxValue);

        public static short LowShort(this int i) => BitConverter.IsLittleEndian ? (short)(i & short.MaxValue) : (short)((i >> 16) & short.MaxValue);
        public static byte LowByte(this int i) => BitConverter.IsLittleEndian ? (byte)(i & byte.MaxValue) : (byte)((i >> 24) & byte.MaxValue);
        public static sbyte LowSByte(this int i) => BitConverter.IsLittleEndian ? (sbyte)(i & sbyte.MaxValue) : (sbyte)((i >> 24) & sbyte.MaxValue);

        public static byte LowByte(this short s) => BitConverter.IsLittleEndian ? (byte)(s & byte.MaxValue) : (byte)((s >> 8) & byte.MaxValue);
        public static sbyte LowSByte(this short s) => BitConverter.IsLittleEndian ? (sbyte)(s & sbyte.MaxValue) : (sbyte)((s >> 8) & sbyte.MaxValue);

        public static int HighInt(this long l) => !BitConverter.IsLittleEndian ? (int)(l & int.MaxValue) : (int)((l >> 32) & int.MaxValue);
        public static short HighShort(this long l) => !BitConverter.IsLittleEndian ? (short)(l & short.MaxValue) : (short)((l >> 48) & short.MaxValue);
        public static byte HighByte(this long l) => !BitConverter.IsLittleEndian ? (byte)(l & byte.MaxValue) : (byte)((l >> 56) & byte.MaxValue);
        public static sbyte HighSByte(this long l) => !BitConverter.IsLittleEndian ? (sbyte)(l & sbyte.MaxValue) : (sbyte)((l >> 56) & sbyte.MaxValue);

        public static short HighShort(this int i) => !BitConverter.IsLittleEndian ? (short)(i & short.MaxValue) : (short)((i >> 16) & short.MaxValue);
        public static byte HighByte(this int i) => !BitConverter.IsLittleEndian ? (byte)(i & byte.MaxValue) : (byte)((i >> 24) & byte.MaxValue);
        public static sbyte HighSByte(this int i) => !BitConverter.IsLittleEndian ? (sbyte)(i & sbyte.MaxValue) : (sbyte)((i >> 24) & sbyte.MaxValue);

        public static byte HighByte(this short s) => !BitConverter.IsLittleEndian ? (byte)(s & byte.MaxValue) : (byte)((s >> 8) & byte.MaxValue);
        public static sbyte HighSByte(this short s) => !BitConverter.IsLittleEndian ? (sbyte)(s & sbyte.MaxValue) : (sbyte)((s >> 8) & sbyte.MaxValue);

        public static uint LowUInt(this ulong l) => BitConverter.IsLittleEndian ? (uint)(l & uint.MaxValue) : (uint)((l >> 32) & uint.MaxValue);
        public static ushort LowUShort(this ulong l) => BitConverter.IsLittleEndian ? (ushort)(l & ushort.MaxValue) : (ushort)((l >> 48) & ushort.MaxValue);
        public static byte LowByte(this ulong l) => BitConverter.IsLittleEndian ? (byte)(l & byte.MaxValue) : (byte)((l >> 56) & byte.MaxValue);
        public static sbyte LowSByte(this ulong l) => BitConverter.IsLittleEndian ? (sbyte)(l & byte.MaxValue) : (sbyte)((l >> 56) & byte.MaxValue);

        public static ushort LowUShort(this uint i) => BitConverter.IsLittleEndian ? (ushort)(i & ushort.MaxValue) : (ushort)((i >> 16) & ushort.MaxValue);
        public static byte LowByte(this uint i) => BitConverter.IsLittleEndian ? (byte)(i & byte.MaxValue) : (byte)((i >> 24) & byte.MaxValue);
        public static sbyte LowSByte(this uint i) => BitConverter.IsLittleEndian ? (sbyte)(i & sbyte.MaxValue) : (sbyte)((i >> 24) & sbyte.MaxValue);

        public static byte LowByte(this ushort s) => BitConverter.IsLittleEndian ? (byte)(s & byte.MaxValue) : (byte)((s >> 8) & byte.MaxValue);
        public static sbyte LowSByte(this ushort s) => BitConverter.IsLittleEndian ? (sbyte)(s & sbyte.MaxValue) : (sbyte)((s >> 8) & sbyte.MaxValue);

        public static uint HighUInt(this ulong l) => !BitConverter.IsLittleEndian ? (uint)(l & uint.MaxValue) : (uint)((l >> 32) & uint.MaxValue);
        public static ushort HighUShort(this ulong l) => !BitConverter.IsLittleEndian ? (ushort)(l & ushort.MaxValue) : (ushort)((l >> 48) & ushort.MaxValue);
        public static byte HighByte(this ulong l) => !BitConverter.IsLittleEndian ? (byte)(l & byte.MaxValue) : (byte)((l >> 56) & byte.MaxValue);
        public static sbyte HighSByte(this ulong l) => !BitConverter.IsLittleEndian ? (sbyte)(l & byte.MaxValue) : (sbyte)((l >> 56) & byte.MaxValue);

        public static ushort HighUShort(this uint i) => !BitConverter.IsLittleEndian ? (ushort)(i & ushort.MaxValue) : (ushort)((i >> 16) & ushort.MaxValue);
        public static byte HighByte(this uint i) => !BitConverter.IsLittleEndian ? (byte)(i & byte.MaxValue) : (byte)((i >> 24) & byte.MaxValue);
        public static sbyte HighSByte(this uint i) => !BitConverter.IsLittleEndian ? (sbyte)(i & sbyte.MaxValue) : (sbyte)((i >> 24) & sbyte.MaxValue);

        public static byte HighByte(this ushort s) => !BitConverter.IsLittleEndian ? (byte)(s & byte.MaxValue) : (byte)((s >> 8) & byte.MaxValue);
        public static sbyte HighSByte(this ushort s) => !BitConverter.IsLittleEndian ? (sbyte)(s & sbyte.MaxValue) : (sbyte)((s >> 8) & sbyte.MaxValue);

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public static bool IsPowerOfTwo(this int i) => BitOperations.IsPow2(i);

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public static int RoundUp(this int i, int multiple) => i % multiple == 0 ? i : RoundDown(i, multiple) + multiple;

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public static int RoundDown(this int i, int multiple) => i - i % multiple;
    }
}
