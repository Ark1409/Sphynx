using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using UserId = System.Guid;

namespace Sphynx.Client.Core
{
    public class SphynxServer
    {
        public const short PORT = 2000;

        public IPAddress ServerAddress { get; set; }

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

        public SphynxSessionUser? ConnectAs(string? username, string? password, out ErrorCode error)
        {
            if (string.IsNullOrEmpty(username = username?.Trim()))
            {
                error = ErrorCode.INVALID_USER;
                return null;
            }

            if (string.IsNullOrEmpty(password = password?.Trim()))
            {
                error = ErrorCode.INVALID_PASSWORD;
                return null;
            }

            // Connect
            Socket userSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

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
