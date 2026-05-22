// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Network.Packet;
using Sphynx.Server.Client;

namespace Sphynx.Server.Infrastructure.Handlers
{
    /// <summary>
    /// An <see cref="IMessageHandler{TPacket}"/> which does nothing.
    /// </summary>
    public class NullMessageHandler : IMessageHandler
    {
        /// <summary>
        /// A public instance of a <see cref="NullMessageHandler"/>.
        /// </summary>
        public static readonly NullMessageHandler Instance = new();

        /// <inheritdoc/>
        public Task HandleMessageAsync(ISphynxClient client, SphynxMessage message, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
