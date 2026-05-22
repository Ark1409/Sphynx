// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MongoDB.Driver;
using Sphynx.Core;
using Sphynx.Model.Room;
using Sphynx.Server.Persistence.User;

namespace Sphynx.Server.Persistence.Room
{
    public class MongoRoomRepository : IRoomRepository
    {
        private readonly IMongoClient _client;
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<dynamic> _collection;

        public event Action<SphynxRoomInfo>? RoomCreated;
        public event Action<SphynxRoomInfo>? RoomDeleted;

        public MongoRoomRepository(IMongoClient client, IMongoDatabase database, IMongoCollection<dynamic> collection)
        {
            _client = client;
            _database = database;
            _collection = collection;
        }

        public Task<SphynxErrorInfo<SphynxRoomInfo?>> InsertRoomAsync(SphynxRoomInfo roomInfo, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<SphynxErrorInfo> UpdateRoomAsync(SphynxRoomInfo updatedRoom, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<SphynxErrorInfo> DeleteRoomAsync(SnowflakeId roomId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<SphynxErrorInfo<SphynxRoomInfo?>> GetRoomAsync(SnowflakeId roomId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<SphynxErrorInfo<SphynxRoomInfo[]?>> GetRoomsAsync(SnowflakeId[] roomIds, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<SphynxErrorInfo<T?>> GetRoomFieldAsync<T>(SnowflakeId roomId, string fieldName, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<SphynxErrorInfo> UpdateRoomFieldAsync<T>(SnowflakeId roomId, string fieldName, T value,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
