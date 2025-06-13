// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using Sphynx.Core;
using Sphynx.Network.PacketV2;
using Sphynx.Network.Transport;

namespace Sphynx.ServerV2.Client
{
    /// <summary>
    /// Represents a client socket connection to the server.
    /// </summary>
    public class SphynxClient : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// The unique ID for this client connection.
        /// </summary>
        public Guid ClientId { get; private set; }

        /// <summary>
        /// Gets the ID of the user that this client has been authenticated with.
        /// </summary>
        public SnowflakeId UserId { get; private set; }

        /// <summary>
        /// Indicates whether this client instance has initiated its read loop.
        /// </summary>
        public bool Started => Volatile.Read(ref _started) != 0;

        // 0 = not started, 1 = started
        private int _started;

        /// <summary>
        /// Event that gets fired when this client disconnects from the server.
        /// </summary>
        public event Action<SphynxClient, Exception>? OnDisconnect;

        private readonly Socket _socket;
        private readonly NetworkStream _stream;
        private readonly IPacketTransporter _packetTransporter;

        private volatile CancellationTokenSource? _cts;

        // 0 = not yet disposed, 1 = disposed started
        private int _disposed;

        public SphynxClient(Socket socket, Guid clientId, SnowflakeId userId, IPacketTransporter packetTransporter)
        {
            _socket = socket;
            _stream = new NetworkStream(_socket, false);

            ClientId = clientId;
            UserId = userId;

            _packetTransporter = packetTransporter;
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (Interlocked.CompareExchange(ref _started, 1, 0) != 0)
                return;

            // TODO: Properly check for half-open sockets
            try
            {
                _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                bool isDisposed = Volatile.Read(ref _disposed) != 0;

                if (isDisposed)
                    _cts.Cancel();

                while (true)
                {
                    var packet = await _packetTransporter.ReceiveAsync(_stream, _cts.Token).ConfigureAwait(false);

                    // TODO: Handle with packet handlers
                }
            }
            catch (Exception ex) when (ex is IOException or SocketException)
            {
                OnDisconnect?.Invoke(this, ex);
            }
        }

        /// <summary>
        /// Sends a packet to the underlying <see cref="Socket"/>.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        /// <returns>true if the packet could be sent; false otherwise.</returns>
        /// <remarks>If this client has not yet authenticated themselves with a user (i.e. <see cref="UserId"/> is null),
        /// the only valid packet that can be sent is one of type <see cref="SphynxPacketType.LOGIN_RES"/>.</remarks>
        public ValueTask SendPacketAsync(SphynxPacket packet, CancellationToken cancellationToken = default)
        {
            // TODO: Do we want to throw on the callers (ig its what we do with cancellation, right?)
            ThrowIfDisposed();

            // TODO: implement retry functionality when packet cannot be sent (up to 3 times ig)
            if (SphynxClientManager.IsAnonymous(this) && packet.PacketType != SphynxPacketType.LOGIN_RES)
            {
                // Packets may only be sent to respond to a LoginRequest (i.e. LoginResponsePackets must be sent) if
                // the client has not yet authenticated themselves with a specific user
                return false;
            }

            try
            {
                if (cancellationToken.IsCancellationRequested)
                    return ValueTask.FromCanceled(cancellationToken);

                if (_cts != null && _cts.IsCancellationRequested)
                    return ValueTask.FromCanceled(_cts.Token);

                // TODO: Test keep-alive message to ensure client is still connected
                // TODO: Do we require a lock?
                return _packetTransporter.SendAsync(_stream, packet, cancellationToken);
            }
            catch (Exception ex)
            {
                OnDisconnect?.Invoke(this, ex);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) != 0)
                throw new ObjectDisposedException(GetType().FullName);
        }

        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
                return;

            _cts?.Cancel();
            _cts?.Dispose();

            _stream.Dispose();
            // TODO: Allow the server to reuse the socket with accept async
            _socket.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
                return;

            _cts?.Cancel();
            _cts?.Dispose();

            await _stream.DisposeAsync();
            // TODO: Allow the server to reuse the socket with accept async
            await _socket.DisconnectAsync(false);
        }
    }
}
