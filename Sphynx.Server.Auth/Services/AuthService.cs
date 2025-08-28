// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.Logging;
using Sphynx.Core;
using Sphynx.ModelV2.User;
using Sphynx.Server.Auth.Model;
using Sphynx.Server.Auth.Persistence;
using Sphynx.ServerV2.Auth;
using Sphynx.ServerV2.Extensions;
using Sphynx.ServerV2.Infrastructure.Services;

namespace Sphynx.Server.Auth.Services
{
    public class AuthService : IAuthService
    {
        private const int PASSWORD_HASH_LENGTH = 256;
        private const int PASSWORD_SALT_LENGTH = PASSWORD_HASH_LENGTH;

        private readonly IAuthUserRepository _userRepository;
        private readonly ISessionService _sessionService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger _logger;

        public AuthService(IPasswordHasher passwordHasher, IAuthUserRepository userRepository, ISessionService sessionService,
            ILogger<AuthService> logger)
        {
            _passwordHasher = passwordHasher;
            _userRepository = userRepository;
            _sessionService = sessionService;
            _logger = logger;
        }

        public async Task<SphynxErrorInfo<SphynxAuthResult?>> LoginUserAsync(SphynxLoginInfo loginInfo, CancellationToken cancellationToken = default)
        {
            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation("Authenticating user against account \"{UserName}\"", loginInfo.UserName);

            var credentialsResult = await VerifyUserCredentialsAsync(loginInfo.UserName, loginInfo.Password, cancellationToken).ConfigureAwait(false);

            if (credentialsResult.ErrorCode != SphynxErrorCode.SUCCESS)
                return new SphynxErrorInfo<SphynxAuthResult?>(credentialsResult.ErrorCode, credentialsResult.Message);

            var userResult = await GetUserAsync(loginInfo.UserName, cancellationToken).ConfigureAwait(false);

            if (userResult.ErrorCode != SphynxErrorCode.SUCCESS)
                return new SphynxErrorInfo<SphynxAuthResult?>(userResult.ErrorCode, userResult.Message);

            var sessionInfo = await CreateUserSessionAsync(userResult.Data!, loginInfo.ClientAddress, cancellationToken).ConfigureAwait(false);

            return new SphynxAuthResult(userResult.Data!, sessionInfo.Data!.Value);
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
                if (_logger.IsEnabled(LogLevel.Warning))
                    _logger.LogWarning("Unable to retrieve user info for user \"{UserName}\"", userName);

                return userResult.MaskServerError();
            }

            Trace.Assert(userResult.Data is not null, "Repository should populate user info on success");

            return userResult;
        }

        public async Task<SphynxErrorInfo<SphynxAuthResult?>> RegisterUserAsync(SphynxLoginInfo registerInfo,
            CancellationToken cancellationToken = default)
        {
            var userResult = await CreateUserAsync(registerInfo.UserName, registerInfo.Password, cancellationToken).ConfigureAwait(false);

            if (userResult.ErrorCode != SphynxErrorCode.SUCCESS)
                return new SphynxErrorInfo<SphynxAuthResult?>(userResult.ErrorCode, userResult.Message);

            var sessionInfo = await CreateUserSessionAsync(userResult.Data!, registerInfo.ClientAddress, cancellationToken).ConfigureAwait(false);

            return new SphynxAuthResult(userResult.Data!, sessionInfo.Data!.Value);
        }

        public async Task<SphynxErrorInfo<int>> LogoutUserAsync(Guid sessionId, LogoutPolicy policy = LogoutPolicy.Self,
            CancellationToken cancellationToken = default)
        {
            // TODO: Notify other servers on sign-out? (so that they can stop broadcasting, for example)

            switch (policy)
            {
                case LogoutPolicy.Global:
                {
                    var revokeResult = await _sessionService.RevokeSessionAsync(sessionId, SessionUpdatePolicy.WriteThrough, cancellationToken);

                    if (revokeResult.ErrorCode != SphynxErrorCode.SUCCESS && revokeResult.ErrorCode != SphynxErrorCode.INVALID_TOKEN)
                        return new SphynxErrorInfo<int>(revokeResult.ErrorCode, revokeResult.Message).MaskServerError();

                    var revokeResults = await _sessionService
                        .RevokeSessionsAsync(revokeResult.Data!.Value.UserId, SessionUpdatePolicy.WriteThrough, cancellationToken)
                        .ConfigureAwait(false);

                    if (revokeResults.ErrorCode != SphynxErrorCode.SUCCESS && revokeResults.ErrorCode != SphynxErrorCode.INVALID_TOKEN)
                        return new SphynxErrorInfo<int>(revokeResults.ErrorCode, revokeResults.Message).MaskServerError() with { Data = 1 };

                    return (int)(revokeResults.Data + 1);
                }

                case LogoutPolicy.Self:
                default:
                {
                    var revokeResult = await _sessionService.RevokeSessionAsync(sessionId, SessionUpdatePolicy.WriteThrough, cancellationToken);

                    if (revokeResult.ErrorCode != SphynxErrorCode.SUCCESS && revokeResult.ErrorCode != SphynxErrorCode.INVALID_TOKEN)
                        return new SphynxErrorInfo<int>(revokeResult.ErrorCode, revokeResult.Message).MaskServerError();

                    return 1;
                }
            }
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

        private async Task<SphynxErrorInfo<SphynxSessionInfo?>> CreateUserSessionAsync(SphynxAuthUser user, IPAddress clientAddress,
            CancellationToken cancellationToken)
        {
            var sessionResult = await _sessionService.CreateSessionAsync(user.UserId, clientAddress, cancellationToken).ConfigureAwait(false);

            if (sessionResult.ErrorCode != SphynxErrorCode.SUCCESS)
                return sessionResult;

            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation("Successfully authenticated user against account \"{UserName}\"", user.UserName);

            Trace.Assert(sessionResult.Data is not null);

            return sessionResult;
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

                return new SphynxAuthUser(Guid.NewGuid(), userName, SphynxUserStatus.ONLINE)
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
