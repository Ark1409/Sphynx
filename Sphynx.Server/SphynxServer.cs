using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using Sphynx.Server.ChatRooms;
using Sphynx.Server.Client;

namespace Sphynx.Server
{
    /// <summary>
    /// Represents the server through which Sphynx clients communicate.
    /// </summary>
    public sealed class SphynxServer : IDisposable
    {
        /// <summary>
        /// Retrieves the encoding for information exchange between clients and servers.
        /// </summary>
        public static Encoding Encoding => Encoding.UTF8;

        /// <summary>
        /// Returns the default IP endpoint for the server.
        /// </summary>
        // TODO: New one each time?
        public static IPEndPoint DefaultEndPoint => new IPEndPoint(Dns.GetHostEntry(Dns.GetHostName()).AddressList[0], Port);

        /// <summary>
        /// Retrieves the port for socket information exchange between clients and servers.
        /// </summary>
        public static short Port => 2000;

        /// <summary>
        /// Maximum number of users in server backlog.
        /// </summary>
        public static int Backlog => 10;

        /// <summary>
        /// Returns buffer size for information exchange.
        /// </summary>
        public static int BufferSize => ushort.MaxValue;

        /// <summary>
        /// Returns the users connected to this server.
        /// </summary>
        public ICollection<SphynxUserInfo> Users => _users.Values;

        /// <summary>
        /// Return a list of all available chat rooms.
        /// </summary>
        public ICollection<ChatRoom> ChatRooms => _chatRooms.Values;

        /// <summary>
        /// Retrives the running state of the server.
        /// </summary>
        public bool Running { get; set; }

        /// <summary>
        /// Returns the endpoint associated with this socket.
        /// </summary>
        public IPEndPoint EndPoint { get; private set; }

        private readonly Dictionary<Guid, SphynxUserInfo> _users;
        private readonly Dictionary<Guid, ChatRoom> _chatRooms;

        private readonly Socket _serverSocket;
        private readonly Thread _serverThread;
        private object _disposeLock = new object();
        private bool _disposed;

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
            _serverSocket = new Socket(serverEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _serverSocket.SendBufferSize = _serverSocket.ReceiveBufferSize = BufferSize;
            _serverThread = new Thread(Run);

            _users = new Dictionary<Guid, SphynxUserInfo>();
            _chatRooms = new Dictionary<Guid, ChatRoom>();
        }

        /// <summary>
        /// Starts the server on a new thread.
        /// </summary>
        public void Start()
        {
            Running = true;
            _serverThread.Start();
        }

        private void Run()
        {
            _serverSocket.Bind(EndPoint);
            _serverSocket.Listen(Backlog);

            while (Running)
            {
                try
                {
                    var user = AddUser(_serverSocket.Accept());
                    Task.Factory.StartNew(() => HandleUser(user));
                }
                catch (SocketException)
                {
                    Console.WriteLine("Interrupted");
                }
            }
        }

        public void HandleUser(SphynxUserInfo user)
        {
            // Receive user messages and broadcast
            // Retrieve user status (online or away) and broadcast
            // Receive request for going into a specific room

        }

        private SphynxUserInfo AddUser(Socket clientSocket)
        {
            var user = new SphynxUserInfo(clientSocket);
            _users.Add(user.UserId, user);

            return user;
        }

        public void AddRoom(ChatRoom room)
        {
            room.MessageAdded += BroadcastRoomMessage;
        }

        public void BroadcastRoomMessage(ChatRoomMessage message)
        {
            var messageData = message.Serialize(Encoding);

            foreach (var user in message.Room.Users)
            {
                if (user.UserId != message.Sender.UserId)
                {
                    byte[] header = { (byte)SocketMessageType.MessageSend };
                    byte[] userId = user.UserId.ToByteArray();
                    byte[] userName = Encoding.GetBytes(user.UserName);

                    user.UserSocket.Send(header);
                    user.UserSocket.Send(userId);
                    user.UserSocket.Send(userName);
                    //user.UserSocket.Send(messageData.RoomId);
                    //user.UserSocket.Send(messageData.Timestamp);
                    //user.UserSocket.Send(messageData.Content);
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            lock (_disposeLock)
            {
                if (_disposed) 
                    return;

                _disposed = true;
                Running = false;

                foreach (var user in _users)
                {
                    user.Value.Dispose();
                }

                if (_serverThread.IsAlive)
                {
                    _serverSocket.Dispose();
                    _serverThread.Join();
                }
            }
        }
    }
}
