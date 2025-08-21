// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Microsoft.Extensions.Logging;
using Sphynx.Core;
using Sphynx.Network.PacketV2.Request;
using Sphynx.Network.PacketV2.Response;
using Sphynx.ServerV2.Client;
using Sphynx.ServerV2.Infrastructure.Handlers;
using Sphynx.ServerV2.Infrastructure.Services;

namespace Sphynx.Server.Auth.Handlers
{
    public class LogoutHandler : IPacketHandler<LogoutRequest>
    {
        private readonly IJwtService _jwtService;
        private readonly ILogger _logger;

        public LogoutHandler(IJwtService jwtService, ILogger<LogoutHandler> logger)
        {
            _jwtService = jwtService;
            _logger = logger;
        }

        public async Task HandlePacketAsync(ISphynxClient client, LogoutRequest request, CancellationToken cancellationToken = default)
        {
            if (!await _jwtService.VerifyTokenAsync(request.RefreshToken, cancellationToken).ConfigureAwait(false))
            {
                await client.SendAsync(new RefreshTokenResponse(SphynxErrorCode.INVALID_TOKEN), cancellationToken).ConfigureAwait(false);
                return;
            }

            var refreshTokenResult = await _jwtService.ReadTokenAsync(request.RefreshToken, cancellationToken).ConfigureAwait(false);

            if (refreshTokenResult.ErrorCode != SphynxErrorCode.SUCCESS)
            {
                var errorInfo = new SphynxErrorInfo(refreshTokenResult.ErrorCode.MaskServerError(), refreshTokenResult.Message);
                await client.SendAsync(new RefreshTokenResponse(errorInfo), cancellationToken).ConfigureAwait(false);
                return;
            }

            var refreshTokenInfo = refreshTokenResult.Data!.Value;

            if (request.AccessToken != refreshTokenInfo.AccessToken)
            {
                await client.SendAsync(new RefreshTokenResponse(SphynxErrorCode.INVALID_TOKEN), cancellationToken).ConfigureAwait(false);
                return;
            }

            await HandleLogoutAsync(client, request, cancellationToken).ConfigureAwait(false);
        }

        private async Task HandleLogoutAsync(ISphynxClient client, LogoutRequest request, CancellationToken cancellationToken)
        {
            var authResult = await _jwtService.DeleteTokenAsync(request.RefreshToken, cancellationToken).ConfigureAwait(false);

            if (authResult.ErrorCode != SphynxErrorCode.SUCCESS)
            {
                await client.SendAsync(new LoginResponse((SphynxErrorInfo)authResult), cancellationToken).ConfigureAwait(false);
                return;
            }

            // TODO: Sign out of all clients?

            if (_logger.IsEnabled(LogLevel.Information))
            {
                var accessToken = _jwtService.ReadToken(request.AccessToken).Data!.Value;
                _logger.LogInformation("Successfully logged out user {UserId}", accessToken.Subject);
            }

            await client.SendAsync(new LogoutResponse(), cancellationToken).ConfigureAwait(false);

            if (client is SphynxTcpClient tcpClient)
                await tcpClient.StopAsync(waitForFinish: false).ConfigureAwait(false);
        }
    }
}
