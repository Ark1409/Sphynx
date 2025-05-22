// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MongoDB.Driver;
using Sphynx.Core;
using Sphynx.ModelV2.Room;
using Sphynx.ServerV2.Persistence.User;

namespace Sphynx.ServerV2.Persistence.Room
{
    public class MongoRoomRepository : IRoomRepository
    {
        private readonly IMongoClient _client;
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<SphynxSelfInfo> _collection;

        public event Action<IChatRoomInfo>? RoomCreated;
        public event Action<IChatRoomInfo>? RoomDeleted;

        public MongoRoomRepository(IMongoClient client, IMongoDatabase database, IMongoCollection<SphynxSelfInfo> collection)
        {
            _client = client;
            _database = database;
            _collection = collection;
        }

        public Task<SphynxErrorInfo<IChatRoomInfo?>> CreateRoomAsync(IChatRoomInfo roomInfo, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<SphynxErrorCode> UpdateRoomAsync(IChatRoomInfo updatedRoom, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<SphynxErrorCode> DeleteRoomAsync(SnowflakeId roomId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<SphynxErrorInfo<IChatRoomInfo?>> GetRoomAsync(SnowflakeId roomId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<SphynxErrorInfo<IChatRoomInfo[]?>> GetRoomsAsync(SnowflakeId[] roomIds, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<SphynxErrorInfo<T?>> GetRoomFieldAsync<T>(SnowflakeId roomId, string fieldName, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<SphynxErrorCode> UpdateRoomFieldAsync<T>(SnowflakeId roomId, string fieldName, T value, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
