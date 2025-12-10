// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MongoDB.Driver;
using Sphynx.Core;
using Sphynx.Model.Room;
using Sphynx.Server.Chat.Model;

namespace Sphynx.Server.Chat.Persistence.Room
{
    public class MongoRoomRepository : IRoomRepository
    {
        private readonly IMongoCollection<SphynxDbRoom> _roomCollection;
        private readonly IMongoCollection<SphynxDbGroupRoom> _groupCollection;
        private readonly IMongoCollection<SphynxDbDirectRoom> _dmCollection;
        private readonly IMongoCollection<SphynxDbGroupMembership> _memberCollection;

        public MongoRoomRepository(IMongoCollection<SphynxDbRoom> roomCollection,
            IMongoCollection<SphynxDbGroupRoom> groupCollection,
            IMongoCollection<SphynxDbDirectRoom> dmCollection,
            IMongoCollection<SphynxDbGroupMembership> memberCollection)
        {
            _roomCollection = roomCollection;
            _groupCollection = groupCollection;
            _dmCollection = dmCollection;
            _memberCollection = memberCollection;
        }

        public async Task<SphynxErrorInfo<SphynxChatDirectRoom?>> InsertDirectRoomAsync(SphynxChatDirectRoom room,
            CancellationToken cancellationToken = default)
        {
            if (room.RoomId == default)
                room.RoomId = Guid.NewGuid();

            if (room.CreatedAt == default)
                room.CreatedAt = DateTimeOffset.UtcNow;

            var dbClient = _roomCollection.Database.Client;
            using var session = await dbClient.StartSessionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            session.StartTransaction();
            {
                var dbRoom = new SphynxDbRoom
                {
                    RoomId = room.RoomId,
                    CreatedAt = room.CreatedAt,
                    RoomType = room.RoomType
                };

                await _roomCollection.InsertOneAsync(session, dbRoom, cancellationToken: cancellationToken).ConfigureAwait(false);
                await _dmCollection.InsertOneAsync(session, room.ToRecord(), cancellationToken: cancellationToken).ConfigureAwait(false);

                await session.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
                return room;
            }
        }

        public async Task<SphynxErrorInfo<SphynxChatGroupRoom?>> InsertGroupRoomAsync(SphynxChatGroupRoom room,
            CancellationToken cancellationToken = default)
        {
            if (room.OwnerId == default)
                return new SphynxErrorInfo<SphynxChatGroupRoom?>(SphynxErrorCode.INVALID_ROOM, "Owner does not exist");

            if (room.RoomId == default)
                room.RoomId = Guid.NewGuid();

            if (room.CreatedAt == default)
                room.CreatedAt = DateTimeOffset.UtcNow;

            if (string.IsNullOrWhiteSpace(room.Name))
                room.Name = $"{room.OwnerId}'s Awesome Room";

            var dbClient = _roomCollection.Database.Client;
            using var session = await dbClient.StartSessionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            session.StartTransaction();
            {
                var dbRoom = new SphynxDbRoom
                {
                    RoomId = room.RoomId,
                    CreatedAt = room.CreatedAt,
                    RoomType = room.RoomType
                };

                await _roomCollection.InsertOneAsync(session, dbRoom, cancellationToken: cancellationToken).ConfigureAwait(false);
                await _groupCollection.InsertOneAsync(session, room.ToRecord(), cancellationToken: cancellationToken).ConfigureAwait(false);

                var dbMembership = new SphynxDbGroupMembership
                {
                    MembershipId = Guid.NewGuid(),
                    RoomId = room.RoomId,
                    IsOwner = true,
                    JoinedAt = DateTimeOffset.UtcNow,
                    MemberId = room.OwnerId,
                };

                await _memberCollection.InsertOneAsync(session, dbMembership, cancellationToken: cancellationToken).ConfigureAwait(false);

                await session.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
                return room;
            }
        }

        public async Task<SphynxErrorInfo<SphynxChatRoom?>> GetRoomAsync(Guid roomId, CancellationToken cancellationToken = default)
        {
            var roomFilter = Builders<SphynxDbRoom>.Filter.Eq(x => x.RoomId, roomId);

            var dbRoom = await _roomCollection.Find(roomFilter).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            if (dbRoom is null)
                return SphynxErrorCode.INVALID_ROOM;

            switch (dbRoom.RoomType)
            {
                case SphynxRoomType.DIRECT_MSG:
                {
                    var roomResult = await GetDirectRoomAsync(roomId, cancellationToken).ConfigureAwait(false);
                    return new SphynxErrorInfo<SphynxChatRoom?>(roomResult.ErrorCode, roomResult.Message, roomResult.Data);
                }
                case SphynxRoomType.GROUP:
                {
                    var roomResult = await GetGroupRoomAsync(roomId, cancellationToken).ConfigureAwait(false);
                    return new SphynxErrorInfo<SphynxChatRoom?>(roomResult.ErrorCode, roomResult.Message, roomResult.Data);
                }
                default:
                {
                    return SphynxErrorCode.INVALID_ROOM;
                }
            }
        }

