// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Sphynx.Network.PacketV2;
using Sphynx.ServerV2.Client;
using Sphynx.ServerV2.Extensions;
using Sphynx.Storage;

namespace Sphynx.ServerV2
{
    /// <summary>
    /// Represents a TCP-oriented <see cref="SphynxServer"/> which accepts <see cref="SphynxPacket"/>s from clients.
    /// </summary>
    public class SphynxTcpServer : SphynxServer
    {
        /// <summary>
        /// The profile with which to configure the server.
        /// </summary>
        public override SphynxTcpServerProfile Profile { get; }

        /// <summary>
        /// The accept socket for the server.
        /// </summary>
        protected Socket? ServerSocket { get; private set; }

        private readonly ConcurrentDictionary<Guid, SphynxTcpClient> _connectedClients = new();
        private WeakObjectPool<Socket>? _socketPool;

        private readonly SemaphoreSlim _disposeSemaphore = new(1, 1);
        private bool _disposed;

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

            Profile.Logger.LogDebug("Initializing socket pool");

            _socketPool = new WeakObjectPool<Socket>(Profile.Backlog);

            Profile.Logger.LogDebug("Initializing listening socket");

            ServerSocket = new Socket(Profile.EndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            ServerSocket.SendBufferSize = ServerSocket.ReceiveBufferSize = Profile.BufferSize;
            ServerSocket.Bind(Profile.EndPoint);
            ServerSocket.Listen(Profile.Backlog);

            Profile.Logger.LogInformation("Started \"{Name}\" at {DateTime} on {EndPoint}", Name, DateTime.Now, Profile.EndPoint);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (!_socketPool.TryTake(out var socket))
                        Profile.Logger.LogTrace("Socket pool exhausted, will allocate on demand to handle the next connection");

                    socket = await ServerSocket.AcceptAsync(socket, cancellationToken).ConfigureAwait(false);

                    if (Profile.Logger.IsEnabled(LogLevel.Information))
                        Profile.Logger.LogInformation("Accepted client on {Address}", socket.RemoteEndPoint);

                    QueueStartClient(socket, cancellationToken);
                }
                catch (Exception ex) when (ex.IsCancellationException())
                {
                    // Server stopped
                }
                catch (Exception ex)
                {
                    Profile.Logger.LogCritical(ex, "Unexpected error in server accept loop");
                }
            }
        }

        private void QueueStartClient(Socket clientSocket, CancellationToken cancellationToken)
        {
            var state = new StartClientState
            {
                Server = this,
                Socket = clientSocket,
                Token = cancellationToken,
            };

            ThreadPool.QueueUserWorkItem(static async void (s) =>
            {
                var server = s.Server;
                var socket = s.Socket;
                var ct = s.Token;

                SphynxTcpClient? client = null;

                try
                {
                    client = server.CreateTcpClient(socket);
                }
                catch (Exception ex)
                {
                    if (server.Profile.Logger.IsEnabled(LogLevel.Error))
                        server.Profile.Logger.LogError(ex, "An error occured while initializing client for endpoint {EndPoint}",
                            socket.RemoteEndPoint);

                    await server.DisposeClientAsync(client).ConfigureAwait(false);
                    return;
                }

                if (server.Profile.Logger.IsEnabled(LogLevel.Debug))
                    server.Profile.Logger.LogDebug("Initialized client instance for endpoint {EndPoint}", socket.RemoteEndPoint);

                // Last-chance check
                if (ct.IsCancellationRequested)
                {
                    await server.DisposeClientAsync(client).ConfigureAwait(false);
                    return;
                }

                try
                {
                    await server.StartClientAsync(client, ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (server.Profile.Logger.IsEnabled(LogLevel.Error))
                        server.Profile.Logger.LogError(ex, "Unhandled exception while starting client for endpoint {EndPoint}", client.EndPoint);

                    await server.DisposeClientAsync(client).ConfigureAwait(false);
                }
            }, state, false);
        }

        private readonly struct StartClientState
        {
            public SphynxTcpServer Server { get; init; }
            public Socket Socket { get; init; }
            public CancellationToken Token { get; init; }
        }

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
            return new SphynxTcpClient(clientSocket, Profile);
        }

        private async Task StartClientAsync(SphynxTcpClient client, CancellationToken cancellationToken)
        {
            bool insertedClient = _connectedClients.TryAdd(client.ClientId, client);
            Debug.Assert(insertedClient);

            OnClientConnected(client);

            using (client.Logger.BeginScope(client.EndPoint))
            {
                try
                {
                    await client.StartAsync(cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    await DisposeClientAsync(client, true).ConfigureAwait(false);
                }
            }
        }

        private async ValueTask<bool> DisposeClientAsync(SphynxTcpClient? client, bool tryReuse = false)
        {
            if (client is null)
                return false;

            if (tryReuse)
            {
                _connectedClients.Remove(client.ClientId, out _);

                try
                {
                    await client.DisposeAsync(disposeSocket: false).ConfigureAwait(false);

                    // Test for disposal or invalid state
                    try
                    {
                        await client.Socket.DisconnectAsync(true).ConfigureAwait(false);
                    }
                    catch (SocketException ex) when (ex.SocketErrorCode == SocketError.NotConnected)
                    {
                        // TODO: Assume not disposed?
                    }

                    _socketPool!.Return(client.Socket);
                    return true;
                }
                catch
                {
                    client.Socket.Dispose();
                }
            }
            else
            {
                try
                {
                    await client.DisposeAsync().ConfigureAwait(false);
                }
                catch
                {
                    // Ignore disposal exceptions
                }
            }

            return false;
        }

        public override async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            await _disposeSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                await DisposeServerAsync().ConfigureAwait(false);
                await DisposeClientsAsync().ConfigureAwait(false);

                await base.DisposeAsync().ConfigureAwait(false);
            }
            finally
            {
                _disposeSemaphore.Release();
                _disposed = true;
            }
        }

        private async ValueTask DisposeServerAsync()
        {
            await StopAsync(waitForFinish: true).ConfigureAwait(false);

            Debug.Assert(_disposeSemaphore.CurrentCount == 0);

            if (ServerSocket is not null && ServerSocket.Connected)
            {
                await ServerSocket.DisconnectAsync(false).ConfigureAwait(false);
                ServerSocket.Shutdown(SocketShutdown.Both);
                ServerSocket.Dispose();
            }
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
