// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MongoDB.Driver;
using Sphynx.Core;
using Sphynx.Server.Chat.Model;

namespace Sphynx.Server.Chat.Persistence.Friend
{
    public class MongoFriendRepository : IFriendRepository
    {
        private readonly IMongoCollection<SphynxDbFriend> _friendCollection;
        private readonly IMongoCollection<SphynxDbFriendRequest> _friendReqCollection;

        // TODO: Might need userRepository in the future to auto-reject friend requests (maybe enforced at service level)

        public MongoFriendRepository(IMongoCollection<SphynxDbFriend> friendCollection, IMongoCollection<SphynxDbFriendRequest> friendReqCollection)
        {
            _friendCollection = friendCollection;
            _friendReqCollection = friendReqCollection;
        }

        public async Task<SphynxErrorInfo<SphynxChatFriendRequest?>> AddFriendAsync(Guid userId, Guid friendId,
            CancellationToken cancellationToken = default)
        {
            if (userId == friendId)
                return new SphynxErrorInfo<SphynxChatFriendRequest?>(SphynxErrorCode.INVALID_USER, "You cannot friend yourself.");

            var dbClient = _friendCollection.Database.Client;
            using var session = await dbClient.StartSessionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            session.StartTransaction();
            {
                var existingReqResult = await GetFriendRequestAsync(userId, friendId, session, cancellationToken).ConfigureAwait(false);

                if (existingReqResult.ErrorCode == SphynxErrorCode.SUCCESS)
                {
                    var acceptResult = await AcceptFriendRequestAsync(userId, friendId, session, cancellationToken).ConfigureAwait(false);

                    if (acceptResult.ErrorCode != SphynxErrorCode.SUCCESS)
                    {
                        await session.AbortTransactionAsync(CancellationToken.None).ConfigureAwait(false);
                        return (SphynxErrorInfo<SphynxChatFriendRequest?>)acceptResult;
                    }

                    await session.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
                    return new SphynxErrorInfo<SphynxChatFriendRequest?>(existingReqResult.Data, existingReqResult.Message);
                }

                var dbFriendReq = new SphynxDbFriendRequest
                {
                    RequestId = Guid.NewGuid(),
                    InitiatorId = userId,
                    OtherId = friendId,
                    SentAt = DateTimeOffset.UtcNow
                };

                await _friendReqCollection.InsertOneAsync(session, dbFriendReq, cancellationToken: cancellationToken).ConfigureAwait(false);

                await session.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
                return dbFriendReq.ToDomain();
            }
        }

        private async Task<SphynxErrorInfo<SphynxChatFriendRequest>> GetFriendRequestAsync(Guid userId, Guid otherId, IClientSessionHandle? session,
            CancellationToken ct)
        {
            var initiatorFilter = Builders<SphynxDbFriendRequest>.Filter.Eq(user => user.InitiatorId, userId);
            var otherFilter = Builders<SphynxDbFriendRequest>.Filter.Eq(user => user.OtherId, otherId);

            var existingReqFilter = Builders<SphynxDbFriendRequest>.Filter.Or(initiatorFilter, otherFilter);

            var findResult = await _friendReqCollection.Find(session, existingReqFilter).FirstOrDefaultAsync(ct).ConfigureAwait(false);

            return findResult is null ? SphynxErrorCode.INVALID_USER : findResult.ToDomain();
        }

        private async Task<SphynxErrorInfo> AcceptFriendRequestAsync(Guid userId, Guid otherId, IClientSessionHandle? session, CancellationToken ct)
        {
            var revokeResult = await RemoveFriendRequestAsync(otherId, userId, session, ct).ConfigureAwait(false);

            if (revokeResult.ErrorCode != SphynxErrorCode.SUCCESS)
                return revokeResult;

            var dbFriend = new SphynxDbFriend
            {
                FriendshipId = Guid.NewGuid(),
                AcceptorId = userId,
                InitiatorId = otherId
            };

            await _friendCollection.InsertOneAsync(session, dbFriend, cancellationToken: ct).ConfigureAwait(false);

            return SphynxErrorCode.SUCCESS;
        }

