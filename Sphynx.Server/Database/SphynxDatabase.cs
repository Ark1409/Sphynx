using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Sphynx.Server.Client;
using Sphynx.Server.ChatRooms;

namespace Sphynx.Server.Database
{
    /// <summary>
    /// Represents a generic class as MongoDB helper
    /// </summary>
    public sealed class SphynxDatabase<T> where T : class
    {
        private readonly string _prefix;
        public MongoClient Client { get; }
        public IMongoDatabase Database { get; }
        public IMongoCollection<T> Collection { get; }

        /// <summary>
        /// Constructor instance of SphynxDatabase
        /// </summary>
        public SphynxDatabase(MongoClient client, string database, string collection, string prefix = "Sphynx")
        {
            _prefix = prefix;
            try
            {
                Client = client;
                Database = Client.GetDatabase(database);
                Collection = Database.GetCollection<T>(collection);
            }
            catch (MongoConfigurationException ex)
            {
                // Console.WriteLine("Failed to connect to MongoDB (MongoEx): " + ex.Message);
                // Console.WriteLine(ex.StackTrace);
                throw;
            }
            catch (Exception ex)
            {
                // Console.WriteLine("Failed to connect to MongoDB (SystemEx): " + ex.Message);
                // Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        /// <summary>
        /// Constructor instance of SphynxDatabase
        /// </summary>
        public SphynxDatabase(string uri, string database, string collection, string prefix = "Sphynx")
            : this(new MongoClient(uri), database, collection, prefix)
        {

        }

        /// <summary>
        /// Constructor instance of SphynxDatabase
        /// </summary>
        public SphynxDatabase(string collection, string prefix = "Sphynx")
            : this(new MongoClient("mongodb+srv://admin:{password}@sphynxcluster.vpdimph.mongodb.net/"),
                  "Sphynx", collection, prefix)
        {

        }

        public T? GetOneDocumentByID(Guid id)
        {
            var collection = Collection;
            var filter = Builders<T>.Filter.Eq("_id", id);
            return collection.Find(filter).SingleOrDefault();
        }

        public T? GetOneDocumentByField(string field, string value)
        {
            var collection = Collection;
            var filter = Builders<T>.Filter.Eq(field, value);
            return collection.Find(filter).SingleOrDefault();
        }

        public IEnumerable<T> GetAllDocuments()
        {
            var collection = Collection;
            return collection.Find(t => true).ToEnumerable();
        }

        public void AddOneDocument(T document)
        {
            var collection = Collection;
            collection.InsertOne(document);
        }

        public void AddManyDocuments(IEnumerable<T> documents)
        {
            var collection = Collection;
            collection.InsertMany(documents);
        }

        public void AddElementToArrayInDocument(Guid id, string arrayName, object element)
        {
            var collection = Collection;
            var filter = Builders<T>.Filter.Eq("_id", id);
            var update = Builders<T>.Update.Push(arrayName, element);
            collection.UpdateOne(filter, update);
        }

        public void UpdateFieldInDocument(Guid id, string field, string value)
        {
            var collection = Collection;
            var filter = Builders<T>.Filter.Eq("_id", id);
            var update = Builders<T>.Update.Set(field, value);
            collection.UpdateOne(filter, update);
        }

        public void ReplaceOneDocument(Guid id, T document)
        {
            var collection = Collection;
            var filter = Builders<T>.Filter.Eq("_id", id);
            collection.ReplaceOne(filter, document);
        }

        public void UpsertOneDocument(Guid id, T document)
        {
            var collection = Collection;
            var filter = Builders<T>.Filter.Eq("_id", id);
            collection.ReplaceOne(filter, document, new ReplaceOptions { IsUpsert = true });
        }

        public void DeleteOneDocumentByID(Guid id)
        {
            var collection = Collection;
            var filter = Builders<T>.Filter.Eq("_id", id);
            collection.DeleteOne(filter);
        }
    }
}
