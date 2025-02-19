// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Network.Packet;

namespace Sphynx.Network.Transport
{
    public interface IPacketTransporter
    {
        void WritePacket(SphynxPacket packet, Stream stream);
        SphynxPacket ReadPacket(Stream stream);
    }
}
