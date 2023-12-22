using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphynx.Server.Client
{
    public enum SocketMessageType : byte
    {
        StatusUpdate = 1,
        RoomRequest,
        MessageSend
    }
}
