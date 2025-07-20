// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Sphynx.Core;
using Sphynx.ModelV2.User;
using Sphynx.Server.Auth.Model;
using Sphynx.Server.Auth.Persistence;

namespace Sphynx.Server.Auth.Services
{
    public class AuthService : IAuthService
    {
        private const int PASSWORD_HASH_LENGTH = 256;
        private const int PASSWORD_SALT_LENGTH = PASSWORD_HASH_LENGTH;

        private readonly IAuthUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger _logger;

        public AuthService(IPasswordHasher passwordHasher, IAuthUserRepository userRepository, ILogger<AuthService> logger)
        {
            _passwordHasher = passwordHasher;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<SphynxErrorInfo<SphynxAuthInfo?>> AuthenticateUserAsync(string userName, string password,
            CancellationToken cancellationToken = default)
        {
            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation("Authenticating user against account \"{UserName}\"", userName);

            var passwordResult = await GetUserPasswordAsync(userName, cancellationToken).ConfigureAwait(false);

            if (passwordResult.ErrorCode != SphynxErrorCode.SUCCESS)
                return new SphynxErrorInfo<SphynxAuthInfo?>(passwordResult.ErrorCode);

            var passwordInfo = passwordResult.Data!.Value;

            if (!_passwordHasher.VerifyPassword(password, passwordInfo.PasswordHash, passwordInfo.PasswordSalt))
            {
                if (_logger.IsEnabled(LogLevel.Trace))
                    _logger.LogTrace("Invalid credentials supplied for account \"{UserName}\"", userName);

                return new SphynxErrorInfo<SphynxAuthInfo?>(SphynxErrorCode.INVALID_CREDENTIALS, "Invalid username or password");
            }

            var userResult = await GetUserAsync(userName, cancellationToken).ConfigureAwait(false);

            if (_logger.IsEnabled(LogLevel.Information) && userResult.ErrorCode == SphynxErrorCode.SUCCESS)
                _logger.LogInformation("Successfully authenticated user against account \"{UserName}\"", userName);

            var authInfo = new SphynxAuthInfo(userResult.Data!, GenerateSessionId(userResult.Data!));
            var authResult = new SphynxErrorInfo<SphynxAuthInfo?>(userResult.ErrorCode, Data: authInfo);

            // TODO: Alert message server

            return authResult;
        }

        private async Task<SphynxErrorInfo<SphynxAuthUser?>> GetUserAsync(string userName, CancellationToken cancellationToken = default)
        {
            var userResult = await _userRepository.GetUserAsync(userName, cancellationToken).ConfigureAwait(false);

            if (userResult.ErrorCode != SphynxErrorCode.SUCCESS)
            {
                if (userResult.ErrorCode is SphynxErrorCode.INVALID_USER or SphynxErrorCode.INVALID_USERNAME)
                    return userResult;

                return new SphynxErrorInfo<SphynxAuthUser?>(SphynxErrorCode.SERVER_ERROR);
            }

            Trace.Assert(userResult.Data is not null, "Repository should populate user info on success");

            return userResult;
        }

        private async Task<SphynxErrorInfo<PasswordInfo?>> GetUserPasswordAsync(string userName, CancellationToken cancellationToken = default)
        {
            var passwordResult = await _userRepository.GetUserPasswordAsync(userName, cancellationToken).ConfigureAwait(false);

            if (passwordResult.ErrorCode != SphynxErrorCode.SUCCESS)
            {
                if (passwordResult.ErrorCode is SphynxErrorCode.INVALID_USER or SphynxErrorCode.INVALID_USERNAME)
                    return passwordResult;

                // Assume then there is an error with the repository
                return new SphynxErrorInfo<PasswordInfo?>(SphynxErrorCode.SERVER_ERROR);
            }

            Trace.Assert(passwordResult.Data.HasValue, "Repository should populate password info on success");

            return passwordResult;
        }

        public async Task<SphynxErrorInfo<SphynxAuthInfo?>> RegisterUserAsync(string userName, string password,
            CancellationToken cancellationToken = default)
        {
            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation("Creating account for user \"{UserName}\"", userName);

            var createdUser = CreateNewUser(userName, password);
            var userResult = await _userRepository.InsertUserAsync(createdUser, cancellationToken);

            if (userResult.ErrorCode != SphynxErrorCode.SUCCESS)
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                    _logger.LogWarning("Account creation for user \"{UserName}\" failed. Error: {Error}", userName, userResult);

                if (userResult.ErrorCode == SphynxErrorCode.INVALID_USERNAME)
                    return new SphynxErrorInfo<SphynxAuthInfo?>(userResult.ErrorCode, "User with matching name already exists");

                return new SphynxErrorInfo<SphynxAuthInfo?>(SphynxErrorCode.SERVER_ERROR);
            }

            Trace.Assert(userResult.Data is not null, "Repository should populate user info on success");

            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation("Successfully created user {UserId} ({UserName}) ", userResult.Data!.UserId, userResult.Data.UserName);

            var authInfo = new SphynxAuthInfo(userResult.Data!, GenerateSessionId(userResult.Data!));
            var authResult = new SphynxErrorInfo<SphynxAuthInfo?>(userResult.ErrorCode, Data: authInfo);

            // TODO: Alert message server

            return authResult;
        }

        private SphynxAuthUser CreateNewUser(string userName, string password)
        {
            const int BUFFER_SIZE = PASSWORD_HASH_LENGTH + PASSWORD_SALT_LENGTH;

            byte[]? rentBuffer = null;
            var buffer = BUFFER_SIZE <= 256 * 2 ? stackalloc byte[BUFFER_SIZE] : (rentBuffer = ArrayPool<byte>.Shared.Rent(BUFFER_SIZE));

            var pwdHash = buffer[..PASSWORD_HASH_LENGTH];
            var pwdSalt = buffer.Slice(PASSWORD_HASH_LENGTH, PASSWORD_SALT_LENGTH);

            try
            {
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

        protected virtual Guid GenerateSessionId(SphynxAuthUser user) => Guid.NewGuid();
    }
}
