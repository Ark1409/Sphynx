using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Sphynx.Server.Database
{
    public sealed class SphynxDatabase
    {
        private readonly string _prefix;
        public MongoClient Client { get; }
        public IMongoDatabase Database { get; }

        public SphynxDatabase(MongoClient client, string database, string prefix = "Sphynx")
        {
            _prefix = prefix;
            Client = client;
            Database = Client.GetDatabase(database);
        }

        public SphynxDatabase(string uri, IMongoClient client, string database, string prefix = "Sphynx")
            : this(new MongoClient(uri), database, prefix) { }

        public IMongoCollection<BsonDocument> UserCollection => Database.GetCollection<BsonDocument>(_prefix + ".users");
        public IMongoCollection<BsonDocument> RoomCollection => Database.GetCollection<BsonDocument>(_prefix + ".rooms");
    }
}
