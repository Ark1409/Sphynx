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
    /// Represents a database helper using MongoDB
    /// </summary>
    public sealed class SphynxDatabase
    {
        private readonly string _prefix;
        public MongoClient Client { get; }
        public IMongoDatabase Database { get; }

        /// <summary>
        /// Constructor instance of SphynxDatabase
        /// </summary>
        /// <param name="client"></param>
        /// <param name="database"></param>
        /// <param name="prefix"></param>
        public SphynxDatabase(MongoClient client, string database, string prefix = "Sphynx")
        {
            _prefix = prefix;
            Client = client;
            Database = Client.GetDatabase(database);
        }

        /// <summary>
        /// Constructor instance of SphynxDatabase
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="client"></param>
        /// <param name="database"></param>
        /// <param name="prefix"></param>
        public SphynxDatabase(string uri, IMongoClient client, string database, string prefix = "Sphynx")
            : this(new MongoClient(uri), database, prefix)
        {

        }

        public IMongoCollection<SphynxUserInfo> UserCollection 
            => Database.GetCollection<SphynxUserInfo>(_prefix + ".users");

        public IMongoCollection<ChatRoom> RoomCollection 
            => Database.GetCollection<ChatRoom>(_prefix + ".rooms");

        /// <summary>
        /// Gets user data by ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns> Returns an instance of <see cref="SphynxUserInfo"/> </returns>
        public SphynxUserInfo? GetUser(Guid id)
        {
            var collection = UserCollection;
            return collection.Find(u => u.UserId == id).SingleOrDefault();
        }

        /// <summary>
        /// Gets user data by username
        /// </summary>
        /// <param name="username"></param>
        /// <returns> Returns an instance of user </returns>
        public SphynxUserInfo? GetUser(string username)
        {
            var collection = UserCollection;
            return collection.Find(u => u.UserName == username).SingleOrDefault();
        }

        /// <summary>
        /// Gets all user datas
        /// </summary>
        /// <returns></returns>
        public IEnumerable<SphynxUserInfo> GetUsers()
        {
            var collection = UserCollection;
            return collection.Find(u => true).ToEnumerable();
        }

        /// <summary>
        /// Inserts user data to database
        /// </summary>
        /// <param name="user"></param>
        public void InsertUser(SphynxUserInfo user)
        {
            var collection = UserCollection;
            collection.InsertOne(user);
        }

        /// <summary>
        /// Inserts many user datas to database
        /// </summary>
        /// <param name="users"></param>
        public void InsertUsers(IEnumerable<SphynxUserInfo> users)
        {
            var collection = UserCollection;
            collection.InsertMany(users);
        }

        /// <summary>
        /// Updates the user data
        /// If not exists, inserts user data to database
        /// </summary>
        /// <param name="user"></param>
        /// <param name="id"></param>
        public void UpsertUser(SphynxUserInfo user, Guid id)
        {
            var collection = UserCollection;
            collection.ReplaceOne(u => u.UserId == id, user, new ReplaceOptions { IsUpsert = true });
        }

        /// <summary>
        /// Delete user data by ID
        /// </summary>
        /// <param name="id"></param>
        public void DeleteUser(Guid id)
        {
            var collection = UserCollection;
            collection.DeleteOne(u => u.UserId == id);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="roomId"></param>
        public ChatRoom? GetRoom(Guid id)
        {
            var collection = RoomCollection;
            return collection.Find(r => r.Id == id).SingleOrDefault();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="roomName"></param>
        public ChatRoom? GetRoom(string roomName)
        {
            var collection = RoomCollection;
            return collection.Find(r => r.Name == roomName).SingleOrDefault();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ChatRoom> GetRooms()
        {
            var collection = RoomCollection;
            return collection.Find(r => true).ToEnumerable();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="room"></param>
        public void InsertRoom(ChatRoom room)
        {
            var collection = RoomCollection;
            collection.InsertOne(room);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rooms"></param>
        public void InsertRoom(IEnumerable<ChatRoom> rooms)
        {
            var collection = RoomCollection;
            collection.InsertMany(rooms);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="room"></param>
        /// <param name="id"></param>
        public void UpsertRoom(ChatRoom room, Guid id)
        {
            var collection = RoomCollection;
            collection.ReplaceOne(r => r.Id == id, room, new ReplaceOptions { IsUpsert = true });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        public void DeleteRoom(Guid id)
        {
            var collection = RoomCollection;
            collection.DeleteOne(r => r.Id == id);
        }
    }
}
