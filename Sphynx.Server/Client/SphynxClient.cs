using System.Net.Sockets;
using Sphynx.Packet;
using Sphynx.Packet.Request;
using Sphynx.Server.Utils;

// ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract

namespace Sphynx.Server.Client
{
    /// <summary>
    /// Delegate for event that is fired when a client socket is detected as disconnected.
    /// </summary>
    /// <param name="client">The client instance that disconnected</param>
    /// <param name="exception">The exception which caused the disconnection.</param>
    public delegate void ClientDisconnect(SphynxClient client, Exception exception);

    /// <summary>
    /// Represents a client socket connection to the server.
    /// </summary>
    public sealed class SphynxClient : IDisposable
    {
        /// <summary>
        /// Gets the ID of the user that this client has been authenticated with. Will be null
        /// if the client has not yet authenticated themselves.
        /// </summary>
        public Guid? UserId => SphynxClientManager.TryGetUserId(this, out var userId) ? userId : null;

        /// <summary>
        /// The server that this <see cref="SphynxClient"/> is connected to.
        /// </summary>
        public SphynxServer Server { get; private set; }

        /// <summary>
        /// Gets the underlying socket for this <see cref="SphynxClient"/>.
        /// </summary>
        public Socket Socket { get; }

        /// <summary>
        /// Gets the network stream for the underlying socket.
        /// </summary>
        public NetworkStream SocketStream { get; }

        /// <summary>
        /// Indicates whether or not this client is still connected to the server.
        /// </summary>
        public bool Connected { private set; get; }

        public bool Started { private set; get; }

        /// <summary>
        /// Event that gets called when this client disconnects from a <see cref="SphynxServer"/>.
        /// </summary>
        public event ClientDisconnect Disconnected;

        private int _disposed;
        private readonly IPacketHandler _packetHandler;

        /// <summary>
        /// Creates a new <see cref="SphynxClient"/> representing a single socket connection to a <see cref="SphynxServer"/>.
        /// </summary>
        /// <param name="clientSocket">The client socket.</param>
        /// <remarks>Could become obsolete in the future.</remarks>
        public SphynxClient(Socket clientSocket) : this(SphynxApp.Server!, clientSocket)
        {
        }

        /// <summary>
        /// Creates a new <see cref="SphynxClient"/> representing a single socket connection to a <see cref="SphynxServer"/>.
        /// </summary>
        /// <param name="server">The server that this socket is connected to.</param>
        /// <param name="clientSocket">The client socket.</param>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public SphynxClient(SphynxServer server, Socket clientSocket)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            Socket = clientSocket;
            // Immediately check whether this socket is already registered to another client instance to prevent unnecessary allocations if possible
            if (SphynxClientManager.TryGetClient(Socket, out _))
            {
                // Quickly exit and dispose this client instance
                Dispose(true);
                return;
            }

            SocketStream = new NetworkStream(clientSocket, false);
            Server = server;

            _disposed = default;
            Connected = true;
            Disconnected = OnDispose;

            _packetHandler = new ClientPacketHandler(this);

            // Register packet after all initialization - final check for pre-existing clients
            if (SphynxClientManager.AddAnonymousClient(this) != this)
            {
                Dispose(true);
            }
        }

        public async Task StartAsync()
        {
            if (Started) return;
            
            // TODO: Properly check for half-open sockets
            try
            {
                Started = true;
                while (true)
                {
                    var packet = await ReceivePacketAsync().ConfigureAwait(false);
                    if (packet is null) continue;
                    
                    // We want authentication to be the only packet evaluated "synchronously"
                    if (packet.PacketType == SphynxPacketType.LOGIN_REQ && SphynxClientManager.IsAnonymous(this))
                    {
                        await _packetHandler.HandlePacketAsync(packet).ConfigureAwait(false);
                    }
                    else
                    {
                        // Do we require that packets sent by the same user are processed and "executed" in order? If so then 
                        // we should await this task as the current setup causes them to run in parallel
                        _packetHandler.HandlePacketAsync(packet).SafeExecute();
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or SocketException)
            {
                Disconnected?.Invoke(this, ex);
            }
        }

        /// <summary>
        /// Reads a packet from the underlying <see cref="Socket"/>.
        /// </summary>
        /// <returns>The deserialized packet, or null if an instantiation error occurs.</returns>
        private async Task<SphynxPacket?> ReceivePacketAsync()
        {
            var header = await SphynxPacketHeader.ReceiveAsync(SocketStream).ConfigureAwait(false);

            if (header is null)
                return null;

            return await SphynxPacket.CreateAsync(header.Value, SocketStream).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends a packet to the underlying <see cref="Socket"/>.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        /// <returns>true if the packet could be sent; false otherwise.</returns>
        /// <remarks>If this client has not yet authenticated themselves with a user (i.e. <see cref="UserId"/> is null),
        /// the only valid packet that can be sent is one of type <see cref="SphynxPacketType.LOGIN_RES"/>.</remarks>
        public async Task<bool> SendPacketAsync(SphynxPacket packet)
        {
            if (SphynxClientManager.IsAnonymous(this) && packet.PacketType != SphynxPacketType.LOGIN_RES)
            {
                // Packets may only be sent to respond to a LoginRequest (i.e. LoginResponsePackets must be sent) if
                // the client has not yet authenticated themselves with a specific user
                return false;
            }

            try
            {
                // TODO: Test keep-alive message to ensure client is still connected
                // TODO: Do we require a lock?
                return await packet.TrySerializeAsync(SocketStream).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Disconnected?.Invoke(this, ex);
            }

            return false;
        }

        private void OnDispose(SphynxClient sender, Exception e) => Dispose();

        /// <inheritdoc/>
        public void Dispose() => Dispose(false);

        // raceDisposing: Some sort of race condition could have occured during initialization (in the case where there are multiple
        // servers within the same application instance) or we simply accidentally create two clients with the same socket,
        // and there is already an existing client holding the socket instance. This parameter will be true and will be called from the ctor in that
        // case but should be false when disposing normally.
        private void Dispose(bool raceDisposing)
        {
            if (Interlocked.Exchange(ref _disposed, 1) != default)
                return;

            Connected = false;
            Server = null!;

            // Allow existing client to keep the socket during initialization race condition
            if (!raceDisposing)
            {
                Socket.Dispose();
                SphynxClientManager.RemoveClient(this, false);
            }

            SocketStream?.Dispose();
            Disconnected = null!;
        }
    }
}