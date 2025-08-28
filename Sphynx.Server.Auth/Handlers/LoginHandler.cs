// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Microsoft.Extensions.Logging;
using Sphynx.Core;
using Sphynx.Network.PacketV2.Request;
using Sphynx.Network.PacketV2.Response;
using Sphynx.Server.Auth.Model;
using Sphynx.Server.Auth.Services;
using Sphynx.ServerV2.Client;
using Sphynx.ServerV2.Extensions;
using Sphynx.ServerV2.Infrastructure.Handlers;

namespace Sphynx.Server.Auth.Handlers
{
    public class LoginHandler : IPacketHandler<LoginRequest>
    {
        private readonly IAuthService _authService;
        private readonly ILogger _logger;

        public LoginHandler(IAuthService authService, ILogger<LoginHandler> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        public async Task HandlePacketAsync(ISphynxClient client, LoginRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.UserName))
            {
                await client.SendAsync(new LoginResponse(SphynxErrorCode.INVALID_USERNAME), cancellationToken).ConfigureAwait(false);
                return;
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                await client.SendAsync(new LoginResponse(SphynxErrorCode.INVALID_PASSWORD), cancellationToken).ConfigureAwait(false);
                return;
            }

            await HandleLoginAsync(client, request, cancellationToken).ConfigureAwait(false);
        }

        private async Task HandleLoginAsync(ISphynxClient client, LoginRequest request, CancellationToken cancellationToken)
        {
            var loginInfo = new SphynxLoginInfo
            {
                UserName = request.UserName.Trim(),
                Password = request.Password.Trim(),
                ClientAddress = client.EndPoint.Address
            };

            var authResult = await _authService.LoginUserAsync(loginInfo, cancellationToken).ConfigureAwait(false);

            if (authResult.ErrorCode != SphynxErrorCode.SUCCESS)
            {
                await client.SendAsync(new LoginResponse(authResult.MaskServerError()), cancellationToken).ConfigureAwait(false);
                return;
            }

            var authInfo = authResult.Data!.Value;
            var response = new LoginResponse(authInfo.User.ToDto(), authInfo.Session.SessionId);

            await client.SendAsync(response, cancellationToken).ConfigureAwait(false);

            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation("Successfully authenticated with user {UserId} ({UserName})", authInfo.User.UserId, authInfo.User.UserName);

            if (client is SphynxTcpClient tcpClient)
                await tcpClient.StopAsync(waitForFinish: false).ConfigureAwait(false);
        }
    }
}
