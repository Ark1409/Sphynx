using MongoDB.Driver;
using Sphynx.Server.ChatRooms;
using Sphynx.Server.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphynx.Server.Database.Collections
{
    internal class UserCollection
    {
        private SphynxDatabase<SphynxUserInfo> _database = new SphynxDatabase<SphynxUserInfo>("users");

        public SphynxUserInfo? GetUser(Guid userId)
        {
            return _database.GetOneDocumentByID(userId);
        }

        public SphynxUserInfo? GetUser(string username)
        {
            return _database.GetOneDocumentByField("UserName", username);
        }

        IEnumerable<SphynxUserInfo> GetAllUsers()
        {
            return _database.GetAllDocuments();
        }

        public void AddUser(SphynxUserInfo user)
        {
            _database.AddOneDocument(user);
        }

        public void UpdateUserName(Guid id, string newUsername)
        {
            _database.UpdateFieldInDocument(id, "UserName", newUsername);
        }

        public void UpdateEmail(Guid id, string newEmail)
        {
            _database.UpdateFieldInDocument(id, "Email", newEmail);
        }

        public void EditUserInfo(Guid id, SphynxUserInfo user)
        {
            _database.ReplaceOneDocument(id, user);
        }

        public void DeleteUser(Guid userId)
        {
            _database.DeleteOneDocumentByID(userId);
        }
    }
}
