using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using MongoDB.Driver;
using Sphynx.Packet;
using Sphynx.Server.Utils;

namespace Sphynx.Server.Storage
{
    /// <summary>
    /// Represents a store which is backed by a single MongoDB collection.
    /// </summary>
    /// <typeparam name="TDocument">The type of document stored within the MongoDB collection.</typeparam>
    public sealed class MongoStore<TDocument> : DatabaseStore<Guid, TDocument>
    {
        /// <summary>
        /// Returns settings for the database associated with the collection for this store.
        /// </summary>
        public MongoDatabaseSettings DbSettings => MongoClientPool.GetDbSettings(_databaseName);

        /// <summary>
        /// Returns settings for the collection associated with this store.
        /// </summary>
        public MongoCollectionSettings Settings => MongoClientPool.GetCollectionSettings<TDocument>(_databaseName, _collectionName);

        private readonly string? _databaseName;
        private readonly string _collectionName;

        /// <summary>
        /// Creates a new MongoDB collection store associated with the given <paramref name="collectionName"/>.
        /// </summary>
        /// <param name="collectionName">The name of the collection with which this store is registered.</param>
        public MongoStore(string collectionName) : this(null!, collectionName)
        {
        }

        /// <summary>
        /// Creates a new MongoDB collection store associated with the given <paramref name="collectionName"/>.
        /// </summary>
        /// <param name="databaseName">Name of the database for the underlying collection.</param>
        /// <param name="collectionName">The name of the collection with which this store is registered.</param>
        public MongoStore(string databaseName, string collectionName)
        {
            _databaseName = databaseName;
            _collectionName = collectionName;
        }

        /// <inheritdoc/>
        public override async Task<SphynxErrorInfo<TDocument?>> GetValueAsync(Guid id)
        {
            var client = RentClient(out var collection);

            try
            {
                // TODO: Create interface for ID property
                var filter = Builders<TDocument>.Filter.Eq<Guid>(t => default, id);

                using (var cursor = await collection.FindAsync(filter))
                {
                    return await cursor.MoveNextAsync()
                        ? new SphynxErrorInfo<TDocument?>(cursor.Current.FirstOrDefault())
                        : new SphynxErrorInfo<TDocument?>(SphynxErrorCode.INVALID_QUERY);
                }
            }
            finally
            {
                MongoClientPool.ReturnClient(client);
            }
        }

        public Task<IEnumerable<TDocument>> GetDocumentsAsync() => GetDocumentsWithAsync(_ => true);
        
        public async Task<IEnumerable<TDocument>> GetDocumentsWithAsync(Func<TDocument, bool> filter)
        {
            var client = RentClient(out var collection);

            try
            {
                return await collection.Find(t => filter(t)).ToListAsync();
            }
            finally
            {
                MongoClientPool.ReturnClient(client);
            }
        }

        /// <inheritdoc/>
        public override async Task<bool> PutAsync(Guid id, TDocument document)
        {
            var client = RentClient(out var collection);

            try
            {
                // TODO: Create interface for ID property
                var filter = Builders<TDocument>.Filter.Eq<Guid>(t => default, id);
                var replaceResult = await collection.ReplaceOneAsync(filter, document, new ReplaceOptions { IsUpsert = true });

                return replaceResult.IsAcknowledged && (!replaceResult.IsModifiedCountAvailable || replaceResult.ModifiedCount >= 1);
            }
            finally
            {
                MongoClientPool.ReturnClient(client);
            }
        }
        
        public Task InsertDocumentAsync(TDocument document)
        {
            var client = RentClient(out var collection);

            try
            {
                return collection.InsertOneAsync(document);
            }
            finally
            {
                MongoClientPool.ReturnClient(client);
            }
        }

        public Task InsertDocumentsAsync(IEnumerable<TDocument> documents)
        {
            var client = RentClient(out var collection);

            try
            {
                return collection.InsertManyAsync(documents);
            }
            finally
            {
                MongoClientPool.ReturnClient(client);
            }
        }

        public Task AddArrayElementAsync<T>(Guid id, string arrayName, T element)
        {
            var client = RentClient(out var collection);

            try
            {
                // TODO: Create interface for ID property
                var filter = Builders<TDocument>.Filter.Eq<Guid>(t => default, id);
                var update = Builders<TDocument>.Update.Push(arrayName, element);
                return collection.UpdateOneAsync(filter, update);
            }
            finally
            {
                MongoClientPool.ReturnClient(client);
            }
        }
        
