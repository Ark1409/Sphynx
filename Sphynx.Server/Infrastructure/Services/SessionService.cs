// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Net;
using Microsoft.Extensions.Logging;
using Sphynx.Core;
using Sphynx.Server.Auth;
using Sphynx.Server.Persistence.Auth;
using Sphynx.Server.Extensions;

namespace Sphynx.Server.Infrastructure.Services
{
    public class SessionService : ISessionService
    {
        private readonly ISessionRepository _activeSessionRepo;
        private readonly ISessionRepository _sessionRepo;
        private readonly SessionOptions _options;

        private readonly ILogger _logger;

        public SessionService(ISessionRepository activeSessionRepo, ISessionRepository sessionRepo, ILogger logger)
            : this(activeSessionRepo, sessionRepo, logger, SessionOptions.Default)
        {
        }

        public SessionService(ISessionRepository activeSessionRepo, ISessionRepository sessionRepo, ILogger logger, SessionOptions options)
        {
            _activeSessionRepo = activeSessionRepo;
            _sessionRepo = sessionRepo;
            _options = options;
            _logger = logger;
        }

        public async Task<SphynxErrorInfo<bool>> IsActiveSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            var existsResult = await _activeSessionRepo.SessionExistsAsync(sessionId, cancellationToken).ConfigureAwait(false);
            return existsResult.MaskServerError();
        }

