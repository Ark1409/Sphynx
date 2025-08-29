// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MongoDB.Driver;
using Sphynx.Core;
using Sphynx.Server.Auth.Model;
using Sphynx.ServerV2.Persistence.User;

namespace Sphynx.Server.Auth.Persistence
{
    public class MongoAuthUserRepository : IAuthUserRepository
    {
        private readonly IMongoCollection<SphynxDbUser> _collection;

        public MongoAuthUserRepository(IMongoDatabase db, string collectionName) : this(db.GetCollection<SphynxDbUser>(collectionName))
        {
        }

        public MongoAuthUserRepository(IMongoCollection<SphynxDbUser> collection)
        {
            _collection = collection;
        }

        public async Task<SphynxErrorInfo<SphynxAuthUser?>> InsertUserAsync(SphynxAuthUser user, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(user.UserName))
                return new SphynxErrorInfo<SphynxAuthUser?>(SphynxErrorCode.INVALID_USERNAME);

            if (user.UserId == default)
                user.UserId = Guid.NewGuid();

            var dbUser = user.ToRecord();

            try
            {
                await _collection.InsertOneAsync(dbUser, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            // We consider duplicate PKs to be some sort of pseudo-transient error
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                return new SphynxErrorInfo<SphynxAuthUser?>(SphynxErrorCode.INVALID_USER, "User with matching ID already exists");
            }

            return new SphynxErrorInfo<SphynxAuthUser?>(user);
        }

        public async Task<SphynxErrorInfo> UpdateUserAsync(SphynxAuthUser updatedUser, CancellationToken cancellationToken = default)
        {
            if (updatedUser.UserId == default)
                return SphynxErrorCode.INVALID_USER;

            var userFilter = Builders<SphynxDbUser>.Filter.Eq(s => s.UserId, updatedUser.UserId);
            var updateBuilder = Builders<SphynxDbUser>.Update;

            var updates = new List<UpdateDefinition<SphynxDbUser>>();

            // TODO: Reflect this away. Perhaps the updated fields can be specified by an expression.

            if (!string.IsNullOrEmpty(updatedUser.UserName))
                updates.Add(updateBuilder.Set(user => user.UserName, updatedUser.UserName));

            updates.Add(updateBuilder.Set(user => user.UserStatus, updatedUser.UserStatus));

            // TODO: Review this logic. We may want to be able to set things to null the in the future.

            if (updatedUser.Friends is not null)
                updates.Add(updateBuilder.Set(user => user.Friends, updatedUser.Friends));

            if (updatedUser.Rooms is not null)
                updates.Add(updateBuilder.Set(user => user.Rooms, updatedUser.Rooms));

            if (updatedUser.LastReadMessages is not null)
                updates.Add(updateBuilder.Set(user => user.LastReadMessages, updatedUser.LastReadMessages));

            if (updatedUser.IncomingFriendRequests is not null)
                updates.Add(updateBuilder.Set(user => user.IncomingFriendRequests, updatedUser.IncomingFriendRequests));

            if (updatedUser.OutgoingFriendRequests is not null)
                updates.Add(updateBuilder.Set(user => user.OutgoingFriendRequests, updatedUser.OutgoingFriendRequests));

            var userUpdate = updateBuilder.Combine(updates);

            var result = await _collection.UpdateOneAsync(userFilter, userUpdate, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (!result.IsAcknowledged)
                return SphynxErrorCode.DB_WRITE_ERROR;

            if (result.IsModifiedCountAvailable && (result.MatchedCount <= 0 || result.ModifiedCount <= 0))
                return new SphynxErrorInfo(SphynxErrorCode.INVALID_USER, "User not found");

            return SphynxErrorCode.SUCCESS;
        }

        public async Task<SphynxErrorInfo<SphynxAuthUser?>> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var userFilter = Builders<SphynxDbUser>.Filter.Eq(user => user.UserId, userId);

            var userProjection = Builders<SphynxDbUser>.Projection
                .Exclude(user => user.Password)
                .Exclude(user => user.PasswordSalt);

            var dbUser = await _collection.Find(userFilter)
                .Project<SphynxDbUser>(userProjection)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return dbUser is null
                ? new SphynxErrorInfo<SphynxAuthUser?>(SphynxErrorCode.INVALID_USER, "User not found")
                : new SphynxErrorInfo<SphynxAuthUser?>(dbUser.ToDomain());
        }

