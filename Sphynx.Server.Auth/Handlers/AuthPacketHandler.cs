// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Microsoft.Extensions.Logging;
using Sphynx.Network.PacketV2;
using Sphynx.Network.PacketV2.Request;
using Sphynx.Server.Auth.Services;
using Sphynx.ServerV2.Client;
using Sphynx.ServerV2.Handlers;

namespace Sphynx.Server.Auth.Handlers
{
    public class AuthPacketHandler : IPacketHandler
    {
        private readonly IPacketHandler<LoginRequest> _loginHandler;
        private readonly IPacketHandler<RegisterRequest> _registerHandler;
        private readonly ILogger _logger;

        public AuthPacketHandler(IAuthService authService, ILoggerFactory loggerFactory)
        {
            _loginHandler = new LoginHandler(authService, loggerFactory.CreateLogger<LoginHandler>());
            _registerHandler = new RegisterHandler(authService, loggerFactory.CreateLogger<RegisterHandler>());
            _logger = loggerFactory.CreateLogger(GetType());
        }

        public AuthPacketHandler(IPacketHandler<LoginRequest> loginHandler, IPacketHandler<RegisterRequest> registerHandler, ILogger logger)
        {
            _loginHandler = loginHandler;
            _registerHandler = registerHandler;
            _logger = logger;
        }

        public Task HandlePacketAsync(ISphynxClient client, SphynxPacket packet, CancellationToken cancellationToken = default)
        {
            if (packet is LoginRequest loginRequest)
                return _loginHandler.HandlePacketAsync(client, loginRequest, cancellationToken);

            if (packet is RegisterRequest registerRequest)
                return _registerHandler.HandlePacketAsync(client, registerRequest, cancellationToken);

            if (_logger.IsEnabled(LogLevel.Warning))
                _logger.LogWarning("Unregistered packet of type {PacketType} received from {EndPoint} ({ClientId})",
                    packet.PacketType, client.EndPoint, client.ClientId);

            return Task.CompletedTask;
        }
    }
}
