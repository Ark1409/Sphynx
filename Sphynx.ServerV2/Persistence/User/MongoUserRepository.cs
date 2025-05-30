// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using MongoDB.Driver;
using Sphynx.Core;
using Sphynx.ModelV2.User;

namespace Sphynx.ServerV2.Persistence.User
{
    public class MongoUserRepository : IUserRepository
    {
        private readonly IMongoCollection<SphynxSelfInfo> _collection;

        public event Action<SphynxUserInfo>? UserCreated;
        public event Action<SphynxUserInfo>? UserDeleted;

        public MongoUserRepository(IMongoDatabase db, string collectionName) : this(db.GetCollection<SphynxSelfInfo>(collectionName))
        {
        }

        public MongoUserRepository(IMongoCollection<SphynxSelfInfo> collection)
        {
            _collection = collection;
        }

        public async Task<SphynxErrorInfo<SphynxSelfInfo?>> InsertUserAsync(SphynxSelfInfo user, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(user.UserName))
                return new SphynxErrorInfo<SphynxSelfInfo?>(SphynxErrorCode.INVALID_USERNAME);

            var dbUser = new SphynxSelfInfo(user);

            if (dbUser.UserId == default)
                dbUser.UserId = SnowflakeId.NewId();

            try
            {
                await _collection.InsertOneAsync(dbUser, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            // We consider duplicate PKs to be some sort of pseudo-transient error
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                return new SphynxErrorInfo<SphynxSelfInfo?>(SphynxErrorCode.INVALID_USER);
            }

            UserCreated?.Invoke(dbUser);

            return new SphynxErrorInfo<SphynxSelfInfo?>(null!); // TODO: fix
        }

        public async Task<SphynxErrorCode> UpdateUserAsync(SphynxSelfInfo updatedUser, CancellationToken cancellationToken = default)
        {
            Debug.Assert(updatedUser is SphynxUserInfo);

            if (updatedUser.UserId == default)
                return SphynxErrorCode.INVALID_USER;

            var userFilter = Builders<SphynxSelfInfo>.Filter.Eq(s => s.UserId, updatedUser.UserId);
            var updateBuilder = Builders<SphynxSelfInfo>.Update;

            // TODO: Reflect this away
            var updates = new List<UpdateDefinition<SphynxSelfInfo>>();

            if (!string.IsNullOrEmpty(updatedUser.UserName))
                updates.Add(updateBuilder.Set(user => user.UserName, updatedUser.UserName));

            updates.Add(updateBuilder.Set(user => user.UserStatus, updatedUser.UserStatus));

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

            if (result.IsModifiedCountAvailable)
            {
                if (result.MatchedCount <= 0 || result.ModifiedCount <= 0)
                    return SphynxErrorCode.INVALID_FIELD;
            }

            return SphynxErrorCode.SUCCESS;
        }

        public async Task<SphynxErrorCode> DeleteUserAsync(SnowflakeId userId, CancellationToken cancellationToken = default)
        {
            var user = await GetSelfAsync(userId, cancellationToken).ConfigureAwait(false);

            if (user.ErrorCode != SphynxErrorCode.SUCCESS)
                return user.ErrorCode;

            var userFilter = Builders<SphynxSelfInfo>.Filter.Eq(u => u.UserId, userId);
            var result = await _collection.DeleteOneAsync(userFilter, cancellationToken).ConfigureAwait(false);

            if (!result.IsAcknowledged)
                return SphynxErrorCode.DB_WRITE_ERROR;

            UserDeleted?.Invoke(user.Data!);

            return result.DeletedCount > 0 ? SphynxErrorCode.SUCCESS : SphynxErrorCode.INVALID_USER;
        }

        public async Task<SphynxErrorInfo<SphynxUserInfo?>> GetUserAsync(SnowflakeId userId, CancellationToken cancellationToken = default)
        {
            var userFilter = Builders<SphynxSelfInfo>.Filter.Eq(user => user.UserId, userId);

            // TODO: Reflect this away
            var userProjection = Builders<SphynxSelfInfo>.Projection
                .Include(s => s.UserId)
                .Include(s => s.UserName)
                .Include(s => s.UserStatus);

            var user = await _collection.Find(userFilter)
                .Project<SphynxUserInfo>(userProjection)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return user is null
                ? new SphynxErrorInfo<SphynxUserInfo?>(SphynxErrorCode.INVALID_USER)
                : new SphynxErrorInfo<SphynxUserInfo?>(user);
        }

        public async Task<SphynxErrorInfo<SphynxUserInfo?>> GetUserAsync(string userName, CancellationToken cancellationToken = default)
        {
            var userFilter = Builders<SphynxSelfInfo>.Filter.Eq(user => user.UserName, userName);

            // TODO: Reflect this away
            var userProjection = Builders<SphynxSelfInfo>.Projection
                .Include(s => s.UserId)
                .Include(s => s.UserName)
                .Include(s => s.UserStatus);

            var user = await _collection.Find(userFilter)
                .Project<SphynxUserInfo>(userProjection)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return user is null
                ? new SphynxErrorInfo<SphynxUserInfo?>(SphynxErrorCode.INVALID_USER)
                : new SphynxErrorInfo<SphynxUserInfo?>(user);
        }

