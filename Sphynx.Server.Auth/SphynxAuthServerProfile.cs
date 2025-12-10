// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Globalization;
using System.Net;
using Microsoft.Extensions.Logging;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using PropertiesDotNet.Core;
using PropertiesDotNet.Serialization;
using PropertiesDotNet.Serialization.Converters;
using PropertiesDotNet.Serialization.ObjectProviders;
using PropertiesDotNet.Serialization.PropertiesTree;
using Sphynx.Core;
using Sphynx.Network.Packet.Request;
using Sphynx.Network.Serialization;
using Sphynx.Network.Transport;
using Sphynx.Server.Auth.Handlers;
using Sphynx.Server.Auth.Middleware;
using Sphynx.Server.Auth.Persistence;
using Sphynx.Server.Auth.Services;
using Sphynx.Server;
using Sphynx.Server.Infrastructure.Middleware;
using Sphynx.Server.Infrastructure.RateLimiting;
using Sphynx.Server.Infrastructure.Routing;
using Sphynx.Server.Infrastructure.Services;
using Sphynx.Server.Persistence;
using Sphynx.Server.Persistence.Auth;
using StackExchange.Redis;

namespace Sphynx.Server.Auth
{
    public sealed class SphynxAuthServerProfile : SphynxTcpServerProfile
    {
        public override ILogger Logger => _logger ??= LoggerFactory.CreateLogger(typeof(SphynxAuthServer));
        private ILogger _logger;

        public IAuthService AuthService { get; private set; }
        public Func<IRateLimiter> RateLimiterFactory { get; private set; }

        private RateLimitingMiddleware<IPAddress> _rateLimitingMiddleware;

        private IMongoClient _mongoClient;
        private IConnectionMultiplexer _redisClient;

        public SphynxAuthServerProfile(bool isDevelopment = true) : base(configure: false)
        {
            ConfigureBase(isDevelopment);
            ConfigureServices(isDevelopment);
            ConfigureMiddleware(isDevelopment);
            ConfigureRoutes(isDevelopment);
        }

        private void ConfigureBase(bool isDevelopment)
        {
            Backlog = isDevelopment ? 32 : 256;

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

            PacketTransporter = new PacketTransporter(new JsonPacketSerializer());
        }

        private void ConfigureServices(bool isDevelopment)
        {
            if (Logger.IsEnabled(LogLevel.Debug))
                Logger.LogDebug("Configuring services for {Configuration} configuration...", isDevelopment ? "DEBUG" : "RELEASE");

            if (isDevelopment)
                ConfigureDevServices();
            else
                ConfigureReleaseServices();
        }

        private void ConfigureReleaseServices()
        {
            BsonSerializer.RegisterSerializer(new SnowflakeIdSerializer());

            var config = LoadServerConfig();

            ConfigureReleaseServices(config);
        }

        private SphynxAuthServerConfig LoadServerConfig()
        {
            var config = SphynxAuthServerConfig.LoadFromEnvironment();

            var serializer = new PropertiesSerializer
            {
                TreeComposer = new FlatTreeComposer(),
                ObjectProvider = new ExpressionObjectProvider(),
            };

            serializer.PrimitiveConverters.AddFirst(new CustomTimeSpanConverter());
            serializer.PrimitiveConverters.AddFirst(new ByteArrayConverter());

            SphynxAuthServerConfig fileConfig = null;

            if (File.Exists(SphynxAuthServerConfig.FILENAME))
            {
                try
                {
                    using var reader = PropertiesReader.FromFile(SphynxAuthServerConfig.FILENAME);
                    fileConfig = serializer.DeserializeObject<SphynxAuthServerConfig>(reader);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Unable to read environment file, using defaults...");
                }
            }
            else
            {
                Logger.LogInformation("Unable to locate environment file, using defaults...");
            }

            if (fileConfig is not null)
                config.MergeFrom(fileConfig);

            if (Logger.IsEnabled(LogLevel.Information))
                Logger.LogInformation("Successfully loaded environment configuration.");

            if (Logger.IsEnabled(LogLevel.Trace))
            {
                Logger.LogTrace("Current environment settings:\r\n" +
                                "-----------------------------\r\n" +
                                "{EnvironmentSettings}", serializer.SerializeObject(config));
            }

            return config;
        }

        private class ByteArrayConverter : IPropertiesPrimitiveConverter
        {
            public bool Accepts(Type type) => type == typeof(byte[]);

            public object Deserialize(PropertiesSerializer serializer, Type type, string input)
            {
                return Convert.FromBase64String(input!);
            }

            public string Serialize(PropertiesSerializer serializer, Type type, object input)
            {
                return Convert.ToBase64String((byte[])input!);
            }
        }

        private class CustomTimeSpanConverter : IPropertiesPrimitiveConverter
        {
            private readonly TimeSpanConverter _converter = new TimeSpanConverter();

            public bool Accepts(Type type) => type == typeof(TimeSpan);

            public object Deserialize(PropertiesSerializer serializer, Type type, string input)
            {
                string? timeSpanMs = input?.Trim().ToLowerInvariant();

                if ((timeSpanMs?.EndsWith("ms") ?? false) && double.TryParse(timeSpanMs.AsSpan()[..^2], out double timeSpan))
                    return TimeSpan.FromMilliseconds(timeSpan);

                return _converter.Deserialize(serializer, type, input);
            }

            public string Serialize(PropertiesSerializer serializer, Type type, object input)
            {
                if (input is null)
                    return "0";

                return ((TimeSpan)input!).TotalMilliseconds.ToString(CultureInfo.InvariantCulture) + "ms";
            }
        }

        private void ConfigureReleaseServices(SphynxAuthServerConfig config)
        {
            EndPoint.Port = config.Port;

            _mongoClient = new MongoClient(config.DbConnectionString);
            _redisClient = ConnectionMultiplexer.Connect(config.RedisConnectionString);

            var db = _mongoClient.GetDatabase(config.DbName);
            var userRepository = new MongoAuthUserRepository(db, config.UserCollectionName);

            var activeSessionRepository = new RedisSessionRepository(_redisClient);
            var sessionRepository = new MongoSessionRepository(db, config.SessionCollectionName);
            var sessionService = new SessionService(activeSessionRepository, sessionRepository, LoggerFactory.CreateLogger<SessionService>());

            // TODO: Make configurable
            var passwordHasher = new Pbkdf2PasswordHasher();

            AuthService = new AuthService(passwordHasher, userRepository, sessionService, LoggerFactory.CreateLogger<AuthService>());

            RateLimiterFactory = () => new TokenBucketRateLimiter(config.RateLimiterPermits, config.RateLimiterPermits, config.RateLimiterPeriod);
        }

        private void ConfigureDevServices()
        {
            var userRepository = new NullUserRepository();
            var passwordHasher = new Pbkdf2PasswordHasher();
            var sessionService = new SessionService(null!, null!, LoggerFactory.CreateLogger<SessionService>());

            AuthService = new AuthService(passwordHasher, userRepository, sessionService, LoggerFactory.CreateLogger<AuthService>());

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
                        var errorInfo = new SphynxErrorInfo(SphynxErrorCode.ENHANCE_YOUR_CALM, "Too many requests.");
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
                .UseHandler(new LogoutHandler(AuthService, LoggerFactory.CreateLogger<LogoutHandler>()));

            PacketRouter = router;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _rateLimitingMiddleware?.Dispose();
                _mongoClient?.Dispose();
                _redisClient?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
