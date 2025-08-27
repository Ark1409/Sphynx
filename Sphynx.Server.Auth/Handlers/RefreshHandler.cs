// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Microsoft.Extensions.Logging;
using Sphynx.Core;
using Sphynx.Network.PacketV2.Request;
using Sphynx.Network.PacketV2.Response;
using Sphynx.ServerV2.Client;
using Sphynx.ServerV2.Extensions;
using Sphynx.ServerV2.Infrastructure.Handlers;
using Sphynx.ServerV2.Infrastructure.Services;

namespace Sphynx.Server.Auth.Handlers
{
    public class RefreshHandler : IPacketHandler<RefreshTokenRequest>
    {
        private readonly IJwtService _jwtService;
        private readonly ILogger _logger;

        public RefreshHandler(IJwtService jwtService, ILogger<RefreshHandler> logger)
        {
            _jwtService = jwtService;
            _logger = logger;
        }

        public async Task HandlePacketAsync(ISphynxClient client, RefreshTokenRequest request, CancellationToken cancellationToken = default)
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

            await HandleRefreshAsync(client, request, cancellationToken).ConfigureAwait(false);
        }

        private async Task HandleRefreshAsync(ISphynxClient client, RefreshTokenRequest request, CancellationToken cancellationToken)
        {
            var refreshTokenResult = await _jwtService.DeleteTokenAsync(request.RefreshToken, cancellationToken).ConfigureAwait(false);

            if (refreshTokenResult.ErrorCode != SphynxErrorCode.SUCCESS)
            {
                var errorInfo = new SphynxErrorInfo(refreshTokenResult.ErrorCode.MaskServerError(), refreshTokenResult.Message);
                await client.SendAsync(new RefreshTokenResponse(errorInfo), cancellationToken).ConfigureAwait(false);
                return;
            }

            var refreshTokenInfo = refreshTokenResult.Data!.Value;
            var newTokenResult = await _jwtService.CreateTokenAsync(refreshTokenInfo.User, cancellationToken).ConfigureAwait(false);

            if (newTokenResult.ErrorCode != SphynxErrorCode.SUCCESS)
            {
                var errorInfo = new SphynxErrorInfo(newTokenResult.ErrorCode.MaskServerError(), newTokenResult.Message);
                await client.SendAsync(new RefreshTokenResponse(errorInfo), cancellationToken).ConfigureAwait(false);
                return;
            }

            var newTokenInfo = newTokenResult.Data!.Value;
            var response = new RefreshTokenResponse(newTokenInfo.AccessToken, newTokenInfo.RefreshTokenInfo.RefreshToken, newTokenInfo.ExpiryTime);

            await client.SendAsync(response, cancellationToken).ConfigureAwait(false);

            if (client is SphynxTcpClient tcpClient)
                await tcpClient.StopAsync(waitForFinish: false).ConfigureAwait(false);
        }
    }
}
