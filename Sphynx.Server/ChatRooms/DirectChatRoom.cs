using Sphynx.Server.Client;

namespace Sphynx.Server.ChatRooms
{
    /// <summary>
    /// Represents a direct-message channel to which people cannot be added.
    /// </summary>
    public sealed class DirectChatRoom : ChatRoom
    {
        public SphynxUserInfo UserOne { get; private set; }
        public SphynxUserInfo UserTwo { get; private set; }

        public DirectChatRoom(string name, SphynxUserInfo userOne, SphynxUserInfo userTwo) : base(name)
        {
            AddUser(UserOne = userOne);
            AddUser(UserTwo = userTwo);
        }
    }
}
