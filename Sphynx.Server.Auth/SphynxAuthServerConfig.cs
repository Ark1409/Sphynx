// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using LitJWT.Algorithms;
using PropertiesDotNet.Serialization;
using Sphynx.ServerV2;

namespace Sphynx.Server.Auth
{
    public record SphynxAuthServerConfig
    {
        public const string FILENAME = ".env";

        public static SphynxAuthServerConfig Default => new()
        {
            Port = SphynxServerProfile.DEFAULT_PORT,
            DbConnectionString = "localhost",
            DbName = "db-name",
            UserCollectionName = "user-collection",
            SessionCollectionName = "refresh-token-collection",
            ActiveSessionExpiryTime = TimeSpan.FromMinutes(5),
            SessionExpiryTime = TimeSpan.FromDays(90),
            RateLimiterPermits = 60,
            RateLimiterPeriod = TimeSpan.FromMinutes(1),
        };

        [PropertiesMember("port")]
        public short Port { get; set; }

        [PropertiesMember("db.connection_string")]
        public string DbConnectionString { get; set; } = null!;

        [PropertiesMember("db.name")]
        public string DbName { get; set; } = null!;

        [PropertiesMember("db.user.collection")]
        public string UserCollectionName { get; set; } = null!;

        [PropertiesMember("session.collection")]
        public string SessionCollectionName { get; set; } = null!;

        [PropertiesMember("session.active_exp")]
        public TimeSpan ActiveSessionExpiryTime { get; set; }

        [PropertiesMember("session.exp")]
        public TimeSpan SessionExpiryTime { get; set; }

        [PropertiesMember("rate_limiter.permits")]
        public int RateLimiterPermits { get; set; }

        [PropertiesMember("rate_limiter.period")]
        public TimeSpan RateLimiterPeriod { get; set; }

        public static SphynxAuthServerConfig LoadFromEnvironment()
        {
            var config = Default;

            string? portEnv = Environment.GetEnvironmentVariable("SPHYNX_AUTH_PORT");
            string? connStringEnv = Environment.GetEnvironmentVariable("SPHYNX_DB_CONN");
            string? dbNameEnv = Environment.GetEnvironmentVariable("SPHYNX_DB");
            string? userColEnv = Environment.GetEnvironmentVariable("SPHYNX_USER_COL");
            string? sessionColEnv = Environment.GetEnvironmentVariable("SPHYNX_SESSION_COL");
            string? activeExpiryEnv = Environment.GetEnvironmentVariable("SPHYNX_ACTIVE_SESSION_EXP");
            string? sessionExpiryEnv = Environment.GetEnvironmentVariable("SPHYNX_SESSION_EXP");
            string? rlPermitsEnv = Environment.GetEnvironmentVariable("SPHYNX_AUTH_RL_PERMITS");
            string? rlPeriodEnv = Environment.GetEnvironmentVariable("SPHYNX_AUTH_RL_PERIOD");

            if (short.TryParse(portEnv, out short port)) config.Port = port;
            if (!string.IsNullOrWhiteSpace(connStringEnv)) config.DbConnectionString = connStringEnv.Trim();
            if (!string.IsNullOrWhiteSpace(dbNameEnv)) config.DbName = dbNameEnv.Trim();
            if (!string.IsNullOrWhiteSpace(userColEnv)) config.UserCollectionName = userColEnv.Trim();
            if (!string.IsNullOrWhiteSpace(sessionColEnv)) config.SessionCollectionName = sessionColEnv.Trim();

            if (long.TryParse(activeExpiryEnv, out long activeExpiryMs) && IsValidTimeSpan(activeExpiryMs))
                config.ActiveSessionExpiryTime = TimeSpan.FromMilliseconds(activeExpiryMs);

            if (long.TryParse(sessionExpiryEnv, out long sessionExpiryMs) && IsValidTimeSpan(sessionExpiryMs))
                config.SessionExpiryTime = TimeSpan.FromMilliseconds(sessionExpiryMs);

            if (int.TryParse(rlPermitsEnv, out int rlPermits) && rlPermits >= 0)
                config.RateLimiterPermits = rlPermits;

            if (long.TryParse(rlPeriodEnv, out long rlPeriodMs) && IsValidTimeSpan(rlPeriodMs))
                config.RateLimiterPeriod = TimeSpan.FromMilliseconds(rlPeriodMs);

            return config;

            static bool IsValidTimeSpan(long timeSpanMs)
            {
                return timeSpanMs >= TimeSpan.MinValue.TotalMilliseconds && timeSpanMs <= TimeSpan.MaxValue.TotalMilliseconds;
            }
        }

        public void MergeFrom(SphynxAuthServerConfig other)
        {
            if (other.Port != default) Port = other.Port;
            if (other.DbConnectionString != default) DbConnectionString = other.DbConnectionString;
            if (other.DbName != default) DbConnectionString = other.DbName;
            if (other.UserCollectionName != default) UserCollectionName = other.UserCollectionName;
            if (other.SessionCollectionName != default) SessionCollectionName = other.SessionCollectionName;
            if (other.ActiveSessionExpiryTime != default) ActiveSessionExpiryTime = other.ActiveSessionExpiryTime;
            if (other.SessionExpiryTime != default) SessionExpiryTime = other.SessionExpiryTime;
            if (other.RateLimiterPermits != default) RateLimiterPermits = other.RateLimiterPermits;
            if (other.RateLimiterPeriod != default) RateLimiterPeriod = other.RateLimiterPeriod;
        }
    }
}
