// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Sphynx.Network.Packet;
using Sphynx.Server.Client;
using Sphynx.Storage;
using Sphynx.Utils;

namespace Sphynx.Server
{
    /// <summary>
    /// Represents a TCP-oriented <see cref="SphynxServer"/> which accepts <see cref="SphynxMessage"/>s from <see cref="SphynxTcpClient"/>s.
    /// </summary>
    public class SphynxTcpServer : SphynxServer
    {
        /// <inheritdoc/>
        public override SphynxTcpServerProfile Profile { get; }

        /// <summary>
        /// The accept socket for the server.
        /// </summary>
        protected Socket? ServerSocket { get; private set; }

        private readonly ConcurrentDictionary<Guid, SphynxTcpClient> _connectedClients = new();
        private ObjectPool<Socket>? _socketPool;

        private readonly SemaphoreSlim _disposeSemaphore = new(1, 1);
        private bool _disposed;

        public SphynxTcpServer(IPEndPoint endpoint) : this(endpoint, null)
        {
        }

        public SphynxTcpServer(IPEndPoint endpoint, string? name) : this(new SphynxTcpServerProfile { EndPoint = endpoint }, name)
        {
        }

        public SphynxTcpServer(SphynxTcpServerProfile profile) : this(profile, null)
        {
        }

        public SphynxTcpServer(SphynxTcpServerProfile profile, string? name) : base(profile, name)
        {
            Profile = profile;
        }

        protected sealed override async Task OnStartAsync(CancellationToken cancellationToken)
        {
            Debug.Assert(ServerSocket == null);
            Debug.Assert(_socketPool == null);

            Logger.LogDebug("Initializing socket pool");

            _socketPool = new ObjectPool<Socket>(Profile.Backlog);

            Logger.LogDebug("Initializing listening socket");

            ServerSocket = new Socket(Profile.EndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            ServerSocket.SendBufferSize = ServerSocket.ReceiveBufferSize = Profile.BufferSize;
            ServerSocket.Bind(Profile.EndPoint);
            ServerSocket.Listen(Profile.Backlog);

            Logger.LogInformation("Started \"{Name}\" at {DateTime} on {EndPoint}", Name, DateTime.Now, Profile.EndPoint);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (!_socketPool.TryTake(out var socket))
                        Logger.LogTrace("Socket pool exhausted: socket will be allocated on-demand for the next connection");

                    socket = await ServerSocket.AcceptAsync(socket, cancellationToken).ConfigureAwait(false);

                    if (Logger.IsEnabled(LogLevel.Information))
                        Logger.LogInformation("Accepted client on {Address}", socket.RemoteEndPoint);

                    StartClient(socket, cancellationToken);
                }
                catch (Exception) when (cancellationToken.IsCancellationRequested)
                {
                    // Server stopped
                }
                catch (Exception ex)
                {
                    Logger.LogCritical(ex, "Unexpected error in server accept loop");
                }
            }
        }

        private void StartClient(Socket clientSocket, CancellationToken token) => ThreadPoolHelper.QueueUserWorkItem(static async void (s) =>
        {
            var server = s.server;
            var socket = s.socket;
            var token = s.token;

            SphynxTcpClient? client = null;

            try
            {
                client = server.CreateTcpClient(socket);
            }
            catch (Exception ex)
            {
                if (server.Logger.IsEnabled(LogLevel.Error))
                    server.Logger.LogError(ex, "An error occured while initializing client for endpoint {EndPoint}",
                        socket.RemoteEndPoint);

                await server.DisposeClientAsync(client).ConfigureAwait(false);
                return;
            }

            if (server.Logger.IsEnabled(LogLevel.Debug))
                server.Logger.LogDebug("Initialized client instance for endpoint {EndPoint}", socket.RemoteEndPoint);

            try
            {
                await server.RunClientAsync(client, token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (server.Logger.IsEnabled(LogLevel.Error))
                    server.Logger.LogError(ex, "Unhandled exception while starting client {Client}", client.ToString());

                if (!await server.DisposeClientAsync(client, tryReuse: true).ConfigureAwait(false) && server.Logger.IsEnabled(LogLevel.Trace))
                    server.Logger.LogTrace("Unable to reuse socket from client {Client}", client.ToString());
            }
        }, (server: this, socket: clientSocket, token));

        /// <summary>
        /// Called once a client connects to the server. This is executed before the client's run loop.
        /// </summary>
        /// <param name="client">The client that connected.</param>
        protected virtual void OnClientConnected(SphynxTcpClient client) { }

        /// <summary>
        /// Creates (but does not start) a suitable <see cref="SphynxTcpClient"/> for the accepted <paramref name="clientSocket"/>.
        /// </summary>
        /// <param name="clientSocket">The accepted client socket.</param>
        /// <returns>A <see cref="SphynxTcpClient"/> instance for the accepted <paramref name="clientSocket"/>.</returns>
        protected virtual SphynxTcpClient CreateTcpClient(Socket clientSocket)
        {
            return new SphynxTcpClient(clientSocket, new SphynxTcpClientOptions(Profile));
        }

        private Task RunClientAsync(SphynxTcpClient client, CancellationToken cancellationToken)
        {
            bool insertedClient = _connectedClients.TryAdd(client.ClientId, client);
            Debug.Assert(insertedClient);

            OnClientConnected(client);
            return client.RunAsync(cancellationToken);
        }

        private async ValueTask<bool> DisposeClientAsync(SphynxTcpClient? client, bool tryReuse = false)
        {
            if (client is null)
                return false;

            if (!tryReuse || !_connectedClients.TryRemove(new KeyValuePair<Guid, SphynxTcpClient>(client.ClientId, client)))
            {
                await client.DisposeAsync().ConfigureAwait(false);
                return false;
            }

            var socket = client.DetachSocket();
            if (socket is null)
            {
                await client.DisposeAsync().ConfigureAwait(false);
                return false;
            }

            // Test for disposal or invalid state before attempting to return to the pool
            if (socket.Connected || !_socketPool!.Return(socket))
            {
                socket.Dispose();
                await client.DisposeAsync().ConfigureAwait(false);
                return false;
            }

            await client.DisposeAsync().ConfigureAwait(false);
            return true;
        }

        public override async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            await _disposeSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                if (_disposed)
                    return;

                await DisposeServerAsync().ConfigureAwait(false);
                await DisposeClientsAsync().ConfigureAwait(false);

                await base.DisposeAsync().ConfigureAwait(false);
            }
            finally
            {
                _disposed = true;
                _disposeSemaphore.Release();
            }
        }

        private async ValueTask DisposeServerAsync()
        {
            Debug.Assert(_disposeSemaphore.CurrentCount == 0);

            await StopAsync(waitForFinish: true).ConfigureAwait(false);

            ServerSocket?.Dispose();

            while (_socketPool?.TryTake(out var socket) ?? false)
                socket.Dispose();
        }

        private async Task DisposeClientsAsync()
        {
            // We don't want any extra clients being added in during the dispose process
            Debug.Assert(!ServerSocket?.Connected ?? true);
            Debug.Assert(_disposeSemaphore.CurrentCount == 0);

            if (_connectedClients.IsEmpty)
                return;

            try
            {
                await Parallel.ForEachAsync(_connectedClients, (kvp, _) => kvp.Value.DisposeAsync()).ConfigureAwait(false);
            }
            catch
            {
                // Ignore disposal exceptions
            }

            _connectedClients.Clear();
        }
    }
}
