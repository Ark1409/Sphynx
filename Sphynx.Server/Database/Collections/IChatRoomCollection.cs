using Sphynx.Server.ChatRooms;
using Sphynx.Server.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphynx.Server.Database.Collections
{
    internal interface IChatRoomCollection
    {
        ChatRoom? GetRoom(int roomId);
        IEnumerable<ChatRoom> GetAllRooms();
        void AddRoom(ChatRoom room);
        void UpdateRoomName(string newRoomName);
        void UpdatePassword(string newPassword);
        void AddNewUser(Guid roomId, SphynxUserInfo user);
        void AddNewMessage(Guid roomId, ChatRoomMessage message);
        void RemoveRoom(Guid roomId);
    }
}
