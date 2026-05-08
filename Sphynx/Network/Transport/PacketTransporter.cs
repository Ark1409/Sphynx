// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Network.Packet;
using Sphynx.Network.Serialization;
using Version = Sphynx.Core.Version;

namespace Sphynx.Network.Transport
{
    public class PacketTransporter : IPacketTransporter
    {
        public int MaxPacketSize { get; set; } = Array.MaxLength;
        public ITypeSerializer<SphynxMessage> PacketSerializer { get; set; }

        public Version Version { get; set; }

        public PacketTransporter(ITypeSerializer<SphynxMessage> packetSerializer)
        {
            PacketSerializer = packetSerializer ?? throw new ArgumentNullException(nameof(packetSerializer));
        }

        public ValueTask SendAsync(Stream stream, SphynxMessage packet, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public ValueTask<SphynxMessage> ReceiveAsync(Stream stream, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }
}
