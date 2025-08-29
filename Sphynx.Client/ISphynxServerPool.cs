using System;
using Sphynx.Client.API;

namespace Sphynx.Client
{
    public interface ISphynxServerPool
    {
        public IAsyncEnumerable<SphynxServerInfo> AuthServers { get; }
        public IAsyncEnumerable<SphynxServerInfo> ChatServers { get; }
    }
}
