// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Network.PacketV2;

namespace Sphynx.Network.Transport
{
    public interface IPacketTransporter
    {
        Task SendAsync(Stream stream, SphynxPacket packet, CancellationToken cancellationToken = default);
        Task<SphynxPacket> ReceiveAsync(Stream stream, CancellationToken cancellationToken = default);
    }
}
