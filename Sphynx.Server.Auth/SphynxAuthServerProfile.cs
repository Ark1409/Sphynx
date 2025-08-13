// Copyright (c) Ark -α- & Specyy.Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Net;
using Microsoft.Extensions.Logging;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Sphynx.Core;
using Sphynx.Network.PacketV2.Request;
using Sphynx.Network.Serialization;
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
using Sphynx.ServerV2.Persistence;
using Sphynx.ServerV2.Persistence.Auth;

namespace Sphynx.Server.Auth
{
    public sealed class SphynxAuthServerProfile : SphynxTcpServerProfile
    {
        public IAuthService AuthService { get; private set; }
        public Func<IRateLimiter> RateLimiterFactory { get; private set; }

        private RateLimitingMiddleware<IPAddress> _rateLimitingMiddleware;
        private IJwtService _jwtService;

        private IMongoClient _mongoClient;

        public SphynxAuthServerProfile(bool isDevelopment = true) : base(configure: false)
        {
            ConfigureBase(isDevelopment);
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

            var transporter = new PacketTransporter(new JsonPacketSerializer());

            // transporter.AddSerializer(SphynxPacketType.LOGIN_REQ, new LoginRequestPacketSerializer())
            //     .AddSerializer(SphynxPacketType.LOGIN_RES, new LoginResponseSerializer(new SphynxSelfInfoSerializer()))
            //     .AddSerializer(SphynxPacketType.REGISTER_REQ, new RegisterRequestSerializer())
            //     .AddSerializer(SphynxPacketType.REGISTER_RES, new RegisterResponseSerializer(new SphynxSelfInfoSerializer()));

            PacketTransporter = transporter;
        }

        private void ConfigureServices(bool isDevelopment)
        {
            if (isDevelopment)
                ConfigureDevServices();
            else
                ConfigureReleaseServices();
        }

        private void ConfigureReleaseServices()
        {
            BsonSerializer.RegisterSerializer(new SnowflakeIdSerializer());

            _mongoClient = new MongoClient((string)null!);
            var passwordHasher = new Pbkdf2PasswordHasher();

            var userDb = _mongoClient.GetDatabase(null!);
            var userRepository = new MongoAuthUserRepository(userDb, null!);

            var refreshDb = _mongoClient.GetDatabase(null!);
            var refreshRepository = new MongoRefreshTokenRepository(refreshDb, null!);

            var jwtOptions = JwtOptions.Default;
            jwtOptions.ExpiryTime = TimeSpan.FromMinutes(15);
            jwtOptions.RefreshTokenExpiryTime = TimeSpan.FromHours(1);

            _jwtService = new JwtService(refreshRepository, jwtOptions);
            AuthService = new AuthService(passwordHasher, userRepository, _jwtService, LoggerFactory.CreateLogger<AuthService>());

            RateLimiterFactory = () => new TokenBucketRateLimiter(1, 10, TimeSpan.FromMinutes(1));
        }

        private void ConfigureDevServices()
        {
            var passwordHasher = new Pbkdf2PasswordHasher();
            var userRepository = new NullUserRepository();
            var refreshRepository = new NullRefreshTokenRepository();

            _jwtService = new JwtService(refreshRepository);
            AuthService = new AuthService(passwordHasher, userRepository, _jwtService, LoggerFactory.CreateLogger<AuthService>());

            RateLimiterFactory = () => new TokenBucketRateLimiter(int.MaxValue, int.MaxValue, TimeSpan.FromTicks(1));
        }

        private void ConfigureMiddleware(bool isDevelopment)
        {
            var router = PacketRouter as PacketRouter ?? new PacketRouter();

            // TODO: Maybe add redis
            if (!isDevelopment)
            {
                _rateLimitingMiddleware = new RateLimitingMiddleware<IPAddress>(RateLimiterFactory, client => client.EndPoint.Address);

                _rateLimitingMiddleware.OnRateLimited += async (info) =>
                {
                    if (info.Packet is SphynxRequest request)
                    {
                        var errorInfo = new SphynxErrorInfo(SphynxErrorCode.ENHANCE_YOUR_CALM,
                            $"Too many requests. Please wait {Math.Ceiling(info.WaitTime.TotalMinutes)} minute(s).");

                        await info.Client.SendAsync(request.CreateResponse(errorInfo)).ConfigureAwait(false);
                    }
                };

                router.UseMiddleware(_rateLimitingMiddleware);
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
            {
                _rateLimitingMiddleware?.Dispose();
                _mongoClient?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
