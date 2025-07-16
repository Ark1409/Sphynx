// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.Xml;
using LitJWT;
using LitJWT.Algorithms;
using Sphynx.Core;
using Sphynx.ServerV2.Auth;

namespace Sphynx.ServerV2.Infrastructure.Services
{
    public class JwtService : IJwtService
    {
        private readonly JwtEncoder _jwtEncoder;
        private readonly JwtDecoder _jwtDecoder;
        private readonly JwtOptions _jwtOptions;

        public JwtService() : this(JwtOptions.Default)
        {
        }

        public JwtService(JwtOptions options)
        {
            if (options.Secret is null || options.Secret.Length == 0)
                options.Secret = HS256Algorithm.GenerateRandomRecommendedKey();

            var algorithm = new HS256Algorithm(options.Secret);

            _jwtEncoder = new JwtEncoder(algorithm);
            _jwtDecoder = new JwtDecoder(algorithm);
            _jwtOptions = options;
        }

        public Task<SphynxJwtInfo> GenerateTokenAsync(SnowflakeId userId, CancellationToken cancellationToken = default)
        {
            var payload = new SphynxJwtPayload
            {
                Issuer = _jwtOptions.Issuer,
                Audience = _jwtOptions.Audience,
                Subject = userId,
                IssuedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow + _jwtOptions.ExpiryTime,
            };

            string encodedPayload = _jwtEncoder.Encode(payload, null);
            var refreshToken = Guid.NewGuid();

            // TODO: Insert refresh token into database

            return Task.FromResult(new SphynxJwtInfo
            {
                Jwt = encodedPayload,
                RefreshToken = refreshToken,
            });
        }

        public Task<SnowflakeId?> VerifyTokenAsync(SphynxJwtInfo jwt, CancellationToken cancellationToken = default)
        {
            var startTime = DateTimeOffset.UtcNow;

            if (_jwtDecoder.TryDecode(jwt.Jwt, out SphynxJwtPayload payload) != DecodeResult.Success)
                return Task.FromResult<SnowflakeId?>(null);

            Debug.Assert(payload.ExpiresAt >= startTime);

            return Task.FromResult<SnowflakeId?>(payload.Subject);
        }
    }
}
