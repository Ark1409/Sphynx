// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Sphynx.Network.PacketV2;
using Sphynx.Network.PacketV2.Request;
using Sphynx.Network.Transport;
using Sphynx.Server.Auth.Extensions;
using Sphynx.Server.Auth.Handlers;

namespace Sphynx.Server.Auth
{
    /// <summary>
    /// Represents a client socket connection to the auth server.
    /// </summary>
    public class SphynxClient : IDisposable, IAsyncDisposable
    {
        private const int TRANSIENT_RETRY_COUNT = 5;

        /// <summary>
        /// The unique ID for this client connection.
        /// </summary>
        public Guid ClientId { get; }

        /// <summary>
        /// Indicates whether this client instance has initiated its read loop.
        /// </summary>
        public bool Started => _started;

        private volatile bool _started;

        /// <summary>
        /// Indicates whether this client instance has begun disposing.
        /// </summary>
        private bool Disposed => Volatile.Read(ref _disposed) != 0;

        private int _disposed;

        /// <summary>
        /// Event that gets fired when this client disconnects from the server.
        /// </summary>
        public event Action<SphynxClient, Exception>? OnDisconnect;

        /// <summary>
        /// The (reusable) socket instance for this client.
        /// </summary>
        internal Socket Socket { get; }

        private readonly NetworkStream _stream;

        private volatile CancellationTokenSource? _cts;

        private readonly IPacketHandler<LoginRequest> _loginHandler;
        private readonly IPacketHandler<RegisterRequest> _registerHandler;
        private readonly IPacketTransporter _packetTransporter;

        private readonly ILogger _logger;

        public SphynxClient(Socket socket, IPacketHandler packetHandler, IPacketTransporter packetTransporter, ILogger logger)
            : this(socket, packetHandler, packetHandler, packetTransporter, logger)
        {
        }

        public SphynxClient(Socket socket, IPacketHandler<LoginRequest> loginHandler, IPacketHandler<RegisterRequest> registerHandler,
            IPacketTransporter packetTransporter, ILogger logger)
            : this(Guid.NewGuid(), socket, loginHandler, registerHandler, packetTransporter, logger)
        {
        }

        public SphynxClient(Guid clientId, Socket socket, IPacketHandler packetHandler, IPacketTransporter packetTransporter, ILogger logger)
            : this(clientId, socket, packetHandler, packetHandler, packetTransporter, logger)
        {
        }

        public SphynxClient(Guid clientId, Socket socket, IPacketHandler<LoginRequest> loginHandler, IPacketHandler<RegisterRequest> registerHandler,
            IPacketTransporter packetTransporter, ILogger logger)
        {
            ClientId = clientId;
            Socket = socket;
            _stream = new NetworkStream(socket, false);

            _loginHandler = loginHandler;
            _registerHandler = registerHandler;
            _packetTransporter = packetTransporter;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (Disposed)
                return Task.FromException(new ObjectDisposedException(GetType().FullName));

            if (Started)
                return Task.CompletedTask;

            // Only the server will ever be starting clients, so it should be ok
            // to just have a normal write instead of an interlocked one.
            _started = true;

            return StartAsync(cancellationToken, TRANSIENT_RETRY_COUNT);
        }

        private async Task StartAsync(CancellationToken cancellationToken, int maxRetryAttempts)
        {
            Debug.Assert(_cts == null);

            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var ct = _cts.Token;

            for (int attempt = 1; attempt <= maxRetryAttempts; attempt++)
            {
                try
                {
                    while (!_cts.IsCancellationRequested)
                    {
                        var packet = await _packetTransporter.ReceiveAsync(_stream, ct).ConfigureAwait(false);

                        // TODO: Rate-limit
                        await HandlePacketAsync(packet, ct, TRANSIENT_RETRY_COUNT).ConfigureAwait(false);
                    }
                }
                catch (Exception ex) when (ex.IsTransient())
                {
                    _logger.LogError(ex, "[{ClientId} ({EndPoint})]: Transient error occured while reading packets. Retrying...",
                        ClientId, Socket.RemoteEndPoint);

                    if (attempt == maxRetryAttempts)
                        OnDisconnect?.Invoke(this, ex);
                }
                catch (TaskCanceledException)
                {
                    _logger.LogWarning("[{ClientId} ({EndPoint})]: Packet read aborted", ClientId, Socket.RemoteEndPoint);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[{ClientId} ({EndPoint})]: Unexpected exception occured while reading packets",
                        ClientId, Socket.RemoteEndPoint);

                    await DisposeAsync().ConfigureAwait(false);
                    break;
                }

                int retryDelay = 500 * (1 << attempt);
                await Task.Delay(retryDelay, ct).ConfigureAwait(false);
            }
        }

