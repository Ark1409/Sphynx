// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MongoDB.Driver;
using Sphynx.Core;
using Sphynx.ServerV2.Auth;

namespace Sphynx.ServerV2.Persistence.Auth
{
    public class MongoSessionRepository : ISessionRepository
    {
        private readonly IMongoCollection<SphynxDbSession> _collection;

        public MongoSessionRepository(IMongoDatabase db, string collectionName) : this(db.GetCollection<SphynxDbSession>(collectionName))
        {
        }

        public MongoSessionRepository(IMongoCollection<SphynxDbSession> collection)
        {
            _collection = collection;
        }

        public async Task<SphynxErrorInfo> InsertAsync(SphynxSessionInfo sessionInfo, CancellationToken cancellationToken = default)
        {
            if (sessionInfo.SessionId == default || sessionInfo.UserId == default)
                return SphynxErrorCode.INVALID_TOKEN;

            // No need to insert already-expired tokens
            if (sessionInfo.ExpiresAt < DateTimeOffset.UtcNow)
                return SphynxErrorCode.SUCCESS;

            var dbToken = sessionInfo.ToRecord();

            try
            {
                await _collection.InsertOneAsync(dbToken, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            // We consider duplicate PKs to be some sort of pseudo-transient error
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                return new SphynxErrorInfo(SphynxErrorCode.INVALID_TOKEN);
            }

            return SphynxErrorCode.SUCCESS;
        }

        public async Task<SphynxErrorInfo<SphynxSessionInfo?>> GetAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            var sessionFilter = Builders<SphynxDbSession>.Filter.Eq(session => session.SessionId, sessionId);

            var dbSession = await _collection.Find(sessionFilter).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            // TODO: Remove if expired?

            return dbSession is null
                ? new SphynxErrorInfo<SphynxSessionInfo?>(SphynxErrorCode.INVALID_TOKEN, "Session not found or expired")
                : new SphynxErrorInfo<SphynxSessionInfo?>(dbSession.ToDomain());
        }

        public async Task<SphynxErrorInfo<SphynxSessionInfo[]?>> GetAsync(Guid[] sessionIds, CancellationToken cancellationToken = default)
        {
            var sessionFilters = sessionIds.Select(id => Builders<SphynxDbSession>.Filter.Eq(token => token.SessionId, id));
            var filter = Builders<SphynxDbSession>.Filter.Or(sessionFilters);

            var dbSessions = await _collection.Find(filter).ToListAsync(cancellationToken).ConfigureAwait(false);

            // TODO: Remove if expired?

            return dbSessions.Select(x => x.ToDomain()).ToArray();
        }

        public async Task<SphynxErrorInfo<SphynxSessionInfo[]?>> GetSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var userFilter = Builders<SphynxDbSession>.Filter.Eq(token => token.UserId, userId);

            var dbSessions = await _collection.Find(userFilter).ToListAsync(cancellationToken).ConfigureAwait(false);

            // TODO: Remove if expired?

            return dbSessions.Select(x => x.ToDomain()).ToArray();
        }

        public async Task<SphynxErrorInfo<bool>> SessionExistsAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            var sessionFilter = Builders<SphynxDbSession>.Filter.Eq(session => session.SessionId, sessionId);

            // TODO: Don't count if expired?
            long sessionCount = await _collection.CountDocumentsAsync(sessionFilter, new CountOptions { Limit = 1 }, cancellationToken)
                .ConfigureAwait(false);

