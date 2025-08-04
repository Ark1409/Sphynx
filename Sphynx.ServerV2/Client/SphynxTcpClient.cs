// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Sphynx.Network.PacketV2;
using Sphynx.Network.Transport;
using Sphynx.ServerV2.Extensions;
using Sphynx.ServerV2.Infrastructure.Handlers;
using Sphynx.ServerV2.Infrastructure.Routing;

namespace Sphynx.ServerV2.Client
{
    /// <summary>
    /// Represents a TCP client socket connection to a <see cref="SphynxTcpServer"/>.
    /// </summary>
    public class SphynxTcpClient : ISphynxClient, IAsyncDisposable
    {
        /// <inheritdoc/>
        public Guid ClientId { get; }

        /// <inheritdoc/>
        public IPEndPoint EndPoint { get; }

        /// <summary>
        /// Retrieves the running state of the client.
        /// </summary>
        public bool IsRunning => !_clientTask?.IsCompleted ?? false;

        /// <summary>
        /// The read task for the client's read loop.
        /// </summary>
        protected Task? ClientTask => _clientTask;

        private volatile Task? _clientTask;

        /// <summary>
        /// Whether the client has been disconnected.
        /// </summary>
        public bool Disconnected => _disconnectReserved;

        private volatile bool _disconnectReserved;

        /// <summary>
        /// A reference to the accepted client socket.
        /// </summary>
        internal Socket Socket { get; private set; }

        /// <summary>
        /// Event that is fired when this client disconnects from the server. This may run concurrently
        /// with <see cref="DisposeAsync()"/>.
        /// </summary>
        public event Func<SphynxTcpClient, Exception?, Task>? OnDisconnect;

        /// <summary>
        /// The logger created for this client.
        /// </summary>
        protected internal ILogger Logger { get; }

        /// <summary>
        /// The packet transporter which is used to send and receive <see cref="SphynxPacket"/>s.
        /// </summary>
        protected IPacketTransporter PacketTransporter { get; }

        /// <summary>
        /// The packet router which is used to route incoming packets to handlers.
        /// </summary>
        protected IPacketRouter PacketRouter { get; }

        private readonly NetworkStream _stream;

        private CancellationTokenSource _clientCts = new();
        private readonly AsyncLocal<bool> _isInsideClientTask = new();
        private readonly SemaphoreSlim _startSemaphore = new(1, 1);
        private readonly SemaphoreSlim _disposeSemaphore = new(0, 1);

        private volatile bool _disposed;

        // 0 = not stopped; 1 = stopping/stopped
        private int _stopped;

        public SphynxTcpClient(Socket socket, SphynxTcpServerProfile profile)
        {
            Socket = socket;
            EndPoint = (IPEndPoint)Socket.RemoteEndPoint!;
            _stream = new NetworkStream(Socket, false);

            ClientId = Guid.NewGuid();

            PacketTransporter = profile.PacketTransporter;
            PacketRouter = profile.PacketRouter;
            Logger = profile.LoggerFactory.CreateLogger(GetType());
        }

        public SphynxTcpClient(Socket socket, IPacketTransporter packetTransporter, IPacketRouter router, ILogger logger)
            : this(socket, Guid.NewGuid(), packetTransporter, router, logger)
        {
        }

        public SphynxTcpClient(Socket socket,
            Guid clientId,
            IPacketTransporter packetTransporter,
            IPacketRouter router,
            ILogger logger)
        {
            Socket = socket;
            EndPoint = (IPEndPoint)Socket.RemoteEndPoint!;
            _stream = new NetworkStream(Socket, false);

            ClientId = clientId;

            PacketTransporter = packetTransporter;
            PacketRouter = router;
            Logger = logger;
        }

        public async ValueTask StartAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfStopped();

            await _startSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            Exception? runtimeException = null;

            try
            {
                // Propagate exceptions to concurrent callers
                if (_clientTask is not null)
                {
                    await _clientTask.ConfigureAwait(false);
                    return;
                }

                if (!_clientCts.IsCancellationRequested)
                {
                    Logger.LogDebug("Starting client run loop...");

                    runtimeException = await StartInternalAsync(cancellationToken).ConfigureAwait(false);

                    Logger.LogDebug("Stopping client run loop...");
                }
            }
            finally
            {
                _startSemaphore.Release();
            }

            await StopAsync(runtimeException).ConfigureAwait(false);
        }

