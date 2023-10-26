using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Sphynx.Server.Client
{
    public sealed class SphynxUserInfo : IDisposable
    {
        public Socket UserSocket { get; private set; }

        public SphynxUserStatus UserStatus { get; private set; }

        public string UserName { get; private set; }

        public string Email { get; private set; }

        private bool _disposed;

        public SphynxUserInfo(Socket socket)
        {
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            UserSocket.Shutdown(SocketShutdown.Both);
            UserSocket.Dispose();
        }
    }
}
