using System.Net.Sockets;

namespace Sphynx.Server.User
{
    public sealed class SphynxUserInfo : IDisposable, IEquatable<SphynxUserInfo>
    {
        public Socket UserSocket { get; private set; }
        public string UserName { get; private set; }
        public SphynxUserStatus UserStatus { get; private set; }
        public Guid UserId { get; private set; }
        public string Email { get; private set; }

        private bool _disposed;

        public SphynxUserInfo(Socket socket)
        {
            ReadSocketInfo(UserSocket = socket);
        }

        private void ReadSocketInfo(Socket socket)
        {
            // socket.Receive();
        }

        public void Dispose()
        {
            // TODO: Custom lock
            lock (UserSocket.SafeHandle)
            {
                if (_disposed)
                    return;

                _disposed = true;
                UserSocket.Shutdown(SocketShutdown.Both);
                UserSocket.Dispose();
            }
        }

        /// <inheritdoc/>
        public bool Equals(SphynxUserInfo? other) => UserId == other?.UserId;

        /// <inheritdoc/>
        public override bool Equals(object? obj) => Equals(obj as SphynxUserInfo);

        /// <inheritdoc/>
        public override int GetHashCode() => UserId.GetHashCode();
    }
}
