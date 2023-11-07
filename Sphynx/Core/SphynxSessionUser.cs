using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

using UserId = System.Guid;

namespace Sphynx.Core
{
    public class SphynxSessionUser : IDisposable
    {
        public enum UserStatus : byte
        {
            OFFLINE = 0,
            ONLINE,
            AWAY,
            DND
        }

        public UserId Id { get; private set; }

        public Socket? UserSocket { get; internal set; }

        public string? SessionId { get; private set; }

        public SphynxSessionUser(Guid id, Socket userSocket, string sessionId)
        {
            Id = id;
            UserSocket = userSocket;
            SessionId = sessionId;
        }

        public void Dispose()
        {
            if(!UserSocket!.Connected)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }
}
