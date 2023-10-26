using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
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
        /// Retrieves the encoding for information exchange between clients and servers.
        /// </summary>
        public static Encoding Encoding => Encoding.UTF8;

        /// <summary>
        /// Returns the default IP endpoint for the server.
        /// </summary>
        // TODO: New one each time?
        public static IPEndPoint DefaultEndPoint => new IPEndPoint(Dns.GetHostEntry(Dns.GetHostName()).AddressList[0], Port);

        /// <summary>
        /// Retrieves the port for socket information exchange between clients and servers.
        /// </summary>
        public static short Port => 2000;

        /// <summary>
        /// Maximum number of users in server backlog.
        /// </summary>
        public static int Backlog => 10;

        /// <summary>
        /// Returns buffer size for information exchange.
        /// </summary>
        public static int BufferSize => ushort.MaxValue;

        /// <summary>
        /// Returns the users connected to this server.
        /// </summary>
        public ReadOnlyCollection<SphynxUserInfo> Users => _users.AsReadOnly();

        private readonly List<SphynxUserInfo> _users;

        /// <summary>
        /// Retrives the running state of the server.
        /// </summary>
        public bool Running { get; set; }

        /// <summary>
        /// Returns the endpoint associated with this socket.
        /// </summary>
        public IPEndPoint EndPoint { get; private set; }

        private readonly Socket _serverSocket;

        private readonly Thread _serverThread;
        private object _disposeLock = new object();
        private bool _disposed;

        /// <summary>
        /// Creates a new Sphynx server, associating it with <see cref="EndPoint"/>.
        /// </summary>
        public SphynxServer() : this(DefaultEndPoint)
        {

        }

        /// <summary>
        /// Creates a new Sphynx server and associates it with the specified <paramref name="serverEndpoint"/>.
        /// </summary>
        /// <param name="serverEndpoint">The server endpoint to bind to.</param>
        public SphynxServer(IPEndPoint serverEndpoint)
        {
            EndPoint = serverEndpoint;
            _serverSocket = new Socket(serverEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _serverThread = new Thread(Run);

            _users = new List<SphynxUserInfo>();
        }

        /// <summary>
        /// Starts the server on a new thread.
        /// </summary>
        public void Start()
        {
            Running = true;
            _serverThread.Start();
        }

        private void Run()
        {
            _serverSocket.Bind(EndPoint);
            _serverSocket.Listen(Backlog);
            _serverSocket.Blocking = false;

            while (Running)
            {
                try
                {
                    AddUser(_serverSocket.Accept());
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.WouldBlock)
                    {
                    }
                }

                Thread.Sleep(1000);
                Console.CursorLeft = 0;
                Console.CursorTop = Console.WindowHeight - 1;
                Console.Write($"Running server @ {SphynxApp.Server!.EndPoint}.");
            }
        }

        private void AddUser(Socket clientSocket) => _users.Add(new SphynxUserInfo(clientSocket));

        /// <inheritdoc/>
        public void Dispose()
        {
            lock (_disposeLock)
            {
                if (_disposed)
                    return;

                _disposed = true;

                Running = false;
                if (_serverThread.IsAlive)
                    _serverThread.Join();

                for (int i = 0; i < _users.Count; i++)
                    _users[i].Dispose();

                _serverSocket.Dispose();
            }
        }
    }
}
