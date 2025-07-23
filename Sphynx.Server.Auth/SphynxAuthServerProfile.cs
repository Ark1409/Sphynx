// Copyright (c) Ark -α- & Specyy.Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Net;
using Microsoft.Extensions.Logging;
using Sphynx.Core;
using Sphynx.Network.PacketV2;
using Sphynx.Network.PacketV2.Request;
using Sphynx.Network.Serialization.Model;
using Sphynx.Network.Serialization.Packet;
using Sphynx.Network.Transport;
using Sphynx.Server.Auth.Handlers;
using Sphynx.Server.Auth.Middleware;
using Sphynx.Server.Auth.Persistence;
using Sphynx.Server.Auth.Services;
using Sphynx.ServerV2;
using Sphynx.ServerV2.Infrastructure.Middleware;
using Sphynx.ServerV2.Infrastructure.RateLimiting;
using Sphynx.ServerV2.Infrastructure.Routing;
using Sphynx.ServerV2.Infrastructure.Services;

namespace Sphynx.Server.Auth
{
    public sealed class SphynxAuthServerProfile : SphynxTcpServerProfile
    {
        public IPasswordHasher PasswordHasher { get; set; }
        public IAuthUserRepository UserRepository { get; set; }
        public IAuthService AuthService { get; set; }
        public Func<IRateLimiter> RateLimiterFactory { get; set; }

        private RateLimitingMiddleware<IPEndPoint> _rateLimitingMiddleware;
        private IJwtService _jwtService;

        public SphynxAuthServerProfile(bool isDevelopment = true) : base(configure: false)
        {
            ConfigureBase(isDevelopment);
            ConfigureTransporter(isDevelopment);
            ConfigureServices(isDevelopment);
            ConfigureMiddleware(isDevelopment);
            ConfigureRoutes(isDevelopment);
        }

        private void ConfigureBase(bool isDevelopment)
        {
            Backlog = isDevelopment ? 16 : 256;

            LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
            {
                builder.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.TimestampFormat = "[MM-dd-yyyy HH:mm:ss] ";
                });
                builder.SetMinimumLevel(isDevelopment ? LogLevel.Debug : LogLevel.Information);
            });

            EndPoint = DefaultEndPoint;
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
            UserRepository = isDevelopment ? new NullUserRepository() : new MongoAuthUserRepository(null!, null!);

            // TODO: Register mongo credentials
            _jwtService = new JwtService(null!);
            AuthService = new AuthService(PasswordHasher, UserRepository, _jwtService, LoggerFactory.CreateLogger<AuthService>());

            if (isDevelopment)
                RateLimiterFactory = () => new TokenBucketRateLimiter(int.MaxValue, int.MaxValue, TimeSpan.FromTicks(1));
            else
                RateLimiterFactory = () => new TokenBucketRateLimiter(1, 10, TimeSpan.FromMinutes(1));
        }

        private void ConfigureMiddleware(bool isDevelopment)
        {
            var router = PacketRouter as PacketRouter ?? new PacketRouter();

            // TODO: Maybe add redis
            if (!isDevelopment)
            {
                var rateLimitingMiddleware = new RateLimitingMiddleware<IPEndPoint>(RateLimiterFactory, client => client.EndPoint);

                rateLimitingMiddleware.OnRateLimited += async (info) =>
                {
                    if (info.Packet is SphynxRequest request)
                    {
                        var errorInfo = new SphynxErrorInfo(SphynxErrorCode.ENHANCE_YOUR_CALM,
                            $"Too many requests. Please wait {Math.Ceiling(info.WaitTime.TotalMinutes)} minute(s).");

                        await info.Client.SendAsync(request.CreateResponse(errorInfo)).ConfigureAwait(false);
                    }
                };

                router.UseMiddleware(_rateLimitingMiddleware = rateLimitingMiddleware);
            }

            router.UseMiddleware(new AuthPacketMiddleware(LoggerFactory.CreateLogger<AuthPacketMiddleware>()));

            PacketRouter = router;
        }

        private void ConfigureRoutes(bool isDevelopment)
        {
            var router = PacketRouter as PacketRouter ?? new PacketRouter();

            router.ThrowOnUnregistered = isDevelopment;

            router.UseHandler(new LoginHandler(AuthService, LoggerFactory.CreateLogger<LoginHandler>()))
                .UseHandler(new RegisterHandler(AuthService, LoggerFactory.CreateLogger<RegisterHandler>()))
                .UseHandler(new RefreshHandler(_jwtService, LoggerFactory.CreateLogger<RefreshHandler>()));

            PacketRouter = router;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _rateLimitingMiddleware?.Dispose();

            base.Dispose(disposing);
        }
    }
}
