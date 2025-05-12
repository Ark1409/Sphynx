// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Microsoft.Extensions.Logging;
using Sphynx.Core;
using Sphynx.Network.PacketV2.Request;
using Sphynx.Network.PacketV2.Response;
using Sphynx.Server.Auth.Services;
using Sphynx.ServerV2.Persistence;

namespace Sphynx.Server.Auth.Handlers
{
    public class LoginHandler : IPacketHandler<LoginRequest>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger _logger;

        public LoginHandler(IUserRepository userRepository, IPasswordHasher passwordHasher, ILogger logger)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        public ValueTask HandlePacketAsync(SphynxClient client, LoginRequest request, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(request.UserName))
                return client.SendPacketAsync(new LoginResponse(SphynxErrorCode.INVALID_USERNAME), token);

            if (string.IsNullOrWhiteSpace(request.Password))
                return client.SendPacketAsync(new LoginResponse(SphynxErrorCode.INVALID_PASSWORD), token);

            return HandleLoginAsync(client, request, token);
        }

        private async ValueTask HandleLoginAsync(SphynxClient client, LoginRequest request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var selfResult = await _userRepository.GetSelfAsync(request.UserName, token);

            if (selfResult.ErrorCode != SphynxErrorCode.SUCCESS)
            {
                await client.SendPacketAsync(new LoginResponse(selfResult.ErrorCode), token).ConfigureAwait(false);
                return;
            }

            var selfInfo = (SphynxSelfInfo)selfResult.Data!;

            if (_passwordHasher.VerifyPassword(request.Password, selfInfo.PasswordSalt, selfInfo.Password))
            {
                await client.SendPacketAsync(new LoginResponse(SphynxErrorCode.INVALID_PASSWORD), token).ConfigureAwait(false);
                return;
            }

            token.ThrowIfCancellationRequested();

            // TODO: Alert message server
            await client.SendPacketAsync(new LoginResponse(selfInfo, Guid.NewGuid()), token).ConfigureAwait(false);

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("[{ClientId}]: Successfully authenticated with user {UserId} ({UserName})",
                    client.ClientId, selfInfo.UserId, request.UserName);
            }

            await client.DisposeAsync().ConfigureAwait(false);
        }
    }
}
