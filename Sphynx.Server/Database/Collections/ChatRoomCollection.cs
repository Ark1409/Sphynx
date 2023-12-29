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
        private SphynxDatabase<ChatRoom> _database = new SphynxDatabase<ChatRoom>("rooms");

        IEnumerable<ChatRoom> GetRooms()
        {
            return _database.GetAllDocuments();
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
    }
}