        public async Task<SphynxErrorInfo<SphynxAuthUser?>> GetUserAsync(string userName, CancellationToken cancellationToken = default)
        {
            var userFilter = Builders<SphynxDbUser>.Filter.Eq(user => user.UserName, userName);

            var userProjection = Builders<SphynxDbUser>.Projection
                .Exclude(user => user.Password)
                .Exclude(user => user.PasswordSalt);

            var dbUser = await _collection.Find(userFilter)
                .Project<SphynxDbUser>(userProjection)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return dbUser is null
                ? new SphynxErrorInfo<SphynxAuthUser?>(SphynxErrorCode.INVALID_USER, "User not found")
                : new SphynxErrorInfo<SphynxAuthUser?>(dbUser.ToDomain());
        }

        public async Task<SphynxErrorInfo<PasswordInfo?>> GetUserPasswordAsync(Guid userId,
            CancellationToken cancellationToken = default)
        {
            var userFilter = Builders<SphynxDbUser>.Filter.Eq(user => user.UserId, userId);

            var passwordProjection = Builders<SphynxDbUser>.Projection
                .Include(user => user.Password)
                .Include(user => user.PasswordSalt);

            using var cursor = await _collection.Find(userFilter)
                .Project<PasswordInfo>(passwordProjection)
                .ToCursorAsync(cancellationToken)
                .ConfigureAwait(false);

            if (!await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                return new SphynxErrorInfo<PasswordInfo?>(SphynxErrorCode.INVALID_USER, "User not found");

            return new SphynxErrorInfo<PasswordInfo?>(cursor.Current.First());
        }

        public async Task<SphynxErrorInfo<PasswordInfo?>> GetUserPasswordAsync(string userName,
            CancellationToken cancellationToken = default)
        {
            var userFilter = Builders<SphynxDbUser>.Filter.Eq(user => user.UserName, userName);

            var passwordProjection = Builders<SphynxDbUser>.Projection
                .Include(user => user.Password)
                .Include(user => user.PasswordSalt);

            using var cursor = await _collection.Find(userFilter)
                .Project<PasswordInfo>(passwordProjection)
                .ToCursorAsync(cancellationToken)
                .ConfigureAwait(false);

            if (!await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                return new SphynxErrorInfo<PasswordInfo?>(SphynxErrorCode.INVALID_USERNAME, "User not found");

            return new SphynxErrorInfo<PasswordInfo?>(cursor.Current.First());
        }

        public async Task<SphynxErrorInfo> UpdateUserPasswordAsync(Guid userId, PasswordInfo password,
            CancellationToken cancellationToken = default)
        {
            var userFilter = Builders<SphynxDbUser>.Filter.Eq(user => user.UserId, userId);

            var hashUpdate = Builders<SphynxDbUser>.Update.Set(user => user.Password, password.PasswordHash);
            var saltUpdate = Builders<SphynxDbUser>.Update.Set(user => user.PasswordSalt, password.PasswordSalt);

            var passwordUpdate = Builders<SphynxDbUser>.Update.Combine(hashUpdate, saltUpdate);

            var result = await _collection.UpdateOneAsync(userFilter, passwordUpdate, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (!result.IsAcknowledged)
                return SphynxErrorCode.DB_WRITE_ERROR;

            if (result.IsModifiedCountAvailable && (result.MatchedCount <= 0 || result.ModifiedCount <= 0))
                return new SphynxErrorInfo(SphynxErrorCode.INVALID_USER, "User not found");

            return SphynxErrorCode.SUCCESS;
        }

        public async Task<SphynxErrorInfo> UpdateUserPasswordAsync(string userName, PasswordInfo password,
            CancellationToken cancellationToken = default)
        {
            var userFilter = Builders<SphynxDbUser>.Filter.Eq(user => user.UserName, userName);

            var hashUpdate = Builders<SphynxDbUser>.Update.Set(user => user.Password, password.PasswordHash);
            var saltUpdate = Builders<SphynxDbUser>.Update.Set(user => user.PasswordSalt, password.PasswordSalt);

            var passwordUpdate = Builders<SphynxDbUser>.Update.Combine(hashUpdate, saltUpdate);

            var result = await _collection.UpdateOneAsync(userFilter, passwordUpdate, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (!result.IsAcknowledged)
                return SphynxErrorCode.DB_WRITE_ERROR;

            if (result.IsModifiedCountAvailable && (result.MatchedCount <= 0 || result.ModifiedCount <= 0))
                return new SphynxErrorInfo(SphynxErrorCode.INVALID_USERNAME, "User not found");

            return SphynxErrorCode.SUCCESS;
        }
    }
}
