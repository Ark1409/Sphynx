// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Microsoft.Extensions.Logging;
using Sphynx.Core;
using Sphynx.Network.PacketV2.Request;
using Sphynx.Network.PacketV2.Response;
using Sphynx.Server.Auth.Services;
using Sphynx.ServerV2.Auth;
using Sphynx.ServerV2.Persistence;

namespace Sphynx.Server.Auth.Handlers
{
    public class RegisterHandler : IPacketHandler<RegisterRequest>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger _logger;

        public RegisterHandler(IUserRepository userRepository, IPasswordHasher passwordHasher, ILogger logger)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        public ValueTask HandlePacketAsync(SphynxClient client, RegisterRequest request, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(request.UserName))
                return client.SendPacketAsync(new RegisterResponse(SphynxErrorCode.INVALID_USERNAME), token);

            if (string.IsNullOrWhiteSpace(request.Password))
                return client.SendPacketAsync(new RegisterResponse(SphynxErrorCode.INVALID_PASSWORD), token);

            return HandleRegisterAsync(client, request, token);
        }

        private async ValueTask HandleRegisterAsync(SphynxClient client, RegisterRequest request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var credentials = new SphynxUserCredentials(request.UserName, request.Password);
            // TODO: Change to InsertUser (you need to hash the pwd)
            var createResult = await _userRepository.CreateUserAsync(credentials, token);

            if (createResult.ErrorCode != SphynxErrorCode.SUCCESS)
            {
                await client.SendPacketAsync(new RegisterResponse(createResult.ErrorCode), token).ConfigureAwait(false);
                return;
            }

            var selfInfo = (SphynxSelfInfo)createResult.Data!;

            // TODO: Alert message server
            await client.SendPacketAsync(new RegisterResponse(selfInfo, Guid.NewGuid()), token).ConfigureAwait(false);

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("[{ClientId}]: Successfully authenticated with user {UserId} ({UserName})",
                    client.ClientId, selfInfo.UserId, request.UserName);
            }

            await client.DisposeAsync().ConfigureAwait(false);
        }
    }
}
