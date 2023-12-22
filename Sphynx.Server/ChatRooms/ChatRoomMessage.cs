using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using Sphynx.Server.Client;

namespace Sphynx.Server.ChatRooms
{
    public delegate void ChatRoomMessageEvent(ChatRoomMessage message);

    public sealed class ChatRoomMessage
    {
        public DateTime Timestamp { get; private set; }
        public ChatRoom Room { get; private set; }
        public SphynxUserInfo Sender { get; private set; }
        public string Content { get; private set; }

        public ChatRoomMessage(ChatRoomMessageData data)
        {

        }

        public ChatRoomMessage(ChatRoom room, SphynxUserInfo user, string content) : this(room, DateTime.Now, user, content)
        {
        }

        public ChatRoomMessage(ChatRoom room, DateTime timestamp, SphynxUserInfo user, string content)
        {
            Room = room;
            Timestamp = timestamp;
            Sender = user;
            Content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public ChatRoomMessageData Serialize(Encoding encoding)
        {
            //long timestamp = Timestamp.ToBinary();
            //byte[] roomId = Room.Id.ToByteArray();
            //byte[] senderId = Sender.UserId.ToByteArray();
            //byte[] senderName = encoding.GetBytes(Sender.UserName);
            //byte[] content = encoding.GetBytes(Content);

            //return new ChatRoomMessageData(timestamp, roomId, senderId, senderName, content);
            return default;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 16, Size = 128)]
    public unsafe struct ChatRoomMessageData
    {
        public readonly byte[] Timestamp;
        public fixed byte RoomId[1001];
        public readonly long SenderId;
        public readonly long SenderName;

        public ChatRoomMessageData(byte[] timestamp, long senderId, long senderName)
        {
            Timestamp = timestamp;
            //RoomId = roomId;
            SenderId = senderId;
            SenderName = senderName;
        }
    }
}
