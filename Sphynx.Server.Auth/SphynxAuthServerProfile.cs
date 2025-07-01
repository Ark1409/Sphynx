// Copyright (c) Ark -α- & Specyy.Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using Microsoft.Extensions.Logging;
using Sphynx.Network.PacketV2;
using Sphynx.Network.Serialization.Model;
using Sphynx.Network.Serialization.Packet;
using Sphynx.Network.Transport;
using Sphynx.Server.Auth.Handlers;
using Sphynx.Server.Auth.Middleware;
using Sphynx.Server.Auth.Persistence;
using Sphynx.Server.Auth.Services;
using Sphynx.ServerV2;
using Sphynx.ServerV2.Infrastructure.Routing;

namespace Sphynx.Server.Auth
{
    public class SphynxAuthServerProfile : SphynxTcpServerProfile
    {
        public IPasswordHasher PasswordHasher { get; set; }
        public IUserRepository UserRepository { get; set; }
        public IAuthService AuthService { get; set; }

        public SphynxAuthServerProfile(bool isDevelopment = true)
        {
            ConfigureBase(isDevelopment);
            ConfigureTransporter(isDevelopment);
            ConfigureServices(isDevelopment);
            ConfigureRoutes(isDevelopment);
        }

        private void ConfigureBase(bool isDevelopment)
        {
            Backlog = isDevelopment ? 16 : 256;

            // TODO: Figure out a workaround
            LoggerFactory.Dispose();

            LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
            {
                builder.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.TimestampFormat = "[MM-dd-yyyy HH:mm:ss] ";
                });
                builder.SetMinimumLevel(isDevelopment ? LogLevel.Trace : LogLevel.Information);
            });
        }

        private void ConfigureTransporter(bool isDevelopment)
        {
            var transporter = new PacketTransporter();

            transporter.AddSerializer(SphynxPacketType.LOGIN_REQ, new LoginRequestPacketSerializer())
                .AddSerializer(SphynxPacketType.LOGIN_RES, new LoginResponseSerializer(new SphynxSelfInfoSerializer()))
                .AddSerializer(SphynxPacketType.REGISTER_REQ, new RegisterRequestSerializer())
                .AddSerializer(SphynxPacketType.REGISTER_RES, new RegisterResponseSerializer(new SphynxSelfInfoSerializer()));

            PacketTransporter = transporter;
        }

        private void ConfigureServices(bool isDevelopment)
        {
            PasswordHasher = new Pbkdf2PasswordHasher();
            // TODO: Register mongo credentials
            UserRepository = isDevelopment ? new NullUserRepository() : new MongoUserRepository(null!, null!);
            AuthService = new AuthService(PasswordHasher, UserRepository, LoggerFactory.CreateLogger<AuthService>());
        }

        private void ConfigureRoutes(bool isDevelopment)
        {
            var router = PacketRouter as PacketRouter ?? new PacketRouter();
            PacketRouter = router;

            router.UseMiddleware(new AuthPacketMiddleware(LoggerFactory.CreateLogger<AuthPacketMiddleware>()))
                .UseHandler(new LoginHandler(AuthService, LoggerFactory.CreateLogger<LoginHandler>()))
                .UseHandler(new RegisterHandler(AuthService, LoggerFactory.CreateLogger<RegisterHandler>()));
        }
    }
}
