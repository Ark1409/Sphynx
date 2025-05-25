// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Sphynx.Core;
using Sphynx.ModelV2.User;
using Sphynx.Network.PacketV2.Request;
using Sphynx.Network.PacketV2.Response;
using Sphynx.Server.Auth.Services;
using Sphynx.ServerV2.Persistence.User;
using SphynxSelfInfo = Sphynx.ServerV2.Persistence.User.SphynxSelfInfo;

namespace Sphynx.Server.Auth.Handlers
{
    public class RegisterHandler : IPacketHandler<RegisterRequest>
    {
        private const int PASSWORD_HASH_LENGTH = 256;
        private const int SALT_HASH_LENGTH = PASSWORD_HASH_LENGTH;

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

            byte[] rentBuffer = ArrayPool<byte>.Shared.Rent(SALT_HASH_LENGTH + PASSWORD_HASH_LENGTH);
            var buffer = rentBuffer.AsMemory();
            var pwdSalt = buffer[..SALT_HASH_LENGTH];
            var pwd = buffer.Slice(SALT_HASH_LENGTH, PASSWORD_HASH_LENGTH);

            SphynxSelfInfo dbUser;

            try
            {
                _passwordHasher.GenerateSalt(pwdSalt.Span);
                _passwordHasher.HashPassword(request.Password, pwdSalt.Span, pwd.Span);

                dbUser = new SphynxSelfInfo(SnowflakeId.NewId(), request.UserName, SphynxUserStatus.ONLINE)
                {
                    Password = Convert.ToBase64String(pwd.Span),
                    PasswordSalt = Convert.ToBase64String(pwdSalt.Span)
                };
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rentBuffer);
            }

            var registerResult = await _userRepository.InsertUserAsync(null!, token);// TODO: Fix

            if (registerResult.ErrorCode != SphynxErrorCode.SUCCESS)
            {
                await client.SendPacketAsync(new RegisterResponse(registerResult.ErrorCode), token).ConfigureAwait(false);
                return;
            }

            Debug.Assert(registerResult.Data is SphynxSelfInfo);

            var selfInfo = new SphynxSelfInfo(registerResult.Data!);

            // TODO: Alert message server
            await client.SendPacketAsync(new RegisterResponse(null!, Guid.NewGuid()), token).ConfigureAwait(false);// TODO: Fix

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("[{ClientId}]: Successfully authenticated with user {UserId} ({UserName})",
                    client.ClientId, selfInfo.UserId, request.UserName);
            }

            await client.DisposeAsync().ConfigureAwait(false);
        }
    }
}
