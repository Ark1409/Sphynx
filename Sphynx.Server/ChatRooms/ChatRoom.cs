using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Attributes;
using Sphynx.Server.Client;

namespace Sphynx.Server.ChatRooms
{
    public abstract class ChatRoom
    {
        [BsonId]
        public virtual Guid Id { get; protected set; }
        public virtual string Name { get; protected set; }
        public virtual string? Password { get; protected set; }
        [BsonRequired]
        public virtual ICollection<SphynxUserInfo> Users => _users.Values;
        public virtual List<ChatRoomMessage> Messages { get; protected set; }
        public event ChatRoomMessageEvent? MessageAdded;

        protected readonly Dictionary<Guid, SphynxUserInfo> _users;

        public ChatRoom() : this(null!)
        {

        }

        public ChatRoom(string name)
        {
            Name = name;
            _users = new Dictionary<Guid, SphynxUserInfo>();
            Messages = new List<ChatRoomMessage>();
            Id = Guid.NewGuid();
        }

        public virtual void AddMessage(ChatRoomMessage message)
        {
            Messages.Add(message ?? throw new ArgumentNullException(nameof(message)));
            MessageAdded?.Invoke(message);
        }

        public virtual SphynxUserInfo? GetUser(string name)
        {
            foreach (var id in _users.Keys)
            {
                var user = _users[id];

                if (user.UserName.Equals(name))
                    return user;
            }

            return null;
        }

        protected virtual bool AddUser(SphynxUserInfo user)
        {
            if (!_users.ContainsKey(user.UserId))
            {
                _users.Add(user.UserId, user);
                return true;
            }

            return false;
        }


        public virtual SphynxUserInfo? GetUser(Guid id) => _users.TryGetValue(id, out var user) ? user : null;
    }
}
