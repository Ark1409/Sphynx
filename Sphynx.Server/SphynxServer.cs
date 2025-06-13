using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Sphynx.Server.Utils;
using Sphynx.Server.Client;

namespace Sphynx.Server
{
    /// <summary>
    /// Represents the server through which Sphynx clients communicate.
    /// </summary>
    public sealed class SphynxServer : IDisposable
    {
        /// <summary>
        /// Returns the default IP endpoint for the server.
        /// </summary>
        // TODO: New one each time?
        public static IPEndPoint DefaultEndPoint => new IPEndPoint(Dns.GetHostEntry(Dns.GetHostName()).AddressList[1], DefaultPort);

        /// <summary>
        /// Retrieves the default port for socket information exchange between clients and servers.
        /// </summary>
        public static short DefaultPort => 2000;

        /// <summary>
        /// Maximum number of users in server backlog.
        /// </summary>
        public static int Backlog => 100;

        /// <summary>
        /// Returns buffer size for information exchange. This only changes the underlying buffer size if
        /// the server is not <see cref="Running"/>.
        /// </summary>
        public int BufferSize { get; set; } = ushort.MaxValue;

        /// <summary>
        /// Retrieves the running state of the server.
        /// </summary>
        public bool Running { get; set; }

        /// <summary>
        /// Returns the endpoint associated with this socket.
        /// </summary>
        public IPEndPoint EndPoint { get; private set; }

        /// <summary>
        /// The name of this <see cref="SphynxServer"/>.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Fired when the server has been started.
        /// </summary>
        public event Action<SphynxServer> OnStart;

        private Socket? _serverSocket;
        private readonly Thread _serverThread;
        private readonly ConcurrentDictionary<Socket, SphynxClient> _connectedClients;
        private readonly object _mutationLock = new object();
        private bool _disposed;

        /// <summary>
        /// Creates a new Sphynx server, associating it with <see cref="DefaultEndPoint"/>.
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
            OnStart = null!;
            _serverThread = new Thread(Run);
            _connectedClients = new ConcurrentDictionary<Socket, SphynxClient>();
            Name = $"{nameof(SphynxServer)}@{_serverThread.ManagedThreadId.ToString()}";
        }

        /// <summary>
        /// Starts the server on a new thread.
        /// </summary>
        /// <returns>true if the server was started as a result of this operation; false if the server had
        /// already been started.</returns>
        public bool Start()
        {
            if (Running) return false;
            
            lock (_mutationLock)
            {
                if (Running) return false;
                
                Running = true;
                _serverThread.Start();
            }

            return true;
        }

        private void Run()
        {
            _serverSocket = new Socket(EndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _serverSocket.SendBufferSize = _serverSocket.ReceiveBufferSize = BufferSize;
            _serverSocket.Bind(EndPoint);
            _serverSocket.Listen(Backlog);

            OnStart?.Invoke(this);

            while (Running)
            {
                try
                {
                    var userSocket = _serverSocket.Accept();
                    AcceptClientAsync(userSocket).SafeBackgroundExecute();
                }
                catch (SocketException)
                {
                    // TODO: Handle exception
                    Console.WriteLine("Interrupted");
                }
            }
        }

        // TODO: Implement "accept and receive" functionality
        private Task AcceptClientAsync(Socket clientSocket) => Task.Factory.StartNew(() =>
        {
            var client = SphynxClientManager.AddAnonymousClient(this, clientSocket);
            
            // <see cref="SphynxClient(SphynxServer, Socket)"/>
            if (client.Connected && !client.Started)
            {
                client.StartAsync().SafeBackgroundExecute();
                Debug.Assert(_connectedClients.TryAdd(client.SocketStream.Socket, client));
            }
        });
        
        /// <inheritdoc/>
        public override string ToString() => Name;

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed) return;

            lock (_mutationLock)
            {
                if (_disposed) return;

                _disposed = true;
                Running = false;

                foreach (var (socket, _) in _connectedClients)
                {
                    Debug.Assert(_connectedClients.Remove(socket, out var client));
                    SphynxClientManager.RemoveClient(client);
                }

                if (_serverThread.IsAlive)
                {
                    _serverSocket?.Dispose();
                    _serverThread.Join();
                }
            }
        }
    }
}