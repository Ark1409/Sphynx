// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MongoDB.Driver;
using Sphynx.Core;
using Sphynx.Server.Auth.Jwt;
using Sphynx.Server.Auth;

namespace Sphynx.Server.Persistence.Auth.Jwt
{
    public class MongoRefreshTokenRepository : IRefreshTokenRepository
    {
        public event Action<SphynxRefreshTokenInfo>? TokenCreated;

        private readonly IMongoCollection<SphynxDbRefreshToken> _collection;

        public MongoRefreshTokenRepository(IMongoDatabase db, string collectionName) : this(db.GetCollection<SphynxDbRefreshToken>(collectionName))
        {
        }

        public MongoRefreshTokenRepository(IMongoCollection<SphynxDbRefreshToken> collection)
        {
            _collection = collection;
        }

        public async Task<SphynxErrorInfo> InsertAsync(SphynxRefreshTokenInfo refreshToken, CancellationToken cancellationToken = default)
        {
            if (refreshToken.RefreshToken == default || refreshToken.User == default)
                return SphynxErrorCode.INVALID_TOKEN;

            // No need to insert already-expired tokens
            if (refreshToken.ExpiryTime < DateTimeOffset.UtcNow)
                return SphynxErrorCode.SUCCESS;

            var dbToken = refreshToken.ToRecord();

            try
            {
                await _collection.InsertOneAsync(dbToken, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            // We consider duplicate PKs to be some sort of pseudo-transient error
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                return new SphynxErrorInfo(SphynxErrorCode.INVALID_TOKEN, "Token already exists");
            }

            TokenCreated?.Invoke(refreshToken);

            return SphynxErrorCode.SUCCESS;
        }

        public async Task<SphynxErrorInfo<SphynxRefreshTokenInfo?>> GetAsync(Guid refreshToken, CancellationToken cancellationToken = default)
        {
            var tokenFilter = Builders<SphynxDbRefreshToken>.Filter.Eq(token => token.RefreshToken, refreshToken);

            var dbToken = await _collection.Find(tokenFilter)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return dbToken is null
                ? new SphynxErrorInfo<SphynxRefreshTokenInfo?>(SphynxErrorCode.INVALID_TOKEN, "Refresh token not found")
                : new SphynxErrorInfo<SphynxRefreshTokenInfo?>(dbToken.ToDomain());
        }

        public async Task<SphynxErrorInfo<bool>> ExistsAsync(Guid refreshToken, CancellationToken cancellationToken = default)
        {
            var tokenFilter = Builders<SphynxDbRefreshToken>.Filter.Eq(token => token.RefreshToken, refreshToken);
            bool exists = await _collection.Find(tokenFilter).Limit(1).CountDocumentsAsync(cancellationToken).ConfigureAwait(false) >= 1;

            return new SphynxErrorInfo<bool>(exists);
        }

        public async Task<SphynxErrorInfo<SphynxRefreshTokenInfo?>> DeleteAsync(Guid refreshToken, CancellationToken cancellationToken = default)
        {
            var tokenFilter = Builders<SphynxDbRefreshToken>.Filter.Eq(token => token.RefreshToken, refreshToken);

            var dbToken = await _collection.FindOneAndDeleteAsync(tokenFilter, cancellationToken: cancellationToken).ConfigureAwait(false);

            return dbToken is null
                ? new SphynxErrorInfo<SphynxRefreshTokenInfo?>(SphynxErrorCode.INVALID_TOKEN, "Refresh token not found")
                : new SphynxErrorInfo<SphynxRefreshTokenInfo?>(dbToken.ToDomain());
        }
    }
}
