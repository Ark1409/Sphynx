// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Sphynx.Utils
{
    public static class NumericExtensions
    {
        public static int LowInt(this long l) => BitConverter.IsLittleEndian ? (int)(l & int.MaxValue) : (int)((l >> 32) & int.MaxValue);
        public static short LowShort(this long l) => BitConverter.IsLittleEndian ? (short)(l & short.MaxValue) : (short)((l >> 48) & short.MaxValue);
        public static byte LowByte(this long l) => BitConverter.IsLittleEndian ? (byte)(l & byte.MaxValue) : (byte)((l >> 56) & byte.MaxValue);

        public static short LowShort(this int i) => BitConverter.IsLittleEndian ? (short)(i & short.MaxValue) : (short)((i >> 48) & short.MaxValue);
        public static byte LowByte(this int i) => BitConverter.IsLittleEndian ? (byte)(i & byte.MaxValue) : (byte)((i >> 56) & byte.MaxValue);

        public static byte LowByte(this short s) => BitConverter.IsLittleEndian ? (byte)(s & byte.MaxValue) : (byte)((s >> 56) & byte.MaxValue);

        public static int HighInt(this long l) => !BitConverter.IsLittleEndian ? (int)(l & int.MaxValue) : (int)((l >> 32) & int.MaxValue);
        public static short HighShort(this long l) => !BitConverter.IsLittleEndian ? (short)(l & short.MaxValue) : (short)((l >> 48) & short.MaxValue);
        public static byte HighByte(this long l) => !BitConverter.IsLittleEndian ? (byte)(l & byte.MaxValue) : (byte)((l >> 56) & byte.MaxValue);

        public static short HighShort(this int i) => !BitConverter.IsLittleEndian ? (short)(i & short.MaxValue) : (short)((i >> 48) & short.MaxValue);
        public static byte HighByte(this int i) => !BitConverter.IsLittleEndian ? (byte)(i & byte.MaxValue) : (byte)((i >> 56) & byte.MaxValue);

        public static byte HighByte(this short s) => !BitConverter.IsLittleEndian ? (byte)(s & byte.MaxValue) : (byte)((s >> 56) & byte.MaxValue);
    }
}
