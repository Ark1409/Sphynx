// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Sphynx.Core;
using Sphynx.ModelV2.User;
using Sphynx.Server.Auth.Model;
using Sphynx.Server.Auth.Persistence;
using Sphynx.ServerV2.Auth;
using Sphynx.ServerV2.Infrastructure.Services;

namespace Sphynx.Server.Auth.Services
{
    public class AuthService : IAuthService
    {
        private const int PASSWORD_HASH_LENGTH = 256;
        private const int PASSWORD_SALT_LENGTH = PASSWORD_HASH_LENGTH;

        private readonly IAuthUserRepository _userRepository;
        private readonly IJwtService _jwtService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger _logger;

        public AuthService(IPasswordHasher passwordHasher, IAuthUserRepository userRepository, IJwtService jwtService, ILogger<AuthService> logger)
        {
            _passwordHasher = passwordHasher;
            _userRepository = userRepository;
            _jwtService = jwtService;
            _logger = logger;
        }

        public async Task<SphynxErrorInfo<SphynxAuthInfo?>> AuthenticateUserAsync(string userName, string password, CancellationToken ct = default)
        {
            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation("Authenticating user against account \"{UserName}\"", userName);

            var verifiedCredentials = await VerifyUserCredentialsAsync(userName, password, ct).ConfigureAwait(false);

            if (verifiedCredentials.ErrorCode != SphynxErrorCode.SUCCESS)
                return new SphynxErrorInfo<SphynxAuthInfo?>(verifiedCredentials.ErrorCode, verifiedCredentials.Message);

            var userResult = await GetUserAsync(userName, ct).ConfigureAwait(false);

            if (userResult.ErrorCode != SphynxErrorCode.SUCCESS)
                return new SphynxErrorInfo<SphynxAuthInfo?>(userResult.ErrorCode, userResult.Message);

            var user = userResult.Data!;
            var jwtInfo = await CreateUserTokenAsync(user, ct).ConfigureAwait(false);

            return new SphynxAuthInfo(user, jwtInfo.Data!.Value);
        }

        private async Task<SphynxErrorInfo<bool>> VerifyUserCredentialsAsync(string userName, string password, CancellationToken cancellationToken)
        {
            var passwordResult = await _userRepository.GetUserPasswordAsync(userName, cancellationToken).ConfigureAwait(false);

            if (passwordResult.ErrorCode != SphynxErrorCode.SUCCESS)
            {
                if (passwordResult.ErrorCode == SphynxErrorCode.INVALID_USERNAME)
                    return new SphynxErrorInfo<bool>(passwordResult.ErrorCode, passwordResult.Message);

                return SphynxErrorCode.SERVER_ERROR;
            }

            Trace.Assert(passwordResult.Data is not null, "Repository should populate password info on success");

            var passwordInfo = passwordResult.Data!.Value;

            if (!_passwordHasher.VerifyPassword(password, passwordInfo.PasswordHash, passwordInfo.PasswordSalt))
            {
                if (_logger.IsEnabled(LogLevel.Trace))
                    _logger.LogTrace("Invalid credentials supplied for account \"{UserName}\"", userName);

                return false;
            }

            return true;
        }

        private async Task<SphynxErrorInfo<SphynxAuthUser?>> GetUserAsync(string userName, CancellationToken cancellationToken = default)
        {
            var userResult = await _userRepository.GetUserAsync(userName, cancellationToken).ConfigureAwait(false);

            if (userResult.ErrorCode != SphynxErrorCode.SUCCESS)
            {
                if (userResult.ErrorCode == SphynxErrorCode.INVALID_USERNAME)
                    return userResult;

                if (_logger.IsEnabled(LogLevel.Warning))
                    _logger.LogWarning("Unable to retrieve user info for user \"{UserName}\"", userName);

                return new SphynxErrorInfo<SphynxAuthUser?>(SphynxErrorCode.SERVER_ERROR);
            }

            Trace.Assert(userResult.Data is not null, "Repository should populate user info on success");

            return userResult;
        }

        public async Task<SphynxErrorInfo<SphynxAuthInfo?>> RegisterUserAsync(string userName, string password,
            CancellationToken cancellationToken = default)
        {
            var userResult = await CreateUserAsync(userName, password, cancellationToken).ConfigureAwait(false);

            if (userResult.ErrorCode != SphynxErrorCode.SUCCESS)
                return new SphynxErrorInfo<SphynxAuthInfo?>(userResult.ErrorCode, userResult.Message);

            var user = userResult.Data!;
            var jwtInfo = await CreateUserTokenAsync(user, cancellationToken).ConfigureAwait(false);

            return new SphynxAuthInfo(user, jwtInfo.Data!.Value);
        }

        private async Task<SphynxErrorInfo<SphynxAuthUser?>> CreateUserAsync(string userName, string password, CancellationToken cancellationToken)
        {
            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation("Creating account for user \"{UserName}\"", userName);

            var createdUser = CreateNewUser(userName, password);
            var userResult = await _userRepository.InsertUserAsync(createdUser, cancellationToken).ConfigureAwait(false);

            if (userResult.ErrorCode != SphynxErrorCode.SUCCESS)
            {
                if (userResult.ErrorCode == SphynxErrorCode.INVALID_USERNAME)
                    return userResult;

                if (_logger.IsEnabled(LogLevel.Warning))
                    _logger.LogWarning("Account creation for user \"{UserName}\" failed. Error: {Error}", userName, userResult);

                return new SphynxErrorInfo<SphynxAuthUser?>(userResult.ErrorCode.MaskServerError());
            }

            Trace.Assert(userResult.Data is not null, "Repository should populate user info on success");

            return userResult;
        }

        private async Task<SphynxErrorInfo<SphynxJwtInfo?>> CreateUserTokenAsync(SphynxAuthUser user, CancellationToken cancellationToken)
        {
            var jwtInfo = await _jwtService.CreateTokenAsync(user.UserId, cancellationToken).ConfigureAwait(false);

            if (jwtInfo.ErrorCode != SphynxErrorCode.SUCCESS)
                return jwtInfo;

            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation("Successfully authenticated user against account \"{UserName}\"", user.UserName);

            Trace.Assert(jwtInfo.Data is not null);

            return jwtInfo;
        }

        private SphynxAuthUser CreateNewUser(string userName, string password)
        {
            const int BUFFER_SIZE = PASSWORD_HASH_LENGTH + PASSWORD_SALT_LENGTH;

            byte[]? rentBuffer = null;
            var buffer = BUFFER_SIZE <= 512 ? stackalloc byte[512] : (rentBuffer = ArrayPool<byte>.Shared.Rent(BUFFER_SIZE));

            try
            {
                var pwdHash = buffer[..PASSWORD_HASH_LENGTH];
                var pwdSalt = buffer.Slice(PASSWORD_HASH_LENGTH, PASSWORD_SALT_LENGTH);

                _passwordHasher.GenerateSalt(pwdSalt);
                _passwordHasher.HashPassword(password, pwdSalt, pwdHash);

                return new SphynxAuthUser(SnowflakeId.NewId(), userName, SphynxUserStatus.ONLINE)
                {
                    PasswordHash = Convert.ToBase64String(pwdHash),
                    PasswordSalt = Convert.ToBase64String(pwdSalt)
                };
            }
            finally
            {
                if (rentBuffer != null)
                    ArrayPool<byte>.Shared.Return(rentBuffer);
            }
        }
    }
}
