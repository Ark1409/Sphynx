// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Microsoft.Extensions.Logging;
using Sphynx.Core;
using Sphynx.Network.PacketV2.Request;
using Sphynx.Network.PacketV2.Response;
using Sphynx.Server.Auth.Model;
using Sphynx.Server.Auth.Services;
using Sphynx.ServerV2.Client;
using Sphynx.ServerV2.Infrastructure.Handlers;

namespace Sphynx.Server.Auth.Handlers
{
    public class RegisterHandler : IPacketHandler<RegisterRequest>
    {
        private readonly IAuthService _authService;
        private readonly ILogger _logger;

        public RegisterHandler(IAuthService authService, ILogger<RegisterHandler> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        public async Task HandlePacketAsync(ISphynxClient client, RegisterRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.UserName))
            {
                await client.SendAsync(new RegisterResponse(SphynxErrorCode.INVALID_USERNAME), cancellationToken);
                return;
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                await client.SendAsync(new RegisterResponse(SphynxErrorCode.INVALID_PASSWORD), cancellationToken).ConfigureAwait(false);
                return;
            }

            await HandleRegisterAsync(client, request, cancellationToken).ConfigureAwait(false);
        }

        private async Task HandleRegisterAsync(ISphynxClient client, RegisterRequest request, CancellationToken cancellationToken)
        {
            var authResult = await _authService.RegisterUserAsync(request.UserName, request.Password, cancellationToken).ConfigureAwait(false);

            if (authResult.ErrorCode != SphynxErrorCode.SUCCESS)
            {
                await client.SendAsync(new RegisterResponse(authResult.ErrorCode), cancellationToken).ConfigureAwait(false);
                return;
            }

            var (userInfo, jwtInfo) = authResult.Data!.Value;
            var response = new RegisterResponse(userInfo.ToDto(), jwtInfo.AccessToken, jwtInfo.RefreshTokenInfo.RefreshToken, jwtInfo.ExpiryTime);

            await client.SendAsync(response, cancellationToken).ConfigureAwait(false);

            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation("Successfully authenticated with user {UserId} ({UserName})", userInfo.UserId, userInfo.UserName);

            if (client is SphynxTcpClient tcpClient)
                await tcpClient.StopAsync(waitForFinish: false).ConfigureAwait(false);
        }
    }
}