            return sessionCount > 0;
        }

        public async Task<SphynxErrorInfo<bool>> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var userFilter = Builders<SphynxDbSession>.Filter.Eq(token => token.UserId, userId);

            // TODO: Don't count if expired?
            long sessionCount = await _collection.CountDocumentsAsync(userFilter, new CountOptions { Limit = 1 }, cancellationToken)
                .ConfigureAwait(false);

            return sessionCount > 0;
        }

        public async Task<SphynxErrorInfo<long>> CountSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var userFilter = Builders<SphynxDbSession>.Filter.Eq(token => token.UserId, userId);

            // TODO: Don't count if expired?
            long sessionCount = await _collection.CountDocumentsAsync(userFilter, cancellationToken: cancellationToken).ConfigureAwait(false);

            return sessionCount;
        }

        public async Task<SphynxErrorInfo<SphynxSessionInfo?>> GetAndUpdateExpiry(Guid sessionId, DateTimeOffset expiryTime,
            CancellationToken cancellationToken = default)
        {
            var sessionFilter = Builders<SphynxDbSession>.Filter.Eq(token => token.SessionId, sessionId);
            var expiryUpdate = Builders<SphynxDbSession>.Update.Set(session => session.ExpiresAt, expiryTime);

            var updateResult = await _collection.FindOneAndUpdateAsync(sessionFilter, expiryUpdate, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return updateResult.ToDomain();
        }

        public async Task<SphynxErrorInfo> UpdateExpiryAsync(Guid sessionId, DateTimeOffset expiryTime, CancellationToken cancellationToken = default)
        {
            var sessionFilter = Builders<SphynxDbSession>.Filter.Eq(token => token.SessionId, sessionId);
            var expiryUpdate = Builders<SphynxDbSession>.Update.Set(session => session.ExpiresAt, expiryTime);

            var updateResult = await _collection.UpdateOneAsync(sessionFilter, expiryUpdate, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (!updateResult.IsAcknowledged)
                return SphynxErrorCode.DB_WRITE_ERROR;

            if (updateResult.MatchedCount < 1 || (updateResult.IsModifiedCountAvailable && updateResult.ModifiedCount < 1))
                return SphynxErrorCode.INVALID_TOKEN;

            return SphynxErrorCode.SUCCESS;
        }

        public async Task<SphynxErrorInfo<int>> DeleteAsync(Guid[] sessionIds, CancellationToken cancellationToken = default)
        {
            var sessionFilters = sessionIds.Select(id => Builders<SphynxDbSession>.Filter.Eq(token => token.SessionId, id));
            var filter = Builders<SphynxDbSession>.Filter.Or(sessionFilters);

            var deleteResult = await _collection.DeleteManyAsync(filter, cancellationToken).ConfigureAwait(false);

            if (!deleteResult.IsAcknowledged)
                return SphynxErrorCode.DB_WRITE_ERROR;

            if (deleteResult.DeletedCount < 1)
                return SphynxErrorCode.INVALID_TOKEN;

            // TODO: Remove if expired?

            return (int)deleteResult.DeletedCount;
        }

        public async Task<SphynxErrorInfo<long>> DeleteSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var userFilter = Builders<SphynxDbSession>.Filter.Eq(token => token.UserId, userId);

            var deleteResult = await _collection.DeleteManyAsync(userFilter, cancellationToken).ConfigureAwait(false);

            if (!deleteResult.IsAcknowledged)
                return SphynxErrorCode.DB_WRITE_ERROR;

            if (deleteResult.DeletedCount < 1)
                return SphynxErrorCode.INVALID_TOKEN;

            // TODO: Remove if expired?

            return deleteResult.DeletedCount;
        }

        public async Task<SphynxErrorInfo<SphynxSessionInfo?>> DeleteAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            var sessionFilter = Builders<SphynxDbSession>.Filter.Eq(token => token.SessionId, sessionId);

            var dbSession = await _collection.FindOneAndDeleteAsync(sessionFilter, cancellationToken: cancellationToken).ConfigureAwait(false);

            return dbSession is null
                ? new SphynxErrorInfo<SphynxSessionInfo?>(SphynxErrorCode.INVALID_TOKEN, "Session not found or expired")
                : new SphynxErrorInfo<SphynxSessionInfo?>(dbSession.ToDomain());
        }
    }
}