        public async Task<SphynxErrorInfo<SphynxChatDirectRoom?>> GetDirectRoomAsync(Guid roomId, CancellationToken cancellationToken = default)
        {
            var roomFilter = Builders<SphynxDbDirectRoom>.Filter.Eq(x => x.RoomId, roomId);

            var dbRoom = await _dmCollection.Find(roomFilter)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return dbRoom is null ? SphynxErrorCode.INVALID_ROOM : dbRoom.ToDomain();
        }

        public async Task<SphynxErrorInfo<SphynxChatGroupRoom?>> GetGroupRoomAsync(Guid roomId, CancellationToken cancellationToken = default)
        {
            var roomFilter = Builders<SphynxDbGroupRoom>.Filter.Eq(x => x.RoomId, roomId);

            var dbRoom = await _groupCollection.Find(roomFilter)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return dbRoom is null ? SphynxErrorCode.INVALID_ROOM : dbRoom.ToDomain();
        }

        public async Task<SphynxErrorInfo<SphynxChatRoom[]?>> GetRoomsAsync(Guid[] roomIds, CancellationToken cancellationToken = default)
        {
            var directRoomsTask = GetDirectRoomsAsync(roomIds, cancellationToken);
            var groupRoomsTask = GetGroupRoomsAsync(roomIds, cancellationToken);

            var directRoomsResult = await directRoomsTask.ConfigureAwait(false);
            var groupRoomsResult = await groupRoomsTask.ConfigureAwait(false);

            var directRooms = directRoomsResult.ErrorCode == SphynxErrorCode.SUCCESS
                ? directRoomsResult.Data!
                : Array.Empty<SphynxChatDirectRoom>();

            var groupRooms = groupRoomsResult.ErrorCode == SphynxErrorCode.SUCCESS
                ? groupRoomsResult.Data!
                : Array.Empty<SphynxChatGroupRoom>();

            var rooms = new List<SphynxChatRoom>(directRooms);
            rooms.AddRange(groupRooms);

            return rooms.ToArray();
        }

        public async Task<SphynxErrorInfo<SphynxChatDirectRoom[]?>> GetDirectRoomsAsync(Guid[] roomIds, CancellationToken cancellationToken = default)
        {
            var roomFilters = roomIds.Select(id => Builders<SphynxDbDirectRoom>.Filter.Eq(x => x.RoomId, id));
            var filter = Builders<SphynxDbDirectRoom>.Filter.Or(roomFilters);

            var dbRooms = await _dmCollection.Find(filter)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return dbRooms.Count > 0 ? dbRooms.Select(x => x.ToDomain()).ToArray() : SphynxErrorCode.INVALID_ROOM;
        }

        public async Task<SphynxErrorInfo<SphynxChatGroupRoom[]?>> GetGroupRoomsAsync(Guid[] roomIds, CancellationToken cancellationToken = default)
        {
            var roomFilters = roomIds.Select(id => Builders<SphynxDbGroupRoom>.Filter.Eq(x => x.RoomId, id));
            var filter = Builders<SphynxDbGroupRoom>.Filter.Or(roomFilters);

            var dbRooms = await _groupCollection.Find(filter)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return dbRooms.Count > 0 ? dbRooms.Select(x => x.ToDomain()).ToArray() : SphynxErrorCode.INVALID_ROOM;
        }

