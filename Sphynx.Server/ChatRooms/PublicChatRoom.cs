using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Sphynx.Server.Client;

namespace Sphynx.Server.ChatRooms
{
    public sealed class PublicChatRoom : ChatRoom
    {
        public PublicChatRoom(string name) : base(name)
        {

        }
    }
}
