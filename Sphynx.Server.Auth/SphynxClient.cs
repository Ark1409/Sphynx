// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Net.Sockets;
using Sphynx.ServerV2;
using Sphynx.ServerV2.Client;
using Sphynx.ServerV2.Infrastructure;

namespace Sphynx.Server.Auth
{
    /// <summary>
    /// Represents a client socket connection to the auth server.
    /// </summary>
    public class SphynxClient : SphynxTcpClient
    {
        private readonly IRateLimiter _rateLimiter = new TokenBucketRateLimiter(tokensPerSecond: 1);

        public SphynxClient(Socket socket, SphynxTcpServerProfile profile) : base(socket, profile)
        {
        }

        protected override ValueTask OnDisconnectAsync(Exception? disconnectException)
        {
            _rateLimiter.Dispose();
            return base.OnDisconnectAsync(disconnectException);
        }
    }
}
