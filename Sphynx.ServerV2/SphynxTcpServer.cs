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

                    StartClient(socket);
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

        // TODO: Fix to be allocation-free
        private void StartClient(Socket clientSocket) => ThreadPool.QueueUserWorkItem(async void (socket) =>
        {
            if (Profile.Logger.IsEnabled(LogLevel.Debug))
                Profile.Logger.LogDebug("Initializing client instance for endpoint {EndPoint}", socket.RemoteEndPoint);

            SphynxTcpClient? client = null;

            try
            {
                client = CreateTcpClient(socket);
            }
            catch (Exception ex)
            {
                if (Profile.Logger.IsEnabled(LogLevel.Error))
                    Profile.Logger.LogError(ex, "An error occured while initializing client for endpoint {EndPoint}", socket.RemoteEndPoint);

                await DisposeClientAsync(client).ConfigureAwait(false);
                return;
            }

            // Last-chance check
            if (ServerCts.IsCancellationRequested)
            {
                await DisposeClientAsync(client).ConfigureAwait(false);
                return;
            }

            try
            {
                await StartClientAsync(client, ServerCts.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Profile.Logger.IsEnabled(LogLevel.Error))
                    Profile.Logger.LogError(ex, "Unhandled exception while starting client for endpoint {EndPoint}", client.EndPoint);

                await DisposeClientAsync(client).ConfigureAwait(false);
            }
        }, clientSocket, false);

        /// <summary>
        /// Creates (but does not start) a suitable <see cref="SphynxTcpClient"/> for the accepted <paramref name="clientSocket"/>.
        /// </summary>
        /// <param name="clientSocket">The accepted client socket.</param>
        /// <returns>A <see cref="SphynxTcpClient"/> instance for the accepted <paramref name="clientSocket"/>.</returns>
        protected virtual SphynxTcpClient CreateTcpClient(Socket clientSocket)
        {
            return new SphynxTcpClient(clientSocket, Profile);
        }

        private async ValueTask StartClientAsync(SphynxTcpClient client, CancellationToken cancellationToken)
        {
            using (client.Logger.BeginScope(client.EndPoint))
            {
                bool insertedClient = _connectedClients.TryAdd(client.ClientId, client);

                Debug.Assert(insertedClient);

                client.OnDisconnect += (c, ex) =>
                {
                    c.Logger.LogInformation(ex, "Client disconnected");

                    c.Dispose();
                    _connectedClients.Remove(c.ClientId, out _);
                    _socketPool!.Return(c.Socket);
                };

                try
                {
                    await client.StartAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    client.Logger.LogError(ex, "Unhandled exception in client read loop");
                }
            }
        }

        private async ValueTask DisposeClientAsync(SphynxTcpClient? client)
        {
            if (client is null)
                return;

            try
            {
                await client.DisposeAsync().ConfigureAwait(false);
            }
            catch
            {
                // Ignore disposal exceptions
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ShutdownServer();
                DisposeClients();
            }

            base.Dispose(disposing);
        }

        private void ShutdownServer()
        {
            ServerCts.Cancel();
            ServerSocket?.Dispose();
        }

        private void DisposeClients()
        {
            // We don't want any extra clients being added in during the dispose process
            Debug.Assert(ServerCts.IsCancellationRequested);
            Debug.Assert(!ServerSocket?.Connected ?? true);

            try
            {
                if (!_connectedClients.IsEmpty)
                    Parallel.ForEach(_connectedClients, kvp => kvp.Value.Dispose());
            }
            catch
            {
                // Ignore disposal exceptions
            }

            _connectedClients.Clear();
        }
    }
}
