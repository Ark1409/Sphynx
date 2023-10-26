using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using Sphynx.Server.Client;

namespace Sphynx.Server
{
    /// <summary>
    /// Represents the server through which Sphynx clients communicate.
    /// </summary>
    public sealed class SphynxServer : IDisposable
    {
        /// <summary>
        /// Retrieves the encoding for information exchange between clients and server.
        /// </summary>
        public static Encoding Encoding => Encoding.UTF8;

        private readonly Socket _serverSocket;
        private readonly Thread _serverThread;
        private object _disposeLock = new object();
        private bool _disposed;

        private readonly List<SphynxUserInfo> _users;

        public ReadOnlyCollection<SphynxUserInfo> Users => _users.AsReadOnly();

        public bool Running { get; set; }

        public SphynxServer() : this(new Socket(SocketType.Stream, ProtocolType.Tcp))
        {
        }

        public SphynxServer(Socket socket)
        {
            _serverSocket = socket;
            _serverThread = new Thread(Run);
        }

        public void Start()
        {
            Running = true;
            _serverThread.Start();
        }

        private void Run()
        {
            while (Running)
            {
            }

            Dispose();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            lock (_disposeLock)
            {
                if (_disposed)
                    return;

                Running = false;
                _disposed = true;

                if (_serverSocket.IsBound)
                    _serverSocket.Shutdown(SocketShutdown.Both);

                _serverSocket.Dispose();

                if (_serverThread.IsAlive)
                    _serverThread.Join();
            }
        }
    }
}
