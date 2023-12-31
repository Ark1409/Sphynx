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
        void UpdateRoomName(Guid roomId, string newRoomName);
        void UpdatePassword(Guid roomId, string newPassword);
        void AddNewUser(Guid roomId, SphynxUserInfo user);
        void AddNewMessage(Guid roomId, ChatRoomMessage message);
        void RemoveRoom(Guid roomId);
        void RemoveMessage(Guid roomId, ChatRoomMessage message);
        void RemoveUser(Guid roomId, Guid userId);

        // Async methods

        Task<ChatRoom?> GetRoomAsync(int roomId);
        Task<IEnumerable<ChatRoom>> GetAllRoomsAsync();
        Task AddRoomAsync(ChatRoom room);
        Task UpdateRoomNameAsync(string newRoomName);
        Task UpdatePasswordAsync(string newPassword);
        Task AddNewUserAsync(Guid roomId, SphynxUserInfo user);
        Task AddNewMessageAsync(Guid roomId, ChatRoomMessage message);
        Task RemoveRoomAsync(Guid roomId);
    }
}
