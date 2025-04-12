// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Network.PacketV2;

namespace Sphynx.Network.Transport
{
    public interface IPacketTransporter
    {
        ValueTask SendPacketAsync(Stream stream, SphynxPacket packet, CancellationToken cancellationToken = default);
        ValueTask<SphynxPacket> ReceivePacketAsync(Stream stream, CancellationToken cancellationToken = default);
    }
}