        public async Task<SphynxErrorInfo<SphynxSelfInfo?>> GetSelfAsync(SnowflakeId userId, CancellationToken cancellationToken = default)
        {
            var userFilter = Builders<SphynxSelfInfo>.Filter.Eq(user => user.UserId, userId);

            using var cursor = await _collection.FindAsync(userFilter, cancellationToken: cancellationToken).ConfigureAwait(false);
            var user = await cursor.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            return user is null
                ? new SphynxErrorInfo<SphynxSelfInfo?>(SphynxErrorCode.INVALID_USER)
                : new SphynxErrorInfo<SphynxSelfInfo?>(null!); // TODO: fix
        }

        public async Task<SphynxErrorInfo<SphynxSelfInfo?>> GetSelfAsync(string userName, CancellationToken cancellationToken = default)
        {
            var userFilter = Builders<SphynxSelfInfo>.Filter.Eq(user => user.UserName, userName);

            using var cursor = await _collection.FindAsync(userFilter, cancellationToken: cancellationToken).ConfigureAwait(false);
            var user = await cursor.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            return user is null
                ? new SphynxErrorInfo<SphynxSelfInfo?>(SphynxErrorCode.INVALID_USER)
                : new SphynxErrorInfo<SphynxSelfInfo?>(null!); // TODO: fix
        }

        public async Task<SphynxErrorInfo<SphynxUserInfo[]?>> GetUsersAsync(SnowflakeId[] userIds, CancellationToken cancellationToken = default)
        {
            if (userIds.Length == 0)
                return new SphynxErrorInfo<SphynxUserInfo[]?>(Array.Empty<SphynxUserInfo>());

            var usersFilter = Builders<SphynxSelfInfo>.Filter.In(user => user.UserId, userIds);

            // TODO: Reflect this away
            var userProjection = Builders<SphynxSelfInfo>.Projection
                .Include(s => s.UserId)
                .Include(s => s.UserName)
                .Include(s => s.UserStatus);

            var users = await _collection.Find(usersFilter)
                .Project<SphynxUserInfo>(userProjection)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new SphynxErrorInfo<SphynxUserInfo[]?>(users?.ToArray());
        }

        public async Task<SphynxErrorInfo<SphynxUserInfo[]?>> GetUsersAsync(string[] userNames, CancellationToken cancellationToken = default)
        {
            if (userNames.Length == 0)
                return new SphynxErrorInfo<SphynxUserInfo[]?>(Array.Empty<SphynxUserInfo>());

            var usersFilter = Builders<SphynxSelfInfo>.Filter.In(user => user.UserName, userNames);

            // TODO: Reflect this away
            var userProjection = Builders<SphynxSelfInfo>.Projection
                .Include(s => s.UserId)
                .Include(s => s.UserName)
                .Include(s => s.UserStatus);

            var users = await _collection.Find(usersFilter)
                .Project<SphynxUserInfo>(userProjection)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new SphynxErrorInfo<SphynxUserInfo[]?>(users?.ToArray());
        }

        public async Task<SphynxErrorInfo<T?>> GetUserFieldAsync<T>(SnowflakeId userId, string fieldName,
            CancellationToken cancellationToken = default)
        {
            var userFilter = Builders<SphynxSelfInfo>.Filter.Eq(user => user.UserId, userId);
            var fieldProjection = Builders<SphynxSelfInfo>.Projection.Include(fieldName);

            using var cursor = await _collection.Find(userFilter).Project<T>(fieldProjection).ToCursorAsync(cancellationToken).ConfigureAwait(false);

            if (!await cursor.MoveNextAsync(cancellationToken))
                return new SphynxErrorInfo<T?>(SphynxErrorCode.INVALID_FIELD);

            return new SphynxErrorInfo<T?>(cursor.Current.First());
        }

        public async Task<SphynxErrorCode> UpdateUserFieldAsync<T>(SnowflakeId userId, string fieldName, T value,
            CancellationToken cancellationToken = default)
        {
            var userFilter = Builders<SphynxSelfInfo>.Filter.Eq(user => user.UserId, userId);
            var fieldUpdate = Builders<SphynxSelfInfo>.Update.Set(fieldName, value);

            var result = await _collection.UpdateOneAsync(userFilter, fieldUpdate, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (!result.IsAcknowledged)
                return SphynxErrorCode.DB_WRITE_ERROR;

            if (result.IsModifiedCountAvailable)
            {
                if (result.MatchedCount <= 0 || result.ModifiedCount <= 0)
                    return SphynxErrorCode.INVALID_FIELD;
            }

            return SphynxErrorCode.SUCCESS;
        }
    }
}