        public async Task<SphynxErrorInfo<bool>> IsActiveUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var existsResult = await _activeSessionRepo.UserExistsAsync(userId, cancellationToken).ConfigureAwait(false);
            return existsResult.MaskServerError();
        }

        public async Task<SphynxErrorInfo<SphynxSessionInfo?>> CreateSessionAsync(Guid userId, IPAddress clientAddress,
            CancellationToken cancellationToken = default)
        {
            var now = DateTimeOffset.UtcNow;

            var sessionInfo = new SphynxSessionInfo
            {
                SessionId = Guid.NewGuid(),
                UserId = userId,
                IpAddress = clientAddress.ToString(),
                CreatedAt = now,
                ExpiresAt = now + _options.ExpiryTime,
            };

            var insertResult = await _sessionRepo.InsertAsync(sessionInfo, cancellationToken).ConfigureAwait(false);

            if (insertResult.ErrorCode != SphynxErrorCode.SUCCESS)
                return (SphynxErrorInfo<SphynxSessionInfo?>)insertResult.MaskServerError();

            sessionInfo = sessionInfo with { ExpiresAt = DateTimeOffset.UtcNow + _options.ActiveExpiryTime };

            // If for some reason we can insert into the main DB but not redis
            try
            {
                // ReSharper disable once MethodSupportsCancellation
                insertResult = await _activeSessionRepo.InsertAsync(sessionInfo).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (_logger.IsEnabled(LogLevel.Error))
                    _logger.LogError(ex, "Could not create active session for user {UserId} ({ClientAddress}). Removing all session data...", userId,
                        clientAddress);

                insertResult = insertResult with { ErrorCode = SphynxErrorCode.SERVER_ERROR };
            }

            if (insertResult.ErrorCode != SphynxErrorCode.SUCCESS)
            {
                try
                {
                    // ReSharper disable once MethodSupportsCancellation
                    var deleteResult = await _sessionRepo.DeleteAsync(sessionInfo.SessionId).ConfigureAwait(false);

                    if (deleteResult.ErrorCode != SphynxErrorCode.SUCCESS && deleteResult.ErrorCode != SphynxErrorCode.INVALID_TOKEN)
                    {
                        if (_logger.IsEnabled(LogLevel.Error))
                            _logger.LogError("Failed to delete all session data for login {ClientAddress}", clientAddress);

                        return deleteResult.MaskServerError();
                    }

                    if (_logger.IsEnabled(LogLevel.Information))
                        _logger.LogInformation("Successfully removed all session data for login {ClientAddress}", clientAddress);
                }
                catch (Exception ex)
                {
                    if (_logger.IsEnabled(LogLevel.Error))
                        _logger.LogError(ex, "Failed to delete all session data for login {ClientAddress}", clientAddress);

                    return (SphynxErrorInfo<SphynxSessionInfo?>)insertResult.MaskServerError();
                }
            }

            return insertResult.WithData<SphynxSessionInfo?>(sessionInfo);
        }

        public async Task<SphynxErrorInfo<long>> CountActiveSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var countResult = await _activeSessionRepo.CountSessionsAsync(userId, cancellationToken).ConfigureAwait(false);
            return countResult.MaskServerError();
        }

        public async Task<SphynxErrorInfo<long>> CountSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var countResult = await _sessionRepo.CountSessionsAsync(userId, cancellationToken).ConfigureAwait(false);
            return countResult.MaskServerError();
        }

        public async Task<SphynxErrorInfo<SphynxSessionInfo?>> ReviveSessionAsync(Guid sessionId,
            SessionUpdatePolicy updatePolicy = SessionUpdatePolicy.Ephemeral, CancellationToken cancellationToken = default)
        {
            var updateResult = await _activeSessionRepo.GetAndUpdateExpiry(sessionId, _options.ActiveExpiryTime, cancellationToken)
                .ConfigureAwait(false);

            // If the session does not exist
            if (updateResult.ErrorCode == SphynxErrorCode.INVALID_TOKEN)
            {
                // Update regardless of policy on cache miss
                var dbUpdateResult = await _sessionRepo.GetAndUpdateExpiry(sessionId, _options.ExpiryTime, cancellationToken).ConfigureAwait(false);

                if (dbUpdateResult.ErrorCode != SphynxErrorCode.SUCCESS)
                    return dbUpdateResult.MaskServerError();

                var sessionInfo = dbUpdateResult.Data!.Value with { ExpiresAt = DateTimeOffset.UtcNow + _options.ActiveExpiryTime };
                var insertResult = await _activeSessionRepo.InsertAsync(sessionInfo, cancellationToken).ConfigureAwait(false);

                if (updateResult.ErrorCode != SphynxErrorCode.SUCCESS)
                {
                    if (_logger.IsEnabled(LogLevel.Warning))
                        _logger.LogWarning("Failed session update for session {SessionId}", sessionId);

                    return (SphynxErrorInfo<SphynxSessionInfo?>)insertResult.MaskServerError();
                }

                return sessionInfo;
            }

            if (updateResult.ErrorCode != SphynxErrorCode.SUCCESS)
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                    _logger.LogWarning("Failed session update for session {SessionId}", sessionId);

                return updateResult.MaskServerError();
            }

            switch (updatePolicy)
            {
                case SessionUpdatePolicy.WriteThrough:
                {
                    try
                    {
                        var dbUpdateResult = await _sessionRepo.UpdateExpiryAsync(sessionId, _options.ExpiryTime, cancellationToken)
                            .ConfigureAwait(false);

                        if (dbUpdateResult.ErrorCode != SphynxErrorCode.SUCCESS)
                        {
                            if (_logger.IsEnabled(LogLevel.Error))
                                _logger.LogError("Failed {UpdatePolicy} session update for session {SessionId}", updatePolicy, sessionId);

                            dbUpdateResult = dbUpdateResult.MaskServerError();
                            return new SphynxErrorInfo<SphynxSessionInfo?>(dbUpdateResult.ErrorCode, dbUpdateResult.Message, updateResult.Data);
                        }

                        return updateResult;
                    }
                    catch (Exception ex)
                    {
                        if (_logger.IsEnabled(LogLevel.Error))
                            _logger.LogError(ex, "Failed {UpdatePolicy} session update for session {SessionId}", updatePolicy, sessionId);

                        return new SphynxErrorInfo<SphynxSessionInfo?>(SphynxErrorCode.SERVER_ERROR, Data: updateResult.Data);
                    }
                }

                case SessionUpdatePolicy.WriteBehind:
                {
                    var updateTask = _sessionRepo.UpdateExpiryAsync(sessionId, _options.ExpiryTime, cancellationToken);

                    if (_logger.IsEnabled(LogLevel.Error))
                    {
                        _ = updateTask.ContinueWith((t, l) =>
                        {
                            var dbUpdateResult = t.Result;
                            ILogger logger = (ILogger)l!;

                            if (dbUpdateResult.ErrorCode != SphynxErrorCode.SUCCESS && logger.IsEnabled(LogLevel.Error))
                                logger.LogError(t.Exception, "Failed {UpdatePolicy} session update for session {SessionId}", updatePolicy, sessionId);
                        }, _logger);
                    }

                    goto default;
                }

                case SessionUpdatePolicy.Ephemeral:
                default:
                    return updateResult;
            }
        }

        public async Task<SphynxErrorInfo<SphynxSessionInfo?>> RevokeSessionAsync(Guid sessionId,
            SessionUpdatePolicy updatePolicy = SessionUpdatePolicy.WriteThrough, CancellationToken cancellationToken = default)
        {
            var deleteResult = await _activeSessionRepo.DeleteAsync(sessionId, cancellationToken).ConfigureAwait(false);

            if (deleteResult.ErrorCode != SphynxErrorCode.SUCCESS && deleteResult.ErrorCode != SphynxErrorCode.INVALID_TOKEN)
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                    _logger.LogWarning("Failed session revoking for session {SessionId}", sessionId);

                return deleteResult.MaskServerError();
            }

            switch (updatePolicy)
            {
                case SessionUpdatePolicy.WriteThrough:
                {
                    var dbDeleteResult = await _sessionRepo.DeleteAsync(sessionId, cancellationToken).ConfigureAwait(false);

                    if (dbDeleteResult.ErrorCode != SphynxErrorCode.SUCCESS)
                    {
                        if (_logger.IsEnabled(LogLevel.Error))
                            _logger.LogError("Failed {UpdatePolicy} session revoking for session {SessionId}", updatePolicy, sessionId);

                        return dbDeleteResult.MaskServerError();
                    }

                    if (_logger.IsEnabled(LogLevel.Information))
                        _logger.LogInformation("Successfully revoked session {SessionId}", sessionId);

                    break;
                }

                case SessionUpdatePolicy.WriteBehind:
                {
                    var deleteTask = _sessionRepo.DeleteAsync(sessionId, cancellationToken);

                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _ = deleteTask.ContinueWith((t, l) =>
                        {
                            var dbDeleteResult = t.Result;
                            ILogger logger = (ILogger)l!;

                            if (dbDeleteResult.ErrorCode != SphynxErrorCode.SUCCESS)
                            {
                                if (logger.IsEnabled(LogLevel.Error))
                                    logger.LogError("Failed {UpdatePolicy} session revoking for session {SessionId}", updatePolicy, sessionId);

                                return;
                            }

                            if (logger.IsEnabled(LogLevel.Information))
                                logger.LogInformation("Successfully revoked session {SessionId}", sessionId);
                        }, _logger);
                    }

                    break;
                }

                // Simply perform a write-behind update on the db in the default case
                case SessionUpdatePolicy.Ephemeral:
                default:
                    _ = _sessionRepo.UpdateExpiryAsync(sessionId, _options.ExpiryTime, cancellationToken);
                    break;
            }

            return deleteResult;
        }

        public async Task<SphynxErrorInfo<long>> RevokeSessionsAsync(Guid userId, SessionUpdatePolicy updatePolicy = SessionUpdatePolicy.WriteThrough,
            CancellationToken cancellationToken = default)
        {
            var deleteResult = await _activeSessionRepo.DeleteSessionsAsync(userId, cancellationToken).ConfigureAwait(false);

            if (deleteResult.ErrorCode != SphynxErrorCode.SUCCESS && deleteResult.ErrorCode != SphynxErrorCode.INVALID_USER)
            {
                if (_logger.IsEnabled(LogLevel.Error))
                    _logger.LogError("Failed session revoking for user {UserId}", userId);

                return deleteResult.MaskServerError();
            }

            switch (updatePolicy)
            {
                case SessionUpdatePolicy.WriteThrough:
                {
                    deleteResult = await _sessionRepo.DeleteSessionsAsync(userId, cancellationToken).ConfigureAwait(false);

                    if (deleteResult.ErrorCode != SphynxErrorCode.SUCCESS && deleteResult.ErrorCode != SphynxErrorCode.INVALID_USER)
                    {
                        if (_logger.IsEnabled(LogLevel.Error))
                            _logger.LogError("Failed {UpdatePolicy} session revoking for user {UserId}", updatePolicy, userId);

                        return deleteResult.MaskServerError();
                    }

                    break;
                }

                case SessionUpdatePolicy.WriteBehind:
                {
                    var deleteTask = _sessionRepo.DeleteSessionsAsync(userId, cancellationToken);

                    if (_logger.IsEnabled(LogLevel.Error))
                    {
                        _ = deleteTask.ContinueWith((t, l) =>
                        {
                            ILogger logger = (ILogger)l!;

                            bool isDeleted = deleteResult.ErrorCode == SphynxErrorCode.SUCCESS ||
                                             deleteResult.ErrorCode == SphynxErrorCode.INVALID_USER;

                            if (!isDeleted && logger.IsEnabled(LogLevel.Error))
                                logger.LogError("Failed {UpdatePolicy} session revoking for user {UserId}", updatePolicy, userId);
                        }, _logger);
                    }

                    break;
                }

                case SessionUpdatePolicy.Ephemeral:
                default:
                    break;
            }

            return deleteResult;
        }
    }
}
