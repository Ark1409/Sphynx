// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Sphynx.Core;
using Sphynx.Network.PacketV2.Request;
using Sphynx.Network.Transport;
using Sphynx.Server.Auth.Handlers;
using Sphynx.Server.Auth.Services;
using Sphynx.ServerV2;
using Sphynx.ServerV2.Persistence;
using Sphynx.ServerV2.Persistence.User;
using Sphynx.Storage;

namespace Sphynx.Server.Auth
{
    /// <summary>
    /// Represents the auth server instance which accepts clients on a specific endpoint.
    /// </summary>
    public class SphynxAuthServer : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Returns the default IP endpoint for the server.
        /// </summary>
        public static readonly IPEndPoint DefaultEndPoint = new(IPAddress.Any, DefaultPort);

        /// <summary>
        /// Retrieves the default port for socket information exchange between client and server.
        /// </summary>
        public static short DefaultPort => 2000;

        /// <summary>
        /// Maximum number of clients in server socket backlog.
        /// </summary>
        public static int Backlog => 256;

        /// <summary>
        /// Returns buffer size for information exchange.
        /// </summary>
        public int BufferSize { get; set; } = 8192;

        /// <summary>
        /// Retrieves the running state of the server.
        /// </summary>
        public bool Running => Volatile.Read(ref _running) != 0;

        // 0 = not running, 1 = running
        private int _running;

        /// <summary>
        /// Returns the endpoint associated with this socket.
        /// </summary>
        public IPEndPoint EndPoint { get; }

        /// <summary>
        /// The name of this <see cref="SphynxServer"/>.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Logging instance used by the server and its clients.
        /// </summary>
        public ILogger Logger { get; set; } = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger(nameof(SphynxAuthServer));

        /// <summary>
        /// The packet transporter used by clients.
        /// </summary>
        public IPacketTransporter PacketTransporter { get; init; } = new PacketTransporter();

        public IPasswordHasher PasswordHasher { get; init; } = new Pbkdf2PasswordHasher();
        public IUserRepository UserRepository { get; init; }
        public IPacketHandler<LoginRequest> LoginHandler { get; init; }
        public IPacketHandler<RegisterRequest> RegisterHandler { get; init; }

        private Socket? _serverSocket;
        private CancellationTokenSource? _acceptCts;

        private int _disposed;

        private readonly ConcurrentDictionary<Guid, SphynxClient> _connectedClients = new();
        private WeakObjectPool<Socket>? _socketPool;

        /// <summary>
        /// Creates (but does not start) a new <c>Sphynx</c> authentication server, associating it with <see cref="DefaultEndPoint"/>.
        /// </summary>
        public SphynxAuthServer() : this(DefaultEndPoint)
        {
        }

        /// <summary>
        /// Creates (but does not start) a new <c>Sphynx</c> authentication server, associating it with the given <param name="ipAddress">
        /// and </param><see cref="DefaultPort"/>.
        /// </summary>
        /// <param name="ipAddress">The ip address to run to server on.</param>
        public SphynxAuthServer(IPAddress ipAddress) : this(new IPEndPoint(ipAddress, DefaultPort))
        {
        }

        /// <summary>
        /// Creates (but does not start) a new <c>Sphynx</c> authentication server and associates it
        /// with the specified <paramref name="serverEndpoint"/>.
        /// </summary>
        /// <param name="serverEndpoint">The server endpoint to bind to.</param>
        public SphynxAuthServer(IPEndPoint serverEndpoint)
        {
            EndPoint = serverEndpoint;
            Name = $"{GetType().Name}@{EndPoint.Address}:{EndPoint.Port}";

            UserRepository = new NullUserRepository()!;

            if (LoginHandler is null)
                LoginHandler = new LoginHandler(UserRepository, PasswordHasher, Logger);

            if (RegisterHandler is null)
                RegisterHandler = new RegisterHandler(UserRepository, PasswordHasher, Logger);
        }

        /// <summary>
        /// Starts the server asynchronously.
        /// </summary>
        /// <remarks>This method returns immediately if the server has already been started.</remarks>
        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
                return Task.CompletedTask;

            return RunAsync(cancellationToken);
        }

        private async Task RunAsync(CancellationToken cancellationToken = default)
        {
            Debug.Assert(_serverSocket == null);
            Debug.Assert(_socketPool == null);
            Debug.Assert(_acceptCts == null);

            Logger.LogInformation("Initializing socket pool");

            _socketPool = new WeakObjectPool<Socket>(Backlog);
            _acceptCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            Logger.LogInformation("Initializing listening socket");

            _serverSocket = new Socket(EndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _serverSocket.SendBufferSize = _serverSocket.ReceiveBufferSize = BufferSize;
            _serverSocket.Bind(EndPoint);
            _serverSocket.Listen(Backlog);

            Logger.LogInformation("Server started at {DateTime} on {Address}:{Port}", DateTime.Now, EndPoint.Address, EndPoint.Port);

            while (Running)
            {
                if (_acceptCts.IsCancellationRequested)
                    break;

                try
                {
                    _socketPool.TryTake(out var socket);

                    Logger.LogInformation("Listening for client...");
                    socket = await _serverSocket.AcceptAsync(socket, cancellationToken).ConfigureAwait(false);
                    Logger.LogInformation("Accepted client on {Address}", socket.RemoteEndPoint);

                    StartClient(socket, _acceptCts.Token);
                }
                catch (Exception ex)
                {
                    Logger.LogCritical(ex, "Unexpected error in server read loop");
                }
            }

            await DisposeAsync().ConfigureAwait(false);
        }

        private void StartClient(Socket clientSocket, CancellationToken cancellationToken) => ThreadPool.QueueUserWorkItem(async void (ct) =>
        {
            try
            {
                var client = new SphynxClient(clientSocket, LoginHandler, RegisterHandler, PacketTransporter, Logger);

                bool insertedClient = _connectedClients.TryAdd(client.ClientId, client);
                Debug.Assert(insertedClient);

                client.OnDisconnect += (c, ex) =>
                {
                    Logger.LogError(ex, "[{EndPoint}]: Client disconnected", c.Socket.RemoteEndPoint);
                    c.Dispose();
                    _socketPool!.Return(c.Socket);
                };

                await client.StartAsync(ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[{EndPoint}]: Unhandled exception in client read loop", clientSocket.RemoteEndPoint);
            }
        }, cancellationToken, false);

        /// <inheritdoc/>
        public override string ToString() => Name;

        public void Dispose()
        {
            if (_disposed != 0 || Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
                return;

            Volatile.Write(ref _running, 0);
            DisposeClients();
            DisposeServer();
        }

        private void DisposeClients()
        {
            foreach (var (_, client) in _connectedClients)
            {
                client.Dispose();
            }
        }

        private void DisposeServer()
        {
            _acceptCts?.Cancel();
            _acceptCts?.Dispose();

            _serverSocket?.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            try
            {
                Dispose();
                return ValueTask.CompletedTask;
            }
            catch (Exception ex)
            {
                return ValueTask.FromException(ex);
            }
        }
    }
}
