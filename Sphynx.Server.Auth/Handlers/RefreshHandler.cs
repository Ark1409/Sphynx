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

            if (!_jwtService.VerifyToken(request.AccessToken))
            {
                await client.SendAsync(new RefreshTokenResponse(SphynxErrorCode.INVALID_TOKEN), cancellationToken).ConfigureAwait(false);
                return;
            }

            await HandleRefreshAsync(client, request, cancellationToken).ConfigureAwait(false);
        }

        private async Task HandleRefreshAsync(ISphynxClient client, RefreshTokenRequest request, CancellationToken cancellationToken)
        {
            var jwtPayloadInfo = _jwtService.ReadToken(request.AccessToken);

            if (jwtPayloadInfo.ErrorCode != SphynxErrorCode.SUCCESS)
            {
                await client.SendAsync(new RefreshTokenResponse(jwtPayloadInfo.ErrorCode.MaskServerError()), cancellationToken).ConfigureAwait(false);
                return;
            }

            var accessTokenUser = jwtPayloadInfo.Data!.Value.Subject;

            // TODO: Are we not replacing UserId and Sessionid with JWT?

            // TODO: - Fix that up ^
            // - Make Login/Register use JWT
            // - Make central IUserRepository in Sphyxn.Server
            // - Start on chat server!
            //   ... not really...
            //    - Rewrite serialization using IMemoryWriter<T>
            //    - think carefully about how ur gonna be able to use this/use BinarySerializer with this
            //    -  x

            if (accessTokenUser != request.UserId)
            {
                await client.SendAsync(new RefreshTokenResponse(SphynxErrorCode.INVALID_TOKEN), cancellationToken).ConfigureAwait(false);
                return;
            }

            await _jwtService.DeleteTokenAsync(request.RefreshToken, cancellationToken).ConfigureAwait(false);

            var newTokenInfo = await _jwtService.CreateTokenAsync(accessTokenUser, cancellationToken).ConfigureAwait(false);

            if (newTokenInfo.ErrorCode != SphynxErrorCode.SUCCESS)
            {
                await client.SendAsync(new RefreshTokenResponse(newTokenInfo.ErrorCode.MaskServerError()), cancellationToken).ConfigureAwait(false);
                return;
            }

            var newToken = newTokenInfo.Data!.Value;
            var response = new RefreshTokenResponse(newToken.AccessToken, newToken.RefreshToken.RefreshToken, newToken.ExpiryTime);

            await client.SendAsync(response, cancellationToken).ConfigureAwait(false);

            if (client is SphynxTcpClient tcpClient)
                await tcpClient.StopAsync(waitForFinish: false).ConfigureAwait(false);
        }
    }
}
