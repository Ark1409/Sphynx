// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Sphynx.Network.PacketV2;
using Sphynx.Network.Transport;
using Sphynx.ServerV2.Extensions;
using Sphynx.ServerV2.Handlers;

namespace Sphynx.ServerV2.Client
{
    /// <summary>
    /// Represents a client socket connection to the server.
    /// </summary>
    public class SphynxTcpClient : ISphynxClient, IDisposable, IAsyncDisposable
    {
        /// <inheritdoc/>
        public Guid ClientId { get; }

        /// <inheritdoc/>
        public IPEndPoint EndPoint { get; }

        /// <summary>
        /// Indicates whether this client instance has initiated its read loop.
        /// </summary>
        public bool Started => Volatile.Read(ref _started) != 0;

        // 0 = not started, 1 = started
        private int _started;

        /// <summary>
        /// Event that is fired when this client disconnects from the server.
        /// </summary>
        public event Action<SphynxTcpClient, Exception?>? OnDisconnect;

        /// <summary>
        /// Cancellation token source for the client's read loop.
        /// </summary>
        protected CancellationTokenSource ClientCts { get; private set; } = new();

        /// <summary>
        /// The logger created for this client.
        /// </summary>
        protected internal ILogger Logger { get; }

        /// <summary>
        /// The packet transporter which is used to send and receive <see cref="SphynxPacket"/>s.
        /// </summary>
        protected IPacketTransporter PacketTransporter { get; }

        /// <summary>
        /// The packet handler which is used to handle incoming packets.
        /// </summary>
        protected IPacketHandler<SphynxPacket> PacketHandler { get; }

        /// <summary>
        /// The read task for the client's read loop.
        /// </summary>
        protected Task? ClientTask => _clientTask;

        /// <summary>
        /// A reference to the accepted client socket.
        /// </summary>
        internal Socket Socket { get; }

        private readonly NetworkStream _stream;

        private volatile Task? _clientTask;
        private readonly AsyncLocal<bool> _isInsideClientTask = new();
        private readonly SemaphoreSlim _startStopSemaphore = new(1, 1);

        // 0 = not disposed, 1 = disposing/disposed
        private int _disposed;

        // 0 = not stopped, 1 = stopped
        private int _stopped;

        public SphynxTcpClient(Socket socket, SphynxTcpServerProfile serverProfile) : this(socket,
            serverProfile.PacketTransporter, serverProfile.PacketHandler, serverProfile.LoggerFactory.CreateLogger(typeof(SphynxTcpClient)))
        {
        }

        public SphynxTcpClient(Socket socket, IPacketTransporter packetTransporter, IPacketHandler<SphynxPacket> packetHandler, ILogger logger)
            : this(socket, Guid.NewGuid(), packetTransporter, packetHandler, logger)
        {
        }

        public SphynxTcpClient(Socket socket, Guid clientId, IPacketTransporter packetTransporter, IPacketHandler<SphynxPacket> packetHandler,
            ILogger logger)
        {
            Socket = socket;
            EndPoint = (IPEndPoint)Socket.RemoteEndPoint!;
            _stream = new NetworkStream(Socket, false);

            ClientId = clientId;

            PacketTransporter = packetTransporter;
            PacketHandler = packetHandler;
            Logger = logger;
        }

        public async ValueTask StartAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfStopped();

            if (Interlocked.CompareExchange(ref _started, 1, 0) != 0)
                return;

            ClientCts = CancellationTokenSource.CreateLinkedTokenSource(ClientCts.Token, cancellationToken);

            if (!ClientCts.IsCancellationRequested)
            {
                await _startStopSemaphore.WaitAsync(ClientCts.Token).ConfigureAwait(false);

                try
                {
                    _isInsideClientTask.Value = true;
                    await (_clientTask = RunAsync(ClientCts.Token)).ConfigureAwait(false);
                }
                // All exceptions should be handled already
                finally
                {
                    _isInsideClientTask.Value = false;
                    _startStopSemaphore.Release();
                }
            }

            await DisposeAsync().ConfigureAwait(false);
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
            try
            {
                // TODO: Handle transient errors
                return await PacketTransporter.ReceiveAsync(_stream, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex.IsCancellationException())
            {
                Logger.LogWarning("Aborted packet read (cancelled)");
            }
            // This might happen if the client forcibly closes the connection
            catch (Exception ex) when (ex is EndOfStreamException or ObjectDisposedException or ArgumentException)
            {
                await StopAsync(ex).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected exception occured while reading packets");

                await StopAsync(ex).ConfigureAwait(false);
            }

            return null;
        }

        private async ValueTask HandlePacketAsync(SphynxPacket packet, CancellationToken cancellationToken)
        {
            try
            {
                await PacketHandler.HandlePacketAsync(this, packet, cancellationToken).ConfigureAwait(false);
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
            ClientCts.Token.ThrowIfCancellationRequested();
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) != 0)
                throw new ObjectDisposedException(GetType().FullName);
        }

        /// <summary>
        /// Signals a wish to disconnect the client from the server, with the given exception.
        /// </summary>
        /// <param name="disconnectException">The disconnection exception.</param>
        /// <remarks>Resources are not freed until <see cref="Dispose(bool)"/> or <see cref="DisposeAsync"/>
        /// is called.</remarks>
        protected async ValueTask StopAsync(Exception? disconnectException)
        {
            // We allow clients to be stopped even when disposed. Just makes our lives easier.
            if (Volatile.Read(ref _disposed) != 0)
                return;

            if (Interlocked.CompareExchange(ref _stopped, 1, 0) != 0)
                return;

            await _startStopSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                ClientCts.Cancel();
                OnDisconnect?.Invoke(this, disconnectException);
            }
            // If we're disconnecting, we should expect a disposal soon anyway
            catch
            {
                _startStopSemaphore.Release();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            // If we are stopping from the run loop, simply return. We don't need to mark our disposal, as
            // dispose is going to be called anyway.
            if (_isInsideClientTask.Value)
            {
                if (!ClientCts.IsCancellationRequested)
                    ClientCts.Cancel();

                return;
            }

            if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
                return;

            if (disposing)
            {
                // If we are disconnecting the client from Dispose rather than DisconnectAsync (i.e. externally),
                // make sure to notify the disconnect subscribers, simply passing null as the disconnection error
                var stopTask = StopAsync(null);

                if (!stopTask.IsCompleted)
                    stopTask.AsTask().Wait();

                OnDisconnect = null;

                _stream.Dispose();

                Socket.Shutdown(SocketShutdown.Both);
                Socket.Disconnect(true);

                var clientTask = _clientTask;

                if (clientTask is not null && !clientTask.IsCompleted)
                    clientTask.Wait();

                ClientCts.Dispose();
                _startStopSemaphore.Dispose();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        public virtual ValueTask DisposeAsync()
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
