// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Network.PacketV2;
using Sphynx.ServerV2.Client;

namespace Sphynx.ServerV2.Infrastructure.Handlers
{
    /// <summary>
    /// An <see cref="IPacketHandler{TPacket}"/> which does nothing.
    /// </summary>
    public class NullPacketHandler : IPacketHandler
    {
        /// <summary>
        /// A public instance of a <see cref="NullPacketHandler"/>.
        /// </summary>
        public static readonly NullPacketHandler Instance = new();

        /// <inheritdoc/>
        public Task HandlePacketAsync(ISphynxClient client, SphynxPacket packet, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
