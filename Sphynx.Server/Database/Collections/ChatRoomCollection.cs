using Sphynx.Server.ChatRooms;
using Sphynx.Server.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.public async Tasks;

namespace Sphynx.Server.Database.Collections
{
    internal class ChatRoomCollection
    {
        private readonly SphynxDatabase<ChatRoom> _database = new SphynxDatabase<ChatRoom>("rooms");

        public ChatRoom? GetRoom(Guid roomId)
        {
            return _database.GetOneDocumentByID(roomId);
        }

        public IEnumerable<ChatRoom> GetRooms()
        {
            return _database.GetAllDocuments();
        }

        public ICollection<SphynxUserInfo>? GetAllUsersInRoom(Guid roomId)
        {
            return GetRoom(roomId)?.Users;
        }

        public void AddRoom(ChatRoom room)
        {
            _database.AddOneDocument(room);
        }

        public void UpdateRoomName(Guid roomId,  string newRoomName)
        {
            _database.UpdateFieldInDocument(roomId, "Name", newRoomName);
        }

        public void UpdatePassword(Guid roomId, string newPassword)
        {
            _database.UpdateFieldInDocument(roomId, "Password", newPassword);
        }

        public void AddNewUser(Guid roomId, SphynxUserInfo user)
        {
            _database.AddElementToArrayInDocument(roomId, "Users", user);
        }

        public void AddNewMessage(Guid roomId, ChatRoomMessage message)
        {
            _database.AddElementToArrayInDocument(roomId, "Messages", message);
        }

        public void DeleteRoom(Guid roomId)
        {
            _database.DeleteOneDocumentByID(roomId);
        }

        public void RemoveMessage(Guid roomId, ChatRoomMessage message)
        {
            _database.RemoveElementFromArrayInDocument(roomId, "Messages", message);
        }

        public void RemoveUser(Guid roomId, Guid userId)
        {
            _database.RemoveDocumentFromNestedCollection<SphynxUserInfo>(roomId, "Users", userId);
        }

        // Async methods

        public async Task<ChatRoom?> GetRoomAsync(Guid roomId)
        {
            return await _database.GetOneDocumentByIDAsync(roomId);
        }

        public async Task<IEnumerable<ChatRoom>> GetAllRoomsAsync()
        {
            return await _database.GetAllDocumentsAsync();
        }

        public async Task AddRoomAsync(ChatRoom room)
        {
            await _database.AddOneDocumentAsync(room);
        }

        public async Task UpdateRoomNameAsync(Guid roomId, string newRoomName)
        {
            await _database.UpdateFieldInDocumentAsync(roomId, "Name", newRoomName);
        }
        
        public async Task UpdatePasswordAsync(Guid roomId, string newPassword)
        {
            await _database.UpdateFieldInDocumentAsync(roomId, "Password", newPassword);
        }

        public async Task AddNewUserAsync(Guid roomId, SphynxUserInfo user)
        {
            await _database.AddElementToArrayInDocumentAsync(roomId, "Users", user);
        }

        public async Task AddNewMessageAsync(Guid roomId, ChatRoomMessage message)
        {
            await _database.AddElementToArrayInDocumentAsync(roomId, "Messages", message);
        }

        public async Task RemoveRoomAsync(Guid roomId)
        {
            await _database.DeleteOneDocumentByIDAsync(roomId);
        }
    }
}
