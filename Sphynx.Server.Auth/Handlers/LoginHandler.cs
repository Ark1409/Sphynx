// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Microsoft.Extensions.Logging;
using Sphynx.Core;
using Sphynx.Network.PacketV2.Request;
using Sphynx.Network.PacketV2.Response;
using Sphynx.Server.Auth.Model;
using Sphynx.Server.Auth.Services;

namespace Sphynx.Server.Auth.Handlers
{
    public class LoginHandler : IPacketHandler<LoginRequest>
    {
        private readonly IAuthService _authService;
        private readonly ILogger _logger;

        public LoginHandler(IAuthService authService, ILogger logger)
        {
            _authService = authService;
            _logger = logger;
        }

        public ValueTask HandlePacketAsync(SphynxClient client, LoginRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.UserName))
                return client.SendPacketAsync(new LoginResponse(SphynxErrorCode.INVALID_USERNAME), cancellationToken);

            if (string.IsNullOrWhiteSpace(request.Password))
                return client.SendPacketAsync(new LoginResponse(SphynxErrorCode.INVALID_PASSWORD), cancellationToken);

            return HandleLoginAsync(client, request, cancellationToken);
        }

        private async ValueTask HandleLoginAsync(SphynxClient client, LoginRequest request, CancellationToken cancellationToken)
        {
            var authResult = await _authService.AuthenticateUserAsync(request.UserName, request.Password, cancellationToken).ConfigureAwait(false);

            if (authResult.ErrorCode != SphynxErrorCode.SUCCESS)
            {
                await client.SendPacketAsync(new LoginResponse(authResult.ErrorCode), cancellationToken).ConfigureAwait(false);
                return;
            }

            var authInfo = authResult.Data!.Value;

            await client.SendPacketAsync(new LoginResponse(authInfo.User.ToDto(), authInfo.SessionId), cancellationToken).ConfigureAwait(false);

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("[{ClientId}]: Successfully authenticated with user {UserId} ({UserName})", client.ClientId, authResult,
                    authInfo.User.UserName);
            }

            await client.DisposeAsync().ConfigureAwait(false);
        }
    }
}
