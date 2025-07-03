// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Microsoft.Extensions.Logging;
using Sphynx.Network.PacketV2;
using Sphynx.Network.PacketV2.Request;
using Sphynx.ServerV2.Client;
using Sphynx.ServerV2.Infrastructure.Middleware;

namespace Sphynx.Server.Auth.Middleware
{
    public class AuthPacketMiddleware : IPacketMiddleware
    {
        private readonly ILogger _logger;

        public AuthPacketMiddleware(ILogger<AuthPacketMiddleware> logger)
        {
            _logger = logger;
        }

        public Task InvokeAsync(ISphynxClient client, SphynxPacket packet, NextDelegate<SphynxPacket> next, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            if (packet is not LoginRequest && packet is not RegisterRequest)
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                    _logger.LogWarning("Rejecting packet of type {PacketType} received from {EndPoint}", packet.PacketType, client.EndPoint);

                return Task.CompletedTask;
            }

            return next(client, packet, token);
        }
    }
}
