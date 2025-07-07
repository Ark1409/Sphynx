// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Network.PacketV2;

namespace Sphynx.Network.Serialization.Packet
{
    public abstract class PacketSerializer<T> : TypeSerializer<T>, IPacketSerializer<T> where T : SphynxPacket
    {
    }
}
