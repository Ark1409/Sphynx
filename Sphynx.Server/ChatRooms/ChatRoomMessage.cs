using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Sphynx.Server.Client;

namespace Sphynx.Server.ChatRooms
{
    public delegate void ChatRoomMessageEvent(ChatRoomMessage message);

    public sealed class ChatRoomMessage
    {
        public DateTime Timestamp { get; private set; }
        public SphynxUserInfo Sender { get; private set; }
        public string Content { get; private set; }

        public ChatRoomMessage(SphynxUserInfo user, string content) : this(DateTime.Now, user, content)
        {
        }

        public ChatRoomMessage(DateTime timestamp, SphynxUserInfo user, string content)
        {
            Timestamp = timestamp;
            Sender = user;
            Content = content ?? throw new ArgumentNullException(nameof(content));
        }
    }
}