        private async ValueTask<Exception?> StartInternalAsync(CancellationToken cancellationToken)
        {
            Debug.Assert(_startSemaphore.CurrentCount == 0);
            Debug.Assert(_clientTask == null);

            if (cancellationToken.CanBeCanceled)
                _clientCts = CancellationTokenSource.CreateLinkedTokenSource(_clientCts.Token, cancellationToken);

            try
            {
                if (!_clientCts.IsCancellationRequested)
                {
                    try
                    {
                        _isInsideClientTask.Value = true;
                        await (_clientTask = RunAsync(_clientCts.Token)).ConfigureAwait(false);
                    }
                    finally
                    {
                        _isInsideClientTask.Value = false;
                    }
                }
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == _clientCts.Token)
            {
                // Client stopped
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An unhandled exception occured during client execution");
                return ex;
            }

            return null;
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var packet = await ReadPacketAsync(cancellationToken).ConfigureAwait(false);

                if (packet is null)
                    continue;

                await HandlePacketAsync(packet, cancellationToken).ConfigureAwait(false);
            }
        }

        private async ValueTask<SphynxPacket?> ReadPacketAsync(CancellationToken cancellationToken)
        {
            // TODO: PoolingAsyncValueTaskMethodBuilder

            try
            {
                // TODO: Handle transient errors
                return await PacketTransporter.ReceiveAsync(_stream, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex.IsCancellationException())
            {
                Logger.LogWarning("Aborted packet read (cancelled)");
            }
            catch (Exception ex) when (ex.IsConnectionResetException())
            {
                Logger.LogTrace(ex, "Connection aborted while reading packet");

                await StopAsync(ex).ConfigureAwait(false);
            }
            // This might happen if the client forcibly closes the connection
            catch (Exception ex) when (ex is EndOfStreamException or ObjectDisposedException or ArgumentException)
            {
                await StopAsync(ex).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected exception occured while reading packet");

                await StopAsync(ex).ConfigureAwait(false);
            }

            return null;
        }

        private async ValueTask HandlePacketAsync(SphynxPacket packet, CancellationToken cancellationToken)
        {
            try
            {
                await PacketRouter.ExecuteAsync(this, packet, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                if (Logger.IsEnabled(LogLevel.Warning))
                    Logger.LogWarning("Packet handling cancelled for packet {PacketType}", packet.PacketType);
            }
            catch (Exception ex)
            {
                if (Logger.IsEnabled(LogLevel.Error))
                    Logger.LogError(ex, "Unhandled exception in packet pipeline for packet {PacketType}", packet.PacketType);
            }
        }

        /// <inheritdoc/>
        public async ValueTask SendAsync(SphynxPacket packet, CancellationToken cancellationToken = default)
        {
            ThrowIfStopped();

            try
            {
                // TODO: Handle transient errors
                // TODO: Verify send atomicity (maybe queue packet sends)

                // Don't think creating a CTS for each send operation would be very wise. Simply passing
                // the provided cancellationToken should be fine; in the worse case, the PacketTransporter
                // throws when trying to write to the underlying stream if this client has already started
                // its disposal process.
                await PacketTransporter.SendAsync(_stream, packet, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex.IsCancellationException())
            {
                if (Logger.IsEnabled(LogLevel.Warning))
                    Logger.LogWarning("Aborted packet sending for packet {PacketType} (cancelled)", packet.PacketType);
            }
            // This might happen if the client forcibly closes the connection
            catch (Exception ex) when (ex is EndOfStreamException or ObjectDisposedException or ArgumentException)
            {
                if (Logger.IsEnabled(LogLevel.Warning))
                    Logger.LogWarning("Abandoning packet send request for packet {PacketType}", packet.PacketType);

                await StopAsync(ex).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logger.IsEnabled(LogLevel.Error))
                    Logger.LogError(ex, "Unexpected exception occured while sending packet {PacketType}", packet.PacketType);

                await StopAsync(ex).ConfigureAwait(false);
            }
        }

        private void ThrowIfStopped()
        {
            ThrowIfDisposed();

            if (_clientCts.IsCancellationRequested)
                throw new OperationCanceledException("The operation was canceled.");
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().FullName);
        }

        /// <summary>
        /// Called once the underlying socket has been disconnected.
        /// </summary>
        /// <param name="disconnectException">The disconnection exception, or null if it was a graceful disconnection.</param>
        protected virtual ValueTask OnDisconnectAsync(Exception? disconnectException)
        {
            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// Signals a wish to disconnect the client from the server, with the given exception.
        /// </summary>
        /// <param name="disconnectException">The disconnection exception.</param>
        /// <param name="waitForFinish">Whether to wait for the client to finish execution.</param>
        /// <returns>A task representing the stop operation. If <paramref name="waitForFinish"/> is true, this task will not
        /// complete until the client has been disconnected; else, it will return after sending a stop signal.</returns>
        /// <remarks>Client resources are not freed until <see cref="DisposeAsync()"/> is called.</remarks>
        public ValueTask StopAsync(Exception? disconnectException = null, bool waitForFinish = true)
        {
            // We allow the client to be stopped even when disposed. Just makes our lives easier.
            if (_disposed)
                return ValueTask.CompletedTask;

            // Try and reserve ourselves
            if (Interlocked.CompareExchange(ref _stopped, 1, 0) != 0)
            {
                if (!waitForFinish || _isInsideClientTask.Value)
                    return ValueTask.CompletedTask;

                return WaitAsync();
            }

            return DisconnectAsync(disconnectException, waitForFinish);
        }

        private ValueTask DisconnectAsync(Exception? disconnectException, bool waitForFinish)
        {
            Debug.Assert(Volatile.Read(ref _stopped) != 0);

            // Signal for stop
            if (!_clientCts.IsCancellationRequested)
            {
                try
                {
                    _clientCts.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // Since we are not acquiring the semaphore before cancelling, it's technically
                    // possible for a concurrent disposal to sneak in after the previous
                    // cancellation check. This can technically be guarded against by yet another
                    // semaphore, but that would potentially make this unlikely path non-synchronous,
                    // which might confuse the caller when waitForFinish == false.
                }
            }

            if (!waitForFinish || _isInsideClientTask.Value)
            {
                QueueDisconnect(disconnectException);
                return ValueTask.CompletedTask;
            }

            return DoDisconnect(disconnectException);

            async ValueTask DoDisconnect(Exception? exception)
            {
                await WaitAsync().ConfigureAwait(false);
                await PerformDisconnectAsync(exception).ConfigureAwait(false);
            }
        }

        private void QueueDisconnect(Exception? disconnectException)
        {
            var state = new DisconnectState
            {
                Client = this,
                DisconnectException = disconnectException
            };

            ThreadPool.QueueUserWorkItem(static async void (s) =>
            {
                await s.Client.WaitAsync().ConfigureAwait(false);
                await s.Client.PerformDisconnectAsync(s.DisconnectException).ConfigureAwait(false);
            }, state, false);
        }

        private readonly struct DisconnectState
        {
            public SphynxTcpClient Client { get; init; }
            public Exception? DisconnectException { get; init; }
        }

        private async ValueTask PerformDisconnectAsync(Exception? disconnectException = null)
        {
            Debug.Assert(_clientTask?.IsCompleted ?? true, "Run loop should complete before disconnecting client");
            // We should implicitly have hold of this semaphore to ensure there are no race conditions during disconnection.
            Debug.Assert(_disposeSemaphore.CurrentCount == 0);

            try
            {
                await Socket.DisconnectAsync(true).ConfigureAwait(false);
                Logger.LogInformation("Client disconnected");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An exception occured while disconnecting the client");
            }

            try
            {
                await OnDisconnectAsync(disconnectException).ConfigureAwait(false);
                _ = OnDisconnect?.Invoke(this, disconnectException);
            }
            catch
            {
                // Just ignore for now
            }

            _disposeSemaphore.Release();
        }

        /// <summary>
        /// Waits for the client to finish execution.
        /// </summary>
        private async ValueTask WaitAsync()
        {
            if (_disposed)
                return;

            if (_clientTask?.IsCompleted ?? false)
                return;

            await _startSemaphore.WaitAsync().ConfigureAwait(false);
            _startSemaphore.Release();
        }

        /// <summary>
        /// Asynchronously disposes of all resources held by this <see cref="SphynxTcpClient"/>.
        /// </summary>
        public ValueTask DisposeAsync() => DisposeAsync(true);

        /// <summary>
        /// Asynchronously disposes of all resources held by this <see cref="SphynxTcpClient"/>.
        /// </summary>
        /// <param name="disposeSocket">Whether to dispose of the socket used by the client.</param>
        public virtual async ValueTask DisposeAsync(bool disposeSocket)
        {
            if (_disposed)
                return;

            await StopAsync(waitForFinish: true).ConfigureAwait(false);
            await DisposeClientAsync(disposeSocket).ConfigureAwait(false);
        }

        private async ValueTask DisposeClientAsync(bool disposeSocket)
        {
            Debug.Assert(Volatile.Read(ref _stopped) != 0, "Client should have been stopped before disposing");

            await _disposeSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                if (_disposed)
                    return;

                OnDisconnect = null;

                await _stream.DisposeAsync().ConfigureAwait(false);
                _clientCts.Dispose();

                if (disposeSocket)
                    Socket.Dispose();
            }
            finally
            {
                _disposed = true;
                _disposeSemaphore.Release();
            }
        }
    }
}