        public Task UpdateFieldAsync(Guid id, string field, string value)
        {
            var client = RentClient(out var collection);

            try
            {
                // TODO: Create interface for ID property
                var filter = Builders<TDocument>.Filter.Eq<Guid>(t => default, id);
                var update = Builders<TDocument>.Update.Set(field, value);
                return collection.UpdateOneAsync(filter, update);
            }
            finally
            {
                MongoClientPool.ReturnClient(client);
            }
        }

        /// <inheritdoc/>
        public override async Task<bool> DeleteAsync(Guid id)
        {
            var client = RentClient(out var collection);

            try
            {
                // TODO: Create interface for ID property
                var filter = Builders<TDocument>.Filter.Eq<Guid>(t => default, id);
                var deleteResult = await collection.DeleteOneAsync(filter);

                return deleteResult.DeletedCount >= 1 && deleteResult.IsAcknowledged;
            }
            finally
            {
                MongoClientPool.ReturnClient(client);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private MongoClientPool.TrackingMongoClient RentClient(out IMongoCollection<TDocument> collection) =>
            RentClient(out _, out _, out collection);

        private MongoClientPool.TrackingMongoClient RentClient(out IMongoClient mongoClient, out IMongoDatabase db,
            out IMongoCollection<TDocument> collection)
        {
            var trackingClient = MongoClientPool.RentClient();

            mongoClient = trackingClient.MongoClient;
            db = trackingClient.GetDatabase(_databaseName);
            collection = trackingClient.GetCollection<TDocument>(_databaseName, _collectionName)!;

            return trackingClient;
        }
    }

    /// <summary>
    /// MongoClient pool which creates new MongoClients only as needed (i.e. max connections have been exceeded for a single client). 
    /// </summary>
    internal static class MongoClientPool
    {
        /// <summary>
        /// File which stores database information.
        /// </summary>
        public const string DB_INFO_FILE = "db.info";

        private static readonly ConcurrentQueue<TrackingMongoClient> _clients = new ConcurrentQueue<TrackingMongoClient>();

        // Lazy init - no thread-safe wrapper needed; _clients already safely handles init
        private static ConcurrentDictionary<IMongoClient, TrackingMongoClient>? _dequeuedClients;

        public static TrackingMongoClient RentClient()
        {
            TrackingMongoClient trackingClient;

            if (_clients.TryPeek(out trackingClient!))
            {
                // It would be safe to assume that we will almost never have more than 100 or so connections (MongoDB default) simultaneously
                // requesting database information; however, we still prepare in the case that we do. Perhaps also look into setting a more
                // appropriate pool size before relying on multiple client instances, since there is currently no way of disposing a
                // MongoClient instance.
                if (trackingClient.NumConnections >= trackingClient.MongoClient.Settings.MaxConnectionPoolSize)
                {
                    if (_clients.TryDequeue(out var fullClient))
                    {
                        trackingClient = EnqueueMongoClient();
                        _dequeuedClients ??= new ConcurrentDictionary<IMongoClient, TrackingMongoClient>();
                        Debug.Assert(_dequeuedClients.TryAdd(fullClient.MongoClient, fullClient));
                    }
                    // Someone beat us to this
                    else
                    {
                        // A new MongoClient should be enqueued shortly after
                        Thread.SpinWait(1);
                    }
                }
            }
            else
            {
                trackingClient = EnqueueMongoClient();
            }

            trackingClient.Rent();
            return trackingClient;
        }

        private static TrackingMongoClient EnqueueMongoClient(string customConnectStr = null!)
        {
            TrackingMongoClient trackingClient;

            IMongoClient InitializeClient()
            {
                // WARNING: Very unsafe! Only use this for testing purposes; load from file otherwise
                if (!string.IsNullOrEmpty(customConnectStr))
                {
                    return new MongoClient(customConnectStr);
                }

                using (var reader = new StreamReader(File.OpenRead(DB_INFO_FILE)))
                {
                    string connectionString = reader.ReadLine()!;
                    return new MongoClient(connectionString);
                }
            }

            _clients.Enqueue(trackingClient = new TrackingMongoClient(new Lazy<IMongoClient>(InitializeClient)));
            return trackingClient;
        }

        public static void ReturnClient(TrackingMongoClient client)
        {
            // _ == client
            if (_dequeuedClients is not null && _dequeuedClients.TryRemove(client.MongoClient, out _))
            {
                _clients.Enqueue(client);
            }

            client.Return();
        }

        public static MongoDatabaseSettings GetDbSettings(string? dbName = null)
        {
            while (true)
            {
                // Good path
                if (_clients.TryPeek(out var client))
                {
                    return client.GetDatabase(dbName).Settings;
                }

                if (_dequeuedClients?.IsEmpty ?? true)
                {
                    // There is no existing client instance. Should only occur once.
                    EnqueueMongoClient();
                    continue;
                }

                return _dequeuedClients.First().Value.GetDatabase(dbName).Settings;
            }
        }

        public static MongoCollectionSettings GetCollectionSettings<T>(string? dbName, string collectionName)
        {
            while (true)
            {
                // Good path
                if (_clients.TryPeek(out var client))
                {
                    return client.GetDatabase(dbName).GetCollection<T>(collectionName).Settings;
                }

                if (_dequeuedClients?.IsEmpty ?? true)
                {
                    // There is no existing client instance. Should only occur once.
                    EnqueueMongoClient();
                    continue;
                }

                return _dequeuedClients.First().Value.GetDatabase(dbName).GetCollection<T>(collectionName).Settings;
            }
        }

        /// <summary>
        /// MongoClient which tracks how many concurrent connections are in use.
        /// </summary>
        internal sealed class TrackingMongoClient
        {
            public IMongoClient MongoClient => _mongoClient.Value;
            private Lazy<IMongoClient> _mongoClient;

            /// <summary>
            /// Returns the default database.
            /// </summary>
            public IMongoDatabase Database => _defaultDb.Value;

            public int NumConnections => Interlocked.CompareExchange(ref _numConnections, 0, 0);
            private int _numConnections;

            private readonly Lazy<Dictionary<string, IMongoDatabase>> _databases;
            private readonly Lazy<Dictionary<string, object>> _collections;
            private readonly Lazy<IMongoDatabase> _defaultDb;

            public TrackingMongoClient(Lazy<IMongoClient> mongoClient)
            {
                _mongoClient = mongoClient;

                IMongoDatabase InitializeDefaultDb()
                {
                    using (var reader = new StreamReader(File.OpenRead(DB_INFO_FILE)))
                    {
                        reader.ReadLine();
                        string dbName = reader.ReadLine()!;
                        return MongoClient.GetDatabase(dbName);
                    }
                }

                _defaultDb = new Lazy<IMongoDatabase>(InitializeDefaultDb);
                _databases = new Lazy<Dictionary<string, IMongoDatabase>>(() => new Dictionary<string, IMongoDatabase>());
                _collections = new Lazy<Dictionary<string, object>>(() => new Dictionary<string, object>());
            }

            public IMongoDatabase GetDatabase(string? dbName = null, MongoDatabaseSettings? settings = null)
            {
                if (string.IsNullOrEmpty(dbName))
                {
                    return _defaultDb.Value;
                }

                if (!_databases.Value.TryGetValue(dbName, out var db) &&
                    !_databases.Value.TryAdd(dbName, db = MongoClient.GetDatabase(dbName, settings)))
                {
                    Debug.Assert(_databases.Value.TryGetValue(dbName, out db));
                }

                return db;
            }
            
            public IMongoCollection<T>? GetCollection<T>(string? dbName, string collectionName)
            {
                var db = GetDatabase(dbName);
                
                if (!_collections.Value.TryGetValue(collectionName, out var rawCollection) &&
                    !_collections.Value.TryAdd(collectionName, rawCollection = db.GetCollection<T>(collectionName)))
                {
                    Debug.Assert(_collections.Value.TryGetValue(collectionName, out rawCollection));
                }

                return rawCollection as IMongoCollection<T>;
            }

            internal void Rent() => Interlocked.Increment(ref _numConnections);
            internal void Return() => Interlocked.Decrement(ref _numConnections);
        }
    }
}