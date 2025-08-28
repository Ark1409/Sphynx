// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Net.Sockets;
using Sphynx.Server;
using Sphynx.Server.Client;

namespace Sphynx.Server.Auth
{
    /// <summary>
    /// The authentication server for the <c>Sphynx</c> chat client.
    /// </summary>
    public class SphynxAuthServer : SphynxTcpServer
    {
        /// <inheritdoc/>
        public override SphynxAuthServerProfile Profile { get; }

        /// <summary>
        /// Creates a new <c>Sphynx</c> authentication server.
        /// </summary>
        /// <param name="isDevelopment">Whether this is a dev build.</param>
        public SphynxAuthServer(bool isDevelopment = true) : this(new SphynxAuthServerProfile(isDevelopment))
        {
        }

        /// <summary>
        /// Creates a new <c>Sphynx</c> authentication server.
        /// </summary>
        public SphynxAuthServer(SphynxAuthServerProfile profile) : base(profile)
        {
            Profile = profile;
            Name = GetType().Name;
        }

        protected override SphynxTcpClient CreateTcpClient(Socket clientSocket) => new SphynxClient(clientSocket, Profile);
    }
}
