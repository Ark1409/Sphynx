using System;
using System.Collections;
using System.Net;
using Sphynx.Client.API;
using Sphynx.Utils;

namespace Sphynx.Client
{
    public class SphynxServerPool : ISphynxServerPool
    {
        private readonly List<SphynxServerInfo> _authServers =
        [
            new SphynxSocketServerInfo { EndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2000) }
        ];

        private readonly List<SphynxServerInfo> _chatServers =
        [
            new SphynxSocketServerInfo { EndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2000) }
        ];

        IAsyncEnumerable<SphynxServerInfo> ISphynxServerPool.AuthServers => AuthServers;
        IAsyncEnumerable<SphynxServerInfo> ISphynxServerPool.ChatServers => ChatServers;

        public DualEnumerator<SphynxServerInfo> AuthServers => _authServers.ToAsyncEnumerable();
        public DualEnumerator<SphynxServerInfo> ChatServers => _chatServers.ToAsyncEnumerable();
    }
}
