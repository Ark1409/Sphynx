// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Network.PacketV2;
using Sphynx.ServerV2.Client;

namespace Sphynx.ServerV2.Handlers
{
    /// <summary>
    /// An <see cref="IPacketHandler{TPacket}"/> which does nothing.
    /// </summary>
    public sealed class NullPacketHandler : IPacketHandler
    {
        /// <inheritdoc/>
        public ValueTask HandlePacketAsync(ISphynxClient client, SphynxPacket packet, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }
}
