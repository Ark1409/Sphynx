// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.CompilerServices;

namespace Sphynx.Network.Transport
{
    public readonly record struct ChannelId : IComparable<ChannelId>
    {
        public const int SIZE = sizeof(ushort);
        public static readonly ChannelId MaxValue = new(ushort.MaxValue);
        public static readonly ChannelId MinValue = new(ushort.MinValue);

        private readonly ushort _value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ChannelId(ushort value) => _value = value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ChannelId(short value) => _value = unchecked((ushort)value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator short(ChannelId id) => unchecked((short)id._value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ChannelId(short id) => new(id);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ushort(ChannelId id) => id._value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ChannelId(ushort id) => new(id);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator int(ChannelId id) => id._value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ChannelId(int id) => new(checked((ushort)id));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(ChannelId other) => _value.CompareTo(other._value);
    }
}