        private async Task HandlePacketAsync(SphynxPacket packet, CancellationToken cancellationToken, int maxRetryAttempts)
        {
            for (int attempt = 1; attempt < maxRetryAttempts; attempt++)
            {
                try
                {
                    await HandlePacketAsync(packet, cancellationToken).ConfigureAwait(false);
                    return;
                }
                catch (Exception ex) when (ex.IsTransient())
                {
                    _logger.LogError(ex, "[{ClientId} ({EndPoint})]: Transient error occured while handling packet {PacketType}. Retrying...",
                        ClientId, Socket.RemoteEndPoint, packet.PacketType);

                    if (attempt == maxRetryAttempts)
                        OnDisconnect?.Invoke(this, ex);
                }
                catch (TaskCanceledException)
                {
                    _logger.LogWarning("[{ClientId} ({EndPoint})]: Packet handling for {PacketType} aborted",
                        ClientId, Socket.RemoteEndPoint, packet.PacketType);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[{ClientId} ({EndPoint})]: Unexpected exception occured while handling packet {PacketType}",
                        ClientId, Socket.RemoteEndPoint, packet.PacketType);

                    await DisposeAsync().ConfigureAwait(false);
                    break;
                }

                int retryDelay = 500 * (1 << attempt);
                await Task.Delay(retryDelay, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task HandlePacketAsync(SphynxPacket packet, CancellationToken cancellationToken = default)
        {
            switch (packet.PacketType)
            {
                case SphynxPacketType.LOGIN_REQ:
                {
                    Debug.Assert(packet is LoginRequest);

                    await _loginHandler.HandlePacketAsync(this, (LoginRequest)packet, cancellationToken).ConfigureAwait(false);
                    break;
                }
                case SphynxPacketType.REGISTER_REQ:
                {
                    Debug.Assert(packet is RegisterRequest);

                    await _registerHandler.HandlePacketAsync(this, (RegisterRequest)packet, cancellationToken).ConfigureAwait(false);
                    break;
                }

                default:
                    await DisposeAsync().ConfigureAwait(false);
                    break;
            }
        }

        /// <summary>
        /// Sends a packet to the underlying <see cref="System.Net.Sockets.Socket"/>.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        /// <param name="cancellationToken">Cancellation token to abort the send request.</param>
        /// <returns>true if the packet could be sent; false otherwise.</returns>
        public ValueTask SendPacketAsync(SphynxPacket packet, CancellationToken cancellationToken = default)
        {
            if (Disposed)
                return ValueTask.FromException(new ObjectDisposedException(GetType().FullName));

            switch (packet.PacketType)
            {
                case SphynxPacketType.LOGIN_RES:
                case SphynxPacketType.REGISTER_RES:
                    break;

                default:
                    return ValueTask.FromException(new ArgumentException($"{nameof(SphynxAuthServer)} may only send authentication-related packets"));
            }

            return SendPacketAsync(packet, cancellationToken, TRANSIENT_RETRY_COUNT);
        }

        private async ValueTask SendPacketAsync(SphynxPacket packet, CancellationToken cancellationToken, int maxRetryAttempts)
        {
            Exception[]? exceptions = null;

            var ct = _cts?.Token ?? CancellationToken.None;

            for (int attempt = 1; attempt <= maxRetryAttempts; attempt++)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    ct.ThrowIfCancellationRequested();

                    await _packetTransporter.SendAsync(_stream, packet, cancellationToken).ConfigureAwait(false);
                    return;
                }
                catch (Exception ex) when (ex.IsTransient())
                {
                    _logger.LogError(ex, "[{ClientId} ({EndPoint})]: Transient error occured while sending packet {PacketType}. Retrying...",
                        ClientId, Socket.RemoteEndPoint, packet.PacketType);

                    exceptions ??= new Exception[maxRetryAttempts];
                    exceptions[attempt - 1] = ex;

                    if (attempt == maxRetryAttempts)
                    {
                        var exception = new AggregateException(exceptions);
                        OnDisconnect?.Invoke(this, exception);

                        throw exception;
                    }
                }
                catch (TaskCanceledException)
                {
                    _logger.LogWarning("[{ClientId} ({EndPoint})]: Packet sending for {PacketType} aborted",
                        ClientId, Socket.RemoteEndPoint, packet.PacketType);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[{ClientId} ({EndPoint})]: Unexpected exception occured while sending packet {PacketType}",
                        ClientId, Socket.RemoteEndPoint, packet.PacketType);

                    await DisposeAsync().ConfigureAwait(false);
                    throw;
                }

                int retryDelay = 500 * (1 << attempt);
                await Task.Delay(retryDelay, cancellationToken).ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
                return;

            _cts?.Cancel();
            _cts?.Dispose();

            _stream.Dispose();
            Socket.Shutdown(SocketShutdown.Both);
            Socket.Disconnect(true);
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
                return;

            _cts?.Cancel();
            _cts?.Dispose();

            await _stream.DisposeAsync().ConfigureAwait(false);
            Socket.Shutdown(SocketShutdown.Both);
            await Socket.DisconnectAsync(true).ConfigureAwait(false);
        }
    }
}
