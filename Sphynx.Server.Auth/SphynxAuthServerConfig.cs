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
            RefreshCollectionName = "refresh-token-collection",
            JwtIssuer = "Sphynx",
            JwtAudience = "Sphynx",
            JwtSecret = HS256Algorithm.GenerateRandomRecommendedKey(),
            AccessTokenExpiryTime = TimeSpan.FromMinutes(15),
            RefreshTokenExpiryTime = TimeSpan.FromDays(14),
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

        [PropertiesMember("db.refresh.collection")]
        public string RefreshCollectionName { get; set; } = null!;

        [PropertiesMember("jwt.issuer")]
        public string JwtIssuer { get; set; } = null!;

        [PropertiesMember("jwt.audience")]
        public string JwtAudience { get; set; } = null!;

        [PropertiesMember("jwt.secret")]
        public byte[] JwtSecret { get; set; } = null!;

        [PropertiesMember("jwt.access.expiry")]
        public TimeSpan AccessTokenExpiryTime { get; set; }

        [PropertiesMember("jwt.refresh.expiry")]
        public TimeSpan RefreshTokenExpiryTime { get; set; }

        [PropertiesMember("rate_limiter.permits")]
        public int RateLimiterPermits { get; set; }

        [PropertiesMember("rate_limiter.period")]
        public TimeSpan RateLimiterPeriod { get; set; }

        public static SphynxAuthServerConfig LoadFromEnvironment()
        {
            var config = Default;

            string? portEnv = Environment.GetEnvironmentVariable("SPHYNX_AUTH_PORT");
            string? connStringEnv = Environment.GetEnvironmentVariable("SPHYNX_AUTH_DB_CONN");
            string? dbNameEnv = Environment.GetEnvironmentVariable("SPHYNX_AUTH_DB");
            string? userColEnv = Environment.GetEnvironmentVariable("SPHYNX_AUTH_USER_COL");
            string? refreshColEnv = Environment.GetEnvironmentVariable("SPHYNX_AUTH_REFRESH_COL");
            string? jwtIssuerEnv = Environment.GetEnvironmentVariable("SPHYNX_AUTH_ISSUER");
            string? jwtAudienceEnv = Environment.GetEnvironmentVariable("SPHYNX_AUTH_AUDIENCE");
            string? jwtSecretEnv = Environment.GetEnvironmentVariable("SPHYNX_AUTH_SECRET");
            string? accessExpiryEnv = Environment.GetEnvironmentVariable("SPHYNX_AUTH_ACCESS_EXP");
            string? refreshExpiryEnv = Environment.GetEnvironmentVariable("SPHYNX_AUTH_REFRESH_EXP");
            string? rlPermitsEnv = Environment.GetEnvironmentVariable("SPHYNX_AUTH_RL_PERMITS");
            string? rlPeriodEnv = Environment.GetEnvironmentVariable("SPHYNX_AUTH_RL_PERIOD");

            if (short.TryParse(portEnv, out short port)) config.Port = port;
            if (!string.IsNullOrWhiteSpace(connStringEnv)) config.DbConnectionString = connStringEnv.Trim();
            if (!string.IsNullOrWhiteSpace(dbNameEnv)) config.DbName = dbNameEnv.Trim();
            if (!string.IsNullOrWhiteSpace(userColEnv)) config.UserCollectionName = userColEnv.Trim();
            if (!string.IsNullOrWhiteSpace(refreshColEnv)) config.RefreshCollectionName = refreshColEnv.Trim();
            if (!string.IsNullOrWhiteSpace(jwtIssuerEnv)) config.JwtIssuer = jwtIssuerEnv.Trim();
            if (!string.IsNullOrWhiteSpace(jwtAudienceEnv)) config.JwtAudience = jwtAudienceEnv.Trim();

            if (!string.IsNullOrWhiteSpace(jwtSecretEnv))
            {
                try
                {
                    config.JwtSecret = Convert.FromBase64String(jwtSecretEnv.Trim());
                }
                catch
                {
                    // lazy
                }
            }

            if (long.TryParse(accessExpiryEnv, out long accessExpiryMs) && IsValidTimeSpan(accessExpiryMs))
                config.AccessTokenExpiryTime = TimeSpan.FromMilliseconds(accessExpiryMs);

            if (long.TryParse(refreshExpiryEnv, out long refreshExpiryMs) && IsValidTimeSpan(refreshExpiryMs))
                config.RefreshTokenExpiryTime = TimeSpan.FromMilliseconds(refreshExpiryMs);

            if (int.TryParse(rlPermitsEnv, out int rlPermits) && rlPermits >= 0) config.RateLimiterPermits = rlPermits;
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
            if (other.RefreshCollectionName != default) RefreshCollectionName = other.RefreshCollectionName;
            if (other.JwtIssuer != default) JwtIssuer = other.JwtIssuer;
            if (other.JwtAudience != default) JwtAudience = other.JwtAudience;
            if (other.JwtSecret != default) JwtSecret = other.JwtSecret;
            if (other.AccessTokenExpiryTime != default) AccessTokenExpiryTime = other.AccessTokenExpiryTime;
            if (other.RefreshTokenExpiryTime != default) RefreshTokenExpiryTime = other.RefreshTokenExpiryTime;
        }
    }
}
