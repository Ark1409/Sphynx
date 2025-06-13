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
        private readonly IMongoCollection<dynamic> _collection;

        public event Action<ChatRoomInfo>? RoomCreated;
        public event Action<ChatRoomInfo>? RoomDeleted;

        public MongoRoomRepository(IMongoClient client, IMongoDatabase database, IMongoCollection<dynamic> collection)
        {
            _client = client;
            _database = database;
            _collection = collection;
        }

        public Task<SphynxErrorInfo<ChatRoomInfo?>> InsertRoomAsync(ChatRoomInfo roomInfo, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<SphynxErrorCode> UpdateRoomAsync(ChatRoomInfo updatedRoom, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<SphynxErrorCode> DeleteRoomAsync(SnowflakeId roomId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<SphynxErrorInfo<ChatRoomInfo?>> GetRoomAsync(SnowflakeId roomId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<SphynxErrorInfo<ChatRoomInfo[]?>> GetRoomsAsync(SnowflakeId[] roomIds, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<SphynxErrorInfo<T?>> GetRoomFieldAsync<T>(SnowflakeId roomId, string fieldName, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<SphynxErrorCode> UpdateRoomFieldAsync<T>(SnowflakeId roomId, string fieldName, T value,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
