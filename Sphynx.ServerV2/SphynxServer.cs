// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Sphynx.Core;
using Sphynx.Network.Transport;
using Sphynx.ServerV2.Client;

namespace Sphynx.ServerV2
{
    /// <summary>
    /// Represents the server instance which accepts clients on a specific endpoint.
    /// </summary>
    public abstract class SphynxServer : IDisposable, IAsyncDisposable
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
        public int BufferSize { get; protected set; } = ushort.MaxValue;

        /// <summary>
        /// Retrieves the running state of the server.
        /// </summary>
        public bool Running => _running;

        private volatile bool _running;

        /// <summary>
        /// Returns the endpoint associated with this socket.
        /// </summary>
        public IPEndPoint EndPoint { get; protected set; }

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

        /// <summary>
        /// The accept socket for the server.
        /// </summary>
        protected Socket? ServerSocket { get; private set; }

        /// <summary>
        /// The cancellation
        /// </summary>
        protected CancellationTokenSource AcceptCts { get; } = new();

        private readonly Thread _serverThread;
        private readonly object _mutationLock = new();
        private volatile bool _disposed;

        // TODO: Abstract away to inteface (for Redis)
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

            Name = $"{GetType().Name}@{_serverThread.ManagedThreadId}";
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
            Debug.Assert(ServerSocket == null);

            ServerSocket = new Socket(EndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            ServerSocket.SendBufferSize = ServerSocket.ReceiveBufferSize = BufferSize;
            ServerSocket.Bind(EndPoint);
            ServerSocket.Listen(Backlog);

            OnStart?.Invoke(this);

            while (_running)
            {
                if (AcceptCts.IsCancellationRequested)
                {
                    _running = false;
                    break;
                }

                try
                {
                    // TODO: Accept async and reuse sockets
                    RegisterClient(ServerSocket.Accept(), AcceptCts.Token);
                }
                catch (SocketException)
                {
                    // TODO: Handle exception
                    Console.WriteLine("Interrupted");
                }
            }
        }

        private void RegisterClient(Socket clientSocket, CancellationToken cancellationToken) => Task.Run(async () =>
        {
            var clientId = Guid.NewGuid();
            var userId = SnowflakeId.NewId();
            // TODO: what do we do on exception
            var client = new SphynxClient(clientSocket, clientId, userId, PacketTransporter);

            bool insertedClient = _connectedClients.TryAdd(clientId, client);
            Debug.Assert(insertedClient);

            await client.StartAsync(cancellationToken);
        }, cancellationToken);

        /// <inheritdoc/>
        public override string ToString() => Name;

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            lock (_mutationLock)
            {
                if (_disposed)
                    return;

                _disposed = true;
                _running = false;

                DisposeServer();
            }
        }

        private void DisposeServer()
        {
            AcceptCts.Cancel();
            AcceptCts.Dispose();

            ServerSocket?.Dispose();

            if (_serverThread.IsAlive)
            {
                _serverThread.Join(30_000);
                throw new TimeoutException($"Accept thread {Name} took too long to terminate");
            }
        }

        public virtual ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
