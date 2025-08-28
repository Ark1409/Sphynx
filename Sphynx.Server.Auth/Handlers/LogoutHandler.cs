// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Microsoft.Extensions.Logging;
using Sphynx.Core;
using Sphynx.Network.Packet.Request;
using Sphynx.Network.Packet.Response;
using Sphynx.Server.Auth.Services;
using Sphynx.Server.Client;
using Sphynx.Server.Extensions;
using Sphynx.Server.Infrastructure.Handlers;
using Sphynx.Server.Infrastructure.Services;

namespace Sphynx.Server.Auth.Handlers
{
    public class LogoutHandler : IPacketHandler<LogoutRequest>
    {
        private readonly IAuthService _authService;
        private readonly ILogger _logger;

        public LogoutHandler(IAuthService authService, ILogger<LogoutHandler> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        public async Task HandlePacketAsync(ISphynxClient client, LogoutRequest request, CancellationToken cancellationToken = default)
        {
            var logoutResult = await _authService
                .LogoutUserAsync(request.SessionId, request.AllSessions ? LogoutPolicy.Global : LogoutPolicy.Self, cancellationToken)
                .ConfigureAwait(false);

            if (logoutResult.ErrorCode != SphynxErrorCode.SUCCESS)
            {
                await client.SendAsync(new LoginResponse(logoutResult), cancellationToken).ConfigureAwait(false);
                return;
            }

            if (_logger.IsEnabled(LogLevel.Information))
            {
                if (request.AllSessions)
                {
                    _logger.LogInformation("Successfully logged out of session {SessionId} and {OtherCount} other(s)", request.SessionId,
                        logoutResult.Data - 1);
                }
                else
                {
                    _logger.LogInformation("Successfully logged out of session {SessionId}", request.SessionId);
                }
            }

            await client.SendAsync(new LogoutResponse(), cancellationToken).ConfigureAwait(false);

            if (client is SphynxTcpClient tcpClient)
                await tcpClient.StopAsync(waitForFinish: false).ConfigureAwait(false);
        }
    }
}
