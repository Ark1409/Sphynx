using System.Net;
using System.Net.Sockets;
using Sphynx.Core;

using UserId = System.Guid;

namespace Sphynx.Client.Core
{
    /// <summary>
    /// Represents a connection to a <c>Sphynx</c> server.
    /// </summary>
    public class SphynxServer
    {
        /// <summary>
        /// Default port for <c>Sphynx</c> servers.
        /// </summary>
        public const short PORT = 2000;

        /// <summary>
        /// <see cref="IPAddress"/> of the specific <c>Sphynx</c> server used to issue connections.
        /// </summary>
        public IPAddress ServerAddress { get; set; }

        /// <summary>
        /// Constructs a new <see cref="SphynxServer"/>
        /// </summary>
        /// <param name="address">The address of the server to utilize for subsequent connections.</param>
        public SphynxServer(IPAddress address)
        {
            ServerAddress = address;
        }

        public enum ErrorCode
        {
            NONE = 0,
            OK = NONE,
            INVALID_USER = 1,
            INVALID_PASSWORD,
            INCORRECT_PASSWORD,
            ALREADY_ONLINE,
            ENHANCE_YOUR_CALM,
            UNABLE_CONNECT
        }

        /// <summary>
        /// Issues a connection to a <c>Sphynx</c> server located at address <see cref="ServerAddress"/>
        /// </summary>
        /// <param name="username">The username to use when connecting</param>
        /// <param name="password">The password of the user</param>
        /// <param name="error">The error code of the operation. <see cref="ErrorCode.OK"/> if the connection was successful.</param>
        /// <returns>A <see cref="SphynxSessionUser"/> representing a the connection session to the server.</returns>
        public SphynxSessionUser? ConnectAs(string username, string password, out ErrorCode error)
        {
            // Connect
            var userSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

            try
            {
                userSocket.Connect(new IPEndPoint(ServerAddress, PORT));
            }
            catch (SocketException)
            {
                error = ErrorCode.UNABLE_CONNECT;
                return null;
            }

            // Send login attempt to server


            // Create the user after having successfully logged in
            SphynxSessionUser user = new SphynxSessionUser(UserId.Empty, userSocket, "");

            error = ErrorCode.OK;
            return user;
        }

        public UserId GetId(string username)
        {
            return UserId.Empty;
        }

        public string GetUsername(UserId id)
        {
            return null;
        }

        public string GetUsername(SphynxSessionUser user) => GetUsername(user.Id);

        public void ChangeUsername(SphynxSessionUser user, string newUsername)
        {

        }

        public void ChangePassword(SphynxSessionUser user, string newPassword)
        {

        }

        public SphynxSessionUser.UserStatus GetStatus(UserId id)
        {
            return SphynxSessionUser.UserStatus.OFFLINE;
        }

        public SphynxSessionUser.UserStatus GetStatus(SphynxSessionUser user) => GetStatus(user.Id);

        public void ChangeStatus(SphynxSessionUser user, SphynxSessionUser.UserStatus Status)
        {

        }

        public void Dispose(SphynxSessionUser? user) => user?.Dispose();
    }
}
