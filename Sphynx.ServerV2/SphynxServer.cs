// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Sphynx.Network.Transport;
using Sphynx.ServerV2.Client;

namespace Sphynx.ServerV2
{
    /// <summary>
    /// Represents the entirety of a functional <c>Sphynx</c> server instance.
    /// </summary>
    public class SphynxServer : IDisposable
    {
        /// <summary>
        /// Returns the default IP endpoint for the server.
        /// </summary>
        public static readonly IPEndPoint DefaultEndPoint = new(Dns.GetHostEntry(Dns.GetHostName()).AddressList[1], DefaultPort);

        /// <summary>
        /// Retrieves the default port for socket information exchange between client and server.
        /// </summary>
        public static short DefaultPort => 2000;

        /// <summary>
        /// Maximum number of users in server backlog.
        /// </summary>
        public static int Backlog => 100;

        /// <summary>
        /// Returns buffer size for information exchange.
        /// </summary>
        /// <remarks>This only changes the underlying buffer size if the server is not already <see cref="Running"/>.</remarks>
        public int BufferSize { get; set; } = ushort.MaxValue;

        /// <summary>
        /// Retrieves the running state of the server.
        /// </summary>
        public bool Running => _running;

        private volatile bool _running;

        /// <summary>
        /// Returns the endpoint associated with this socket.
        /// </summary>
        public IPEndPoint EndPoint { get; private set; }

        /// <summary>
        /// The name of this <see cref="SphynxServer"/>.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The packet transporter used by clients.
        /// </summary>
        public IPacketTransporter PacketTransporter { get; set; } = new PacketTransporter();

        /// <summary>
        /// Fired on the accept thread when the server has been started.
        /// </summary>
        public event Action<SphynxServer>? OnStart;

        private Socket? _serverSocket;
        private readonly Thread _serverThread;
        private readonly object _mutationLock = new();
        private volatile bool _disposed;

        private readonly CancellationTokenSource _acceptCts = new();

        private readonly ConcurrentDictionary<Guid, SphynxClient> _connectedClients = new();

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
            _serverThread = new Thread(Run);

            Name = $"{nameof(SphynxServer)}@{_serverThread.ManagedThreadId}";
        }

        /// <summary>
        /// Starts the server on a new thread.
        /// </summary>
        /// <returns>true if the server was started as a result of this operation; false if the server has already been started.</returns>
        public bool Start()
        {
            if (_running)
                return false;

            lock (_mutationLock)
            {
                if (_running)
                    return false;

                _running = true;
                _serverThread.Start();
            }

            return true;
        }

        private void Run()
        {
            Debug.Assert(_serverSocket == null);

            _serverSocket = new Socket(EndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _serverSocket.SendBufferSize = _serverSocket.ReceiveBufferSize = BufferSize;
            _serverSocket.Bind(EndPoint);
            _serverSocket.Listen(Backlog);

            OnStart?.Invoke(this);

            while (_running)
            {
                try
                {
                    RegisterClient(_serverSocket.Accept(), _acceptCts.Token);
                }
                catch (SocketException)
                {
                    // TODO: Handle exception
                    Console.WriteLine("Interrupted");
                }
            }
        }

        private void RegisterClient(Socket clientSocket, CancellationToken cancellationToken = default) => Task.Run(() =>
        {
            // TODO: what do we do on exception
        }, cancellationToken);

        /// <inheritdoc/>
        public override string ToString() => Name;

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed)
                return;

            lock (_mutationLock)
            {
                if (_disposed)
                    return;

                _disposed = true;
                _running = false;

                foreach (var (id, _) in _connectedClients)
                {
                    bool removed = _connectedClients.TryRemove(id, out _);
                    Debug.Assert(removed);
                }

                _serverSocket?.Dispose();
                _serverThread.Join(10_000);

                if (_serverThread.IsAlive)
                    throw new TimeoutException($"Accept thread {Name} took too long to terminate");
            }
        }
    }
}
