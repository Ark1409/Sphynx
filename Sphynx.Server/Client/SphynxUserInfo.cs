using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Sphynx.Server.Client
{
    public sealed class SphynxUserInfo : IDisposable, IEquatable<SphynxUserInfo>
    {
        [BsonId]
        public Guid UserId { get; private set; }
        public Socket UserSocket { get; private set; }
        public string UserName { get; private set; }
        public SphynxUserStatus UserStatus { get; private set; }
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
