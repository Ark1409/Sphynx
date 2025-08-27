// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.CompilerServices;
using System.Text.Json;
using Sphynx.Core;
using Sphynx.ServerV2.Auth;
using StackExchange.Redis;

namespace Sphynx.ServerV2.Persistence.Auth
{
    public class RedisSessionRepository : ISessionRepository
    {
        private static readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerOptions.Default)
        {
            WriteIndented = false,
            AllowTrailingCommas = false,
            IgnoreReadOnlyProperties = false,
            IgnoreReadOnlyFields = false,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        private readonly IDatabase _db;

        public RedisSessionRepository(IConnectionMultiplexer multiplexer) : this(multiplexer.GetDatabase())
        {
        }

        public RedisSessionRepository(IDatabase db)
        {
            _db = db;
        }

        public async Task<SphynxErrorInfo> InsertAsync(SphynxSessionInfo sessionInfo, CancellationToken cancellationToken = default)
        {
            if (sessionInfo.SessionId == default || sessionInfo.UserId == default)
                return SphynxErrorCode.INVALID_TOKEN;

            cancellationToken.ThrowIfCancellationRequested();

            var sessionKey = GetSessionKey(sessionInfo.SessionId);
            var userSessionKey = GetUserSessionKey(sessionInfo.UserId, sessionInfo.SessionId);

            var dbSession = (SphynxRedisSession)sessionInfo;
            string sessionValue = JsonSerializer.Serialize(dbSession, _serializerOptions);
            var ttl = sessionInfo.ExpiresAt - DateTimeOffset.UtcNow;

            if (ttl <= TimeSpan.Zero)
                return SphynxErrorCode.SUCCESS;

            cancellationToken.ThrowIfCancellationRequested();

            var trans = _db.CreateTransaction();
            {
                trans.AddCondition(Condition.KeyNotExists(sessionKey));
                trans.AddCondition(Condition.KeyNotExists(userSessionKey));

                _ = trans.StringSetAsync(sessionKey, sessionValue, ttl, When.NotExists);
                _ = trans.StringSetAsync(userSessionKey, true, ttl, When.NotExists);

                bool transacted = await trans.ExecuteAsync(CommandFlags.DemandMaster).ConfigureAwait(false);
                return transacted ? SphynxErrorCode.SUCCESS : SphynxErrorCode.DB_WRITE_ERROR;
            }
        }

        public async Task<SphynxErrorInfo<SphynxSessionInfo?>> GetAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var sessionKey = GetSessionKey(sessionId);
            var sessionBundle = await _db.StringGetWithExpiryAsync(sessionKey, CommandFlags.PreferReplica).ConfigureAwait(false);
            var sessionValue = sessionBundle.Value;

            if (sessionValue.IsNull)
                return SphynxErrorCode.INVALID_TOKEN;

            var dbSession = JsonSerializer.Deserialize<SphynxRedisSession>(sessionValue.ToString(), _serializerOptions);

            return dbSession.WithExpiry(sessionBundle.Expiry!.Value);
        }

        public async Task<SphynxErrorInfo<bool>> SessionExistsAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var sessionKey = GetSessionKey(sessionId);
            return await _db.KeyExistsAsync(sessionKey).ConfigureAwait(false);
        }

        public async Task<SphynxErrorInfo<bool>> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var servers = _db.Multiplexer.GetServers();

            if (servers.Length < 1)
                return SphynxErrorCode.DB_READ_ERROR;

            // Assume the first server
            var keysEnumerator = servers[0].KeysAsync(_db.Database, GetUserSessionsPattern(userId)).WithCancellation(cancellationToken);

            await foreach (var _ in keysEnumerator.ConfigureAwait(false))
                return true;

            return false;
        }

        public async Task<SphynxErrorInfo<long>> CountSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var servers = _db.Multiplexer.GetServers();

            if (servers.Length < 1)
                return SphynxErrorCode.DB_READ_ERROR;

            // Assume the first server
            var keysEnumerator = servers[0].KeysAsync(_db.Database, GetUserSessionsPattern(userId)).WithCancellation(cancellationToken);
            long sessionCount = 0;

            await foreach (var _ in keysEnumerator.ConfigureAwait(false))
                sessionCount++;

            return sessionCount;
        }

        public async Task<SphynxErrorInfo<SphynxSessionInfo?>> GetAndUpdateExpiry(Guid sessionId, DateTimeOffset expiryTime,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var sessionKey = GetSessionKey(sessionId);
            var sessionValue = await _db.StringGetAsync(sessionKey, CommandFlags.PreferReplica).ConfigureAwait(false);

            if (sessionValue.IsNull)
                return SphynxErrorCode.INVALID_TOKEN;

            var dbSession = JsonSerializer.Deserialize<SphynxRedisSession>(sessionValue.ToString(), _serializerOptions);
            var userSessionKey = GetUserSessionKey(dbSession.UserId, sessionId);
            var ttl = expiryTime - DateTimeOffset.UtcNow;

            if (ttl <= TimeSpan.Zero)
                ttl = TimeSpan.Zero;

            cancellationToken.ThrowIfCancellationRequested();

            var trans = _db.CreateTransaction();
            {
                trans.AddCondition(Condition.KeyExists(sessionKey));
                trans.AddCondition(Condition.KeyExists(userSessionKey));

                _ = trans.KeyExpireAsync(sessionKey, ttl);
                _ = trans.KeyExpireAsync(userSessionKey, ttl);

                if (!await trans.ExecuteAsync(CommandFlags.DemandMaster).ConfigureAwait(false))
                    return SphynxErrorCode.DB_WRITE_ERROR;

                return dbSession.WithExpiry(expiryTime);
            }
        }

        public async Task<SphynxErrorInfo> UpdateExpiryAsync(Guid sessionId, DateTimeOffset expiryTime, CancellationToken cancellationToken = default)
        {
            return await GetAndUpdateExpiry(sessionId, expiryTime, cancellationToken).ConfigureAwait(false);
        }

        public async Task<SphynxErrorInfo<SphynxSessionInfo?>> DeleteAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var sessionKey = GetSessionKey(sessionId);

            var sessionBundle = await _db.StringGetWithExpiryAsync(sessionKey, CommandFlags.PreferReplica).ConfigureAwait(false);
            var sessionValue = sessionBundle.Value;

            if (sessionValue.IsNull)
                return SphynxErrorCode.INVALID_TOKEN;

            var dbSession = JsonSerializer.Deserialize<SphynxRedisSession>(sessionValue.ToString(), _serializerOptions);
            var userSessionKey = GetUserSessionKey(dbSession.UserId, sessionId);

            cancellationToken.ThrowIfCancellationRequested();

            var trans = _db.CreateTransaction();
            {
                _ = trans.KeyDeleteAsync(sessionKey, CommandFlags.PreferReplica);
                _ = trans.KeyDeleteAsync(userSessionKey, CommandFlags.PreferReplica);

                if (!await trans.ExecuteAsync(CommandFlags.DemandMaster).ConfigureAwait(false))
                    return SphynxErrorCode.DB_WRITE_ERROR;

                return dbSession.WithExpiry(sessionBundle.Expiry!.Value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static RedisKey GetSessionKey(Guid sessionId) => $"session:{sessionId}";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static RedisKey GetUserSessionKey(Guid userId, Guid sessionId) => $"userId:{userId}:session:{sessionId}";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static RedisValue GetUserSessionsPattern(Guid userId) => $"userId:{userId}:session:*";
    }
}