        public async Task<SphynxErrorInfo<SphynxChatGroupRoom?>> UpdateGroupRoomAsync(SphynxChatGroupRoom room,
            CancellationToken cancellationToken = default)
        {
            // TODO: Proper update API
            var groupFilter = Builders<SphynxDbGroupRoom>.Filter.Eq(x => x.RoomId, room.RoomId);
            var dbRoom = room.ToRecord();

            var replaceResult = await _groupCollection.FindOneAndReplaceAsync(groupFilter, dbRoom, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return replaceResult is not null ? replaceResult.ToDomain() : SphynxErrorCode.INVALID_ROOM;
        }

        public async Task<SphynxErrorInfo<SphynxChatGroupMembership?>> AddMemberAsync(Guid groupId, Guid userId,
            CancellationToken cancellationToken = default)
        {
            var dbMembership = new SphynxDbGroupMembership
            {
                MembershipId = Guid.NewGuid(),
                RoomId = groupId,
                IsOwner = false,
                JoinedAt = DateTimeOffset.UtcNow,
                MemberId = userId,
            };

            try
            {
                await _memberCollection.InsertOneAsync(dbMembership, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (MongoDuplicateKeyException)
            {
                return SphynxErrorCode.INVALID_MEMBERSHIP;
            }

            return dbMembership.ToDomain();
        }

        public async Task<SphynxErrorInfo> RemoveMemberAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default)
        {
            var membershipResult = await GetMemberAsync(groupId, userId, cancellationToken).ConfigureAwait(false);

            if (membershipResult.ErrorCode != SphynxErrorCode.SUCCESS)
                return membershipResult;

            var membership = membershipResult.Data!;

            if (membership.IsOwner)
                return new SphynxErrorInfo(SphynxErrorCode.INVALID_USER, "Cannot remove the group owner");

            var groupFilter = Builders<SphynxDbGroupMembership>.Filter.Eq(x => x.RoomId, groupId);
            var memberFilter = Builders<SphynxDbGroupMembership>.Filter.Eq(x => x.MemberId, userId);
            var notOwnerFilter = Builders<SphynxDbGroupMembership>.Filter.Eq(x => x.IsOwner, false);

            var filter = Builders<SphynxDbGroupMembership>.Filter.And(groupFilter, memberFilter, notOwnerFilter);

            var deleteResult = await _memberCollection.DeleteOneAsync(filter, cancellationToken).ConfigureAwait(false);

            return deleteResult.DeletedCount > 0 ? SphynxErrorCode.SUCCESS : SphynxErrorCode.INVALID_MEMBERSHIP;
        }

        public async Task<SphynxErrorInfo<SphynxChatGroupMembership?>> GetMemberAsync(Guid groupId, Guid userId,
            CancellationToken cancellationToken = default)
        {
            var groupFilter = Builders<SphynxDbGroupMembership>.Filter.Eq(x => x.RoomId, groupId);
            var memberFilter = Builders<SphynxDbGroupMembership>.Filter.Eq(x => x.MemberId, userId);

            var filter = Builders<SphynxDbGroupMembership>.Filter.And(groupFilter, memberFilter);

            var membership = await _memberCollection.Find(filter).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            return membership == null ? SphynxErrorCode.INVALID_MEMBERSHIP : membership.ToDomain();
        }

        public async Task<SphynxErrorInfo<SphynxChatGroupMembership[]?>> GetMembersAsync(Guid groupId, CancellationToken cancellationToken = default)
        {
            var groupFilter = Builders<SphynxDbGroupMembership>.Filter.Eq(x => x.RoomId, groupId);

            var dbMembers = await _memberCollection.Find(groupFilter).ToListAsync(cancellationToken).ConfigureAwait(false);

            return dbMembers.Select(x => x.ToDomain()).ToArray();
        }

        public async Task<SphynxErrorInfo<Guid[]?>> GetMemberIdsAsync(Guid groupId, CancellationToken cancellationToken = default)
        {
            var groupFilter = Builders<SphynxDbGroupMembership>.Filter.Eq(x => x.RoomId, groupId);
            var projection = Builders<SphynxDbGroupMembership>.Projection.Include(x => x.MemberId);

            // TODO: ...
            var dbMembers = await _memberCollection.Find(groupFilter)
                .Project<SphynxDbGroupMembership>(projection)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return dbMembers.Select(x => x.MemberId).ToArray();
        }

        public async Task<bool> IsMemberAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default)
        {
            var groupFilter = Builders<SphynxDbGroupMembership>.Filter.Eq(x => x.RoomId, groupId);
            var memberFilter = Builders<SphynxDbGroupMembership>.Filter.Eq(x => x.MemberId, userId);

            var filter = Builders<SphynxDbGroupMembership>.Filter.And(groupFilter, memberFilter);

            return await _memberCollection.Find(filter).Limit(1).CountDocumentsAsync(cancellationToken).ConfigureAwait(false) > 0;
        }

        public async Task<long> CountMembersAsync(Guid groupId, CancellationToken cancellationToken = default)
        {
            var groupFilter = Builders<SphynxDbGroupMembership>.Filter.Eq(x => x.RoomId, groupId);
            return await _memberCollection.CountDocumentsAsync(groupFilter, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async Task<SphynxErrorInfo> DeleteRoomAsync(Guid roomId, CancellationToken cancellationToken = default)
        {
            var roomFilter = Builders<SphynxDbRoom>.Filter.Eq(x => x.RoomId, roomId);

            var dbClient = _roomCollection.Database.Client;
            using var session = await dbClient.StartSessionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            session.StartTransaction();
            {
                var deletedRoom = await _roomCollection
                    .FindOneAndDeleteAsync(session, roomFilter, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                switch (deletedRoom.RoomType)
                {
                    case SphynxRoomType.DIRECT_MSG:
                    {
                        var dmFilter = Builders<SphynxDbDirectRoom>.Filter.Eq(x => x.RoomId, roomId);

                        var deleteResult = await _dmCollection
                            .DeleteOneAsync(session, dmFilter, cancellationToken: cancellationToken)
                            .ConfigureAwait(false);

                        if (deleteResult.DeletedCount <= 0)
                        {
                            await session.AbortTransactionAsync(CancellationToken.None).ConfigureAwait(false);
                            return SphynxErrorCode.INVALID_ROOM;
                        }

                        await session.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
                        return SphynxErrorCode.SUCCESS;
                    }
                    case SphynxRoomType.GROUP:
                    {
                        var groupFilter = Builders<SphynxDbGroupRoom>.Filter.Eq(x => x.RoomId, roomId);

                        var deleteResult = await _groupCollection
                            .DeleteOneAsync(session, groupFilter, cancellationToken: cancellationToken)
                            .ConfigureAwait(false);

                        if (deleteResult.DeletedCount <= 0)
                        {
                            await session.AbortTransactionAsync(CancellationToken.None).ConfigureAwait(false);
                            return SphynxErrorCode.INVALID_ROOM;
                        }

                        await session.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
                        return SphynxErrorCode.SUCCESS;
                    }
                    default:
                        await session.AbortTransactionAsync(CancellationToken.None).ConfigureAwait(false);
                        return SphynxErrorCode.INVALID_ROOM;
                }
            }
        }

        public async Task<SphynxErrorInfo> DeleteDirectRoomAsync(Guid roomId, CancellationToken cancellationToken = default)
        {
            var roomFilter = Builders<SphynxDbRoom>.Filter.Eq(x => x.RoomId, roomId);
            var dmFilter = Builders<SphynxDbDirectRoom>.Filter.Eq(x => x.RoomId, roomId);

            var dbClient = _roomCollection.Database.Client;
            using var session = await dbClient.StartSessionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            session.StartTransaction();
            {
                var dmDeleteTask = _dmCollection.DeleteOneAsync(session, dmFilter, cancellationToken: cancellationToken);
                var roomDeleteTask = _roomCollection.DeleteOneAsync(session, roomFilter, cancellationToken: cancellationToken);

                var deleteResult = await dmDeleteTask.ConfigureAwait(false);

                if (deleteResult.DeletedCount <= 0)
                {
                    await session.AbortTransactionAsync(CancellationToken.None).ConfigureAwait(false);
                    return SphynxErrorCode.INVALID_ROOM;
                }

                await roomDeleteTask.ConfigureAwait(false);

                deleteResult = await dmDeleteTask.ConfigureAwait(false);

                if (deleteResult.DeletedCount <= 0)
                {
                    await session.AbortTransactionAsync(CancellationToken.None).ConfigureAwait(false);
                    return SphynxErrorCode.INVALID_ROOM;
                }

                await session.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
                return SphynxErrorCode.SUCCESS;
            }
        }

        public async Task<SphynxErrorInfo> DeleteGroupRoomAsync(Guid roomId, CancellationToken cancellationToken = default)
        {
            var roomFilter = Builders<SphynxDbRoom>.Filter.Eq(x => x.RoomId, roomId);
            var dmFilter = Builders<SphynxDbGroupRoom>.Filter.Eq(x => x.RoomId, roomId);

            var dbClient = _roomCollection.Database.Client;
            using var session = await dbClient.StartSessionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            session.StartTransaction();
            {
                var groupDeleteTask = _groupCollection.DeleteOneAsync(session, dmFilter, cancellationToken: cancellationToken);
                var roomDeleteTask = _roomCollection.DeleteOneAsync(session, roomFilter, cancellationToken: cancellationToken);

                var deleteResult = await groupDeleteTask.ConfigureAwait(false);

                if (deleteResult.DeletedCount <= 0)
                {
                    await session.AbortTransactionAsync(CancellationToken.None).ConfigureAwait(false);
                    return SphynxErrorCode.INVALID_ROOM;
                }

                await roomDeleteTask.ConfigureAwait(false);

                deleteResult = await groupDeleteTask.ConfigureAwait(false);

                if (deleteResult.DeletedCount <= 0)
                {
                    await session.AbortTransactionAsync(CancellationToken.None).ConfigureAwait(false);
                    return SphynxErrorCode.INVALID_ROOM;
                }

                await session.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
                return SphynxErrorCode.SUCCESS;
            }
        }
    }
}
