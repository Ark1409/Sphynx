// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Microsoft.Extensions.Logging;
using Sphynx.Network.Packet;
using Sphynx.Network.Packet.Request;
using Sphynx.Server.Client;
using Sphynx.Server.Infrastructure.Middleware;

namespace Sphynx.Server.Auth.Middleware
{
    public class AuthPacketMiddleware : IMessageMiddleware
    {
        private readonly ILogger _logger;

        public AuthPacketMiddleware(ILogger<AuthPacketMiddleware> logger)
        {
            _logger = logger;
        }

        public Task InvokeAsync(ISphynxClient client, SphynxMessage packet, NextDelegate<SphynxMessage> next, CancellationToken token = default)
        {
            if (token.IsCancellationRequested)
                return Task.FromCanceled(token);

            if (packet is not LoginRequest && packet is not RegisterRequest && packet is not LogoutRequest)
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                    _logger.LogWarning("Rejecting packet of type {MessageType} received from {EndPoint}", packet.MessageType, client.EndPoint);

                // TODO: Respond with invalid request?
                return Task.CompletedTask;
            }

            return next(client, packet, token);
        }
    }
}
