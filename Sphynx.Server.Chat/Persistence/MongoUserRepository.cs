// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MongoDB.Driver;
using Sphynx.Core;
using Sphynx.Model.User;
using Sphynx.Server.Chat.Model;
using Sphynx.Server.Persistence.User;

namespace Sphynx.Server.Chat.Persistence
{
    public class MongoUserRepository : IUserRepository
    {
        private readonly IMongoCollection<SphynxDbUser> _collection;

        public MongoUserRepository(IMongoDatabase db, string collectionName) : this(db.GetCollection<SphynxDbUser>(collectionName))
        {
        }

        public MongoUserRepository(IMongoCollection<SphynxDbUser> collection)
        {
            _collection = collection;
        }

        public async Task<SphynxErrorInfo> UpdateStatusAsync(Guid userId, SphynxUserStatus newStatus, CancellationToken cancellationToken = default)
        {
            var userFilter = Builders<SphynxDbUser>.Filter.Eq(user => user.UserId, userId);
            var statusUpdate = Builders<SphynxDbUser>.Update.Set(user => user.UserStatus, newStatus);

            var updateResult = await _collection.UpdateOneAsync(userFilter, statusUpdate, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (!updateResult.IsAcknowledged)
                return SphynxErrorCode.DB_WRITE_ERROR;

            if (updateResult.IsModifiedCountAvailable && updateResult.ModifiedCount < 1)
                return SphynxErrorCode.DB_WRITE_ERROR;

            return SphynxErrorCode.SUCCESS;
        }

        public async Task<SphynxErrorInfo<SphynxChatUser?>> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var userFilter = Builders<SphynxDbUser>.Filter.Eq(user => user.UserId, userId);

            var dbUser = await _collection.Find(userFilter)
                .Project<SphynxDbUser>(GetUserProjection())
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return dbUser is null
                ? new SphynxErrorInfo<SphynxChatUser?>(SphynxErrorCode.INVALID_USER, "User not found")
                : new SphynxErrorInfo<SphynxChatUser?>(dbUser.ToSimpleDomain());
        }

        public async Task<SphynxErrorInfo<SphynxChatUser?>> GetUserAsync(string userName, CancellationToken cancellationToken = default)
        {
            var userFilter = Builders<SphynxDbUser>.Filter.Eq(user => user.UserName, userName);

            var dbUser = await _collection.Find(userFilter)
                .Project<SphynxDbUser>(GetUserProjection())
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return dbUser is null
                ? new SphynxErrorInfo<SphynxChatUser?>(SphynxErrorCode.INVALID_USER, "User not found")
                : new SphynxErrorInfo<SphynxChatUser?>(dbUser.ToSimpleDomain());
        }

        public async Task<SphynxErrorInfo<SphynxChatUser[]?>> GetUsersAsync(Guid[] userIds, CancellationToken cancellationToken = default)
        {
            var userFilters = userIds.Select(id => Builders<SphynxDbUser>.Filter.Eq(user => user.UserId, id));
            var filter = Builders<SphynxDbUser>.Filter.Or(userFilters);

            var dbUsers = await _collection.Find(filter)
                .Project<SphynxDbUser>(GetUserProjection())
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return dbUsers.Select(x => x.ToSimpleDomain()).ToArray();
        }

        public async Task<SphynxErrorInfo<SphynxChatUser[]?>> GetUsersAsync(string[] userNames, CancellationToken cancellationToken = default)
        {
            var userFilters = userNames.Select(name => Builders<SphynxDbUser>.Filter.Eq(user => user.UserName, name));
            var filter = Builders<SphynxDbUser>.Filter.Or(userFilters);

            var dbUsers = await _collection.Find(filter)
                .Project<SphynxDbUser>(GetUserProjection())
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return dbUsers.Select(x => x.ToSimpleDomain()).ToArray();
        }

        private ProjectionDefinition<SphynxDbUser> GetUserProjection()
        {
            return Builders<SphynxDbUser>.Projection
                .Exclude(user => user.Password)
                .Exclude(user => user.PasswordSalt);
        }
    }
}
