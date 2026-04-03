// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Sphynx.Network.Transport
{
    public readonly record struct ChannelId(int Value)
    {
        public const int SIZE = sizeof(int);
        public static readonly ChannelId MaxValue = int.MaxValue;
        public static readonly ChannelId MinValue = 0;

        public static implicit operator int(ChannelId id) => id.Value;
        public static implicit operator ChannelId(int id) => new(id);
    }
}