        public async Task<SphynxErrorInfo<Guid[]?>> GetFriendRequestUsersAsync(Guid userId, FriendRequestType type = FriendRequestType.All,
            CancellationToken cancellationToken = default)
        {
            var friendReqsFilter = GetFriendRequestFilter(userId, type);
            var usersProjection = Builders<SphynxDbFriendRequest>.Projection
                .Include(friends => friends.InitiatorId)
                .Include(friends => friends.OtherId);

            var friendReqs = _friendReqCollection.Find(friendReqsFilter).Project<SphynxDbFriendRequest>(usersProjection);
            var friendReqUsers = new List<Guid>();

            using var cursor = await friendReqs.ToCursorAsync(cancellationToken).ConfigureAwait(false);

            while (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
            {
                foreach (var friendReq in cursor.Current)
                {
                    friendReqUsers.Add(friendReq.InitiatorId == userId ? friendReq.OtherId : friendReq.InitiatorId);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }

            return friendReqUsers.ToArray();
        }

        public Task<SphynxErrorInfo> RemoveFriendRequestAsync(Guid userId, Guid friendId, CancellationToken cancellationToken = default)
        {
            if (userId == friendId)
                return Task.FromResult(new SphynxErrorInfo(SphynxErrorCode.INVALID_USER));

            return RemoveFriendRequestAsync(userId, friendId, null, cancellationToken);
        }

        public async Task<SphynxErrorInfo<SphynxChatFriend[]?>> GetFriendsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var initiatorFilter = Builders<SphynxDbFriend>.Filter.And(
                Builders<SphynxDbFriend>.Filter.Eq(user => user.InitiatorId, userId),
                Builders<SphynxDbFriend>.Filter.Not(Builders<SphynxDbFriend>.Filter.Eq(user => user.AcceptorId, userId))
            );

            var acceptorFilter = Builders<SphynxDbFriend>.Filter.And(
                Builders<SphynxDbFriend>.Filter.Eq(user => user.AcceptorId, userId),
                Builders<SphynxDbFriend>.Filter.Not(Builders<SphynxDbFriend>.Filter.Eq(user => user.InitiatorId, userId))
            );

            var friendsFilter = Builders<SphynxDbFriend>.Filter.Or(initiatorFilter, acceptorFilter);

            var dbFriends = await _friendCollection.Find(friendsFilter).ToListAsync(cancellationToken).ConfigureAwait(false);
            return dbFriends.Select(x => x.ToDomain()).ToArray();
        }

        private async Task<SphynxErrorInfo> RemoveFriendRequestAsync(Guid userId, Guid friendId, IClientSessionHandle? session, CancellationToken ct)
        {
            // TODO: Make sure noone's resolving? (not sure this is possible, and might be nonissue)

            if (userId == friendId)
                return new SphynxErrorInfo(SphynxErrorCode.INVALID_USER);

            var initiatorFilter = Builders<SphynxDbFriendRequest>.Filter.Eq(user => user.InitiatorId, userId);
            var acceptorFilter = Builders<SphynxDbFriendRequest>.Filter.Eq(user => user.OtherId, friendId);

            var friendFilter = Builders<SphynxDbFriendRequest>.Filter.Or(initiatorFilter, acceptorFilter);

            var deleteResult = await _friendReqCollection.DeleteOneAsync(session, friendFilter, cancellationToken: ct)
                .ConfigureAwait(false);
            return deleteResult.DeletedCount > 0 ? SphynxErrorCode.SUCCESS : SphynxErrorCode.INVALID_USER;
        }

        public async Task<SphynxErrorInfo> RemoveFriendAsync(Guid userId, Guid friendId, CancellationToken cancellationToken = default)
        {
            var initiatorFilter = Builders<SphynxDbFriend>.Filter.And(
                Builders<SphynxDbFriend>.Filter.Eq(user => user.InitiatorId, userId),
                Builders<SphynxDbFriend>.Filter.Eq(user => user.AcceptorId, friendId)
            );

            var acceptorFilter = Builders<SphynxDbFriend>.Filter.And(
                Builders<SphynxDbFriend>.Filter.Eq(user => user.InitiatorId, friendId),
                Builders<SphynxDbFriend>.Filter.Eq(user => user.AcceptorId, userId)
            );

            var friendFilter = Builders<SphynxDbFriend>.Filter.Or(initiatorFilter, acceptorFilter);

            var deleteResult = await _friendCollection.DeleteOneAsync(friendFilter, cancellationToken: cancellationToken).ConfigureAwait(false);
            return deleteResult.DeletedCount > 0 ? SphynxErrorCode.SUCCESS : SphynxErrorCode.INVALID_USER;
        }

        public async Task<SphynxErrorInfo<SphynxChatFriendRequest[]?>> GetFriendRequestsAsync(Guid userId,
            FriendRequestType type = FriendRequestType.All,
            CancellationToken cancellationToken = default)
        {
            var filter = GetFriendRequestFilter(userId, type);
            var friendReqs = await _friendReqCollection.Find(filter).ToListAsync(cancellationToken).ConfigureAwait(false);
            return friendReqs.Select(x => x.ToDomain()).ToArray();
        }

        public async Task<SphynxErrorInfo<Guid[]?>> GetFriendIdsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var initiatorFilter = Builders<SphynxDbFriend>.Filter.And(
                Builders<SphynxDbFriend>.Filter.Eq(user => user.InitiatorId, userId),
                Builders<SphynxDbFriend>.Filter.Not(Builders<SphynxDbFriend>.Filter.Eq(user => user.AcceptorId, userId))
            );

            var acceptorFilter = Builders<SphynxDbFriend>.Filter.And(
                Builders<SphynxDbFriend>.Filter.Eq(user => user.AcceptorId, userId),
                Builders<SphynxDbFriend>.Filter.Not(Builders<SphynxDbFriend>.Filter.Eq(user => user.InitiatorId, userId))
            );

            var friendsFilter = Builders<SphynxDbFriend>.Filter.Or(initiatorFilter, acceptorFilter);
            var usersProjection = Builders<SphynxDbFriend>.Projection
                .Include(friends => friends.AcceptorId)
                .Include(friends => friends.InitiatorId);

            var friendReqs = _friendCollection.Find(friendsFilter).Project<SphynxDbFriend>(usersProjection);
            var friendIds = new List<Guid>();

            using var cursor = await friendReqs.ToCursorAsync(cancellationToken).ConfigureAwait(false);

            while (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
            {
                foreach (var friendReq in cursor.Current)
                {
                    friendIds.Add(friendReq.InitiatorId == userId ? friendReq.AcceptorId : friendReq.InitiatorId);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }

            return friendIds.ToArray();
        }

        public async Task<bool> IsFriendAsync(Guid userId, Guid friendId, CancellationToken cancellationToken = default)
        {
            if (userId == friendId)
                return false;

            var userFilter = Builders<SphynxDbFriend>.Filter.Or(
                Builders<SphynxDbFriend>.Filter.Eq(user => user.InitiatorId, userId),
                Builders<SphynxDbFriend>.Filter.Eq(user => user.AcceptorId, userId)
            );

            var friendFilter = Builders<SphynxDbFriend>.Filter.Or(
                Builders<SphynxDbFriend>.Filter.Eq(user => user.InitiatorId, friendId),
                Builders<SphynxDbFriend>.Filter.Eq(user => user.AcceptorId, friendId)
            );

            var friendshipFilter = Builders<SphynxDbFriend>.Filter.And(userFilter, friendFilter);

            return await _friendCollection.Find(friendshipFilter).Limit(1).CountDocumentsAsync(cancellationToken).ConfigureAwait(false) > 0;
        }

        public async Task<long> CountFriendsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var userFilter = Builders<SphynxDbFriend>.Filter.Or(
                Builders<SphynxDbFriend>.Filter.Eq(user => user.InitiatorId, userId),
                Builders<SphynxDbFriend>.Filter.Eq(user => user.AcceptorId, userId)
            );

            return await _friendCollection.CountDocumentsAsync(userFilter, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async Task<long> CountFriendRequestsAsync(Guid userId, FriendRequestType type = FriendRequestType.All,
            CancellationToken cancellationToken = default)
        {
            var filter = GetFriendRequestFilter(userId, type);
            return await _friendReqCollection.CountDocumentsAsync(filter, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        private static FilterDefinition<SphynxDbFriendRequest> GetFriendRequestFilter(Guid userId, FriendRequestType type)
        {
            FilterDefinition<SphynxDbFriendRequest> userFilter;

            switch (type)
            {
                case FriendRequestType.Incoming:
                    userFilter = Builders<SphynxDbFriendRequest>.Filter.Eq(user => user.OtherId, userId);
                    break;
                case FriendRequestType.Outgoing:
                    userFilter = Builders<SphynxDbFriendRequest>.Filter.Eq(user => user.InitiatorId, userId);
                    break;
                default:
                    userFilter = Builders<SphynxDbFriendRequest>.Filter.Or(
                        Builders<SphynxDbFriendRequest>.Filter.Eq(user => user.InitiatorId, userId),
                        Builders<SphynxDbFriendRequest>.Filter.Eq(user => user.OtherId, userId)
                    );
                    break;
            }

            return userFilter;
        }
    }
}
