// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using LitJWT;
using LitJWT.Algorithms;
using Sphynx.Core;
using Sphynx.ServerV2.Auth;
using Sphynx.ServerV2.Persistence.Auth;

namespace Sphynx.ServerV2.Infrastructure.Services
{
    public class JwtService : IJwtService
    {
        private readonly JwtEncoder _jwtEncoder;
        private readonly JwtDecoder _jwtDecoder;
        private readonly JwtOptions _jwtOptions;

        private readonly IRefreshTokenRepository _refreshRepository;

        public JwtService(IRefreshTokenRepository refreshRepository) : this(refreshRepository, JwtOptions.Default)
        {
        }

        public JwtService(IRefreshTokenRepository refreshRepository, JwtOptions options)
        {
            if (options.Secret is null || options.Secret.Length == 0)
                options.Secret = HS256Algorithm.GenerateRandomRecommendedKey();

            var algorithm = new HS256Algorithm(options.Secret);

            _jwtEncoder = new JwtEncoder(algorithm);
            _jwtDecoder = new JwtDecoder(algorithm);
            _jwtOptions = options;

            _refreshRepository = refreshRepository;
        }

        public async Task<SphynxErrorInfo<SphynxJwtInfo?>> CreateTokenAsync(SnowflakeId userId, CancellationToken cancellationToken = default)
        {
            var now = DateTimeOffset.UtcNow;

            var payload = new SphynxJwtPayload
            {
                Issuer = _jwtOptions.Issuer,
                Audience = _jwtOptions.Audience,
                Subject = userId,
                IssuedAt = now,
                ExpiresAt = now + _jwtOptions.ExpiryTime,
            };

            string accessToken = _jwtEncoder.Encode(payload, null);
            var refreshToken = Guid.NewGuid();

            var jwtInfo = new SphynxJwtInfo
            {
                AccessToken = accessToken,
                RefreshToken = new SphynxRefreshTokenInfo
                {
                    RefreshToken = refreshToken,
                    AccessToken = accessToken,
                    User = userId,
                    ExpiryTime = now + _jwtOptions.RefreshTokenExpiryTime,
                    CreatedAt = now,
                },
                ExpiryTime = payload.ExpiresAt
            };

            var errorCode = await _refreshRepository.InsertAsync(jwtInfo.RefreshToken, cancellationToken).ConfigureAwait(false);

            if (errorCode != SphynxErrorCode.SUCCESS)
                return new SphynxErrorInfo<SphynxJwtInfo?>(errorCode.MaskServerError());

            return new SphynxErrorInfo<SphynxJwtInfo?>(jwtInfo);
        }

        public ValueTask<SphynxErrorInfo<SphynxJwtPayload?>> ReadTokenAsync(string jwt, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return ValueTask.FromCanceled<SphynxErrorInfo<SphynxJwtPayload?>>(cancellationToken);

            if (_jwtDecoder.TryDecode(jwt, out SphynxJwtPayload payload) != DecodeResult.Success)
                return ValueTask.FromResult(new SphynxErrorInfo<SphynxJwtPayload?>(SphynxErrorCode.INVALID_TOKEN));

            return ValueTask.FromResult(new SphynxErrorInfo<SphynxJwtPayload?>(payload));
        }

        public ValueTask<bool> VerifyTokenAsync(string jwt, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return ValueTask.FromCanceled<bool>(cancellationToken);

            if (_jwtDecoder.TryDecode(jwt, out SphynxJwtPayload payload) != DecodeResult.Success)
                return ValueTask.FromResult(false);

            if (payload.Issuer != _jwtOptions.Issuer || payload.Audience != _jwtOptions.Audience)
                return ValueTask.FromResult(false);

            return ValueTask.FromResult(true);
        }
    }
}
