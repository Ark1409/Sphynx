using Sphynx.Server.ChatRooms;
using Sphynx.Server.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphynx.Server.Database.Collections
{
    internal class ChatRoomCollection
    {
        private SphynxDatabase<ChatRoom> _database;

        ChatRoom? GetRoom(Guid roomId)
        {
            return _database.GetOneDocumentByID(roomId);
        }

        IEnumerable<ChatRoom> GetRooms()
        {
            return _database.GetAllDocuments();
        }

        void UpdateRoomName(Guid roomId,  string newRoomName)
        {
            _database.UpdateFieldInDocument(roomId, "Name", newRoomName);
        }

        void UpdatePassword(Guid roomId, string newPassword)
        {
            _database.UpdateFieldInDocument(roomId, "Password", newPassword);
        }

        void AddNewUser(Guid roomId, SphynxUserInfo user)
        {
            _database.AddElementToArrayInDocument(roomId, "Users", user);
        }

        void AddNewMessage(Guid roomId, ChatRoomMessage message)
        {
            _database.AddElementToArrayInDocument(roomId, "Messages", message);
        }

        void DeleteRoom(Guid roomId)
        {
            _database.DeleteOneDocumentByID(roomId);
        }
    }
}
