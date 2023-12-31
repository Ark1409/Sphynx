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
        private readonly SphynxDatabase<SphynxUserInfo> _database = new SphynxDatabase<SphynxUserInfo>("users");

        public SphynxUserInfo? GetUser(Guid userId)
        {
            return _database.GetOneDocumentByID(userId);
        }

        public SphynxUserInfo? GetUser(string username)
        {
            return _database.GetOneDocumentByField("UserName", username);
        }

        public IEnumerable<SphynxUserInfo> GetAllUsers()
        {
            return _database.GetAllDocuments();
        }

        public void AddUser(SphynxUserInfo user)
        {
            _database.AddOneDocument(user);
        }

        public void UpdateUserName(Guid userId, string newUsername)
        {
            _database.UpdateFieldInDocument(userId, "UserName", newUsername);
        }

        public void UpdateEmail(Guid userId, string newEmail)
        {
            _database.UpdateFieldInDocument(userId, "Email", newEmail);
        }

        public void EditUserInfo(Guid userId, SphynxUserInfo user)
        {
            _database.ReplaceOneDocument(userId, user);
        }

        public void DeleteUser(Guid userId)
        {
            _database.DeleteOneDocumentByID(userId);
        }

        // Async methods

        async Task<SphynxUserInfo?> GetUserAsync(Guid userId)
        {
            return await _database.GetOneDocumentByIDAsync(userId);
        }

        async Task<SphynxUserInfo?> GetUserAsync(string username)
        {
            return await _database.GetOneDocumentByFieldAsync("UserName", username);
        }

        async Task<IEnumerable<SphynxUserInfo>> GetAllUsersAsync()
        {
            return await _database.GetAllDocumentsAsync();
        }

        async Task AddUserAsync(SphynxUserInfo user)
        {
            await _database.AddOneDocumentAsync(user);
        }

        async Task UpdateUserNameAsync(Guid userId, string newUsername)
        {
            await _database.UpdateFieldInDocumentAsync(userId, "UserName", newUsername);
        }

        async Task UpdateEmailAsync(Guid userId, string newEmail)
        {
            await _database.UpdateFieldInDocumentAsync(userId, "Email", newEmail);
        }

        async Task EditUserInfoAsync(Guid userId, SphynxUserInfo user)
        {
            await _database.ReplaceOneDocumentAsync(userId, user);
        }

        async Task DeleteUserAsync(Guid userId)
        {
            await _database.DeleteOneDocumentByIDAsync(userId);
        }
    }
}
