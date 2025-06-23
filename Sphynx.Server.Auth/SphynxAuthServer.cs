// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Sphynx.Server.Auth.Handlers;
using Sphynx.Server.Auth.Persistence;
using Sphynx.Server.Auth.Services;
using Sphynx.ServerV2;
using Sphynx.ServerV2.Client;

namespace Sphynx.Server.Auth
{
    /// <summary>
    /// The authentication server for the <c>Sphynx</c> chat client.
    /// </summary>
    public class SphynxAuthServer : SphynxTcpServer
    {
        // TODO: Maybe auth server configures itself (sets up its own middleware. etc)
        public IPasswordHasher PasswordHasher { get; } = new Pbkdf2PasswordHasher();
        public IUserRepository UserRepository { get; } = new NullUserRepository();
        public IAuthService AuthService { get; }

        /// <summary>
        /// Creates a new <c>Sphynx</c> authentication server and associates it
        /// with the specified <paramref name="serverEndpoint"/>.
        /// </summary>
        /// <param name="serverEndpoint">The server endpoint to bind to.</param>
        public SphynxAuthServer(IPEndPoint serverEndpoint) : this(new SphynxTcpServerProfile { EndPoint = serverEndpoint })
        {
        }

        /// <summary>
        /// Creates a new <c>Sphynx</c> authentication server and associates it
        /// with the specified <paramref name="profile"/>.
        /// </summary>
        /// <param name="profile">The server profile.</param>
        public SphynxAuthServer(SphynxTcpServerProfile profile) : base(profile)
        {
            if (string.IsNullOrEmpty(Name))
                Name = $"{GetType().Name}@{profile}";

            AuthService = new AuthService(PasswordHasher, UserRepository, Profile.LoggerFactory.CreateLogger<AuthService>());
            Profile.PacketHandler = new AuthPacketHandler(AuthService, Profile.LoggerFactory);
        }

        protected override SphynxTcpClient CreateTcpClient(Socket clientSocket) => new SphynxClient(clientSocket, Profile);
    }
}
