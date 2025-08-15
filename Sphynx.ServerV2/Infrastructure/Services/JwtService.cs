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
                RefreshTokenInfo = new SphynxRefreshTokenInfo
                {
                    RefreshToken = refreshToken,
                    AccessToken = accessToken,
                    User = userId,
                    ExpiryTime = now + _jwtOptions.RefreshTokenExpiryTime,
                    CreatedAt = now,
                },
                ExpiryTime = payload.ExpiresAt
            };

            var errorInfo = await _refreshRepository.InsertAsync(jwtInfo.RefreshTokenInfo, cancellationToken).ConfigureAwait(false);

            if (errorInfo.ErrorCode != SphynxErrorCode.SUCCESS)
            {
                bool isNonSendableError = (errorInfo.ErrorCode.IsServerError() && errorInfo.ErrorCode != SphynxErrorCode.SERVER_ERROR) ||
                                          errorInfo.ErrorCode == SphynxErrorCode.INVALID_TOKEN;

                if (isNonSendableError)
                    return SphynxErrorCode.SERVER_ERROR;

                return new SphynxErrorInfo<SphynxJwtInfo?>(errorInfo.ErrorCode.MaskServerError(), errorInfo.Message);
            }

            return new SphynxErrorInfo<SphynxJwtInfo?>(jwtInfo);
        }

        public SphynxErrorInfo<SphynxJwtPayload?> ReadToken(string jwt)
        {
            var decodeResult = _jwtDecoder.TryDecode(jwt, out SphynxJwtPayload payload);

            switch (decodeResult)
            {
                case DecodeResult.Success:
                case DecodeResult.FailedVerifyExpire:
                case DecodeResult.FailedVerifySignature:
                case DecodeResult.FailedVerifyNotBefore:
                    return payload;
                default:
                    return SphynxErrorCode.INVALID_TOKEN;
            }
        }

        public async Task<SphynxErrorInfo<SphynxRefreshTokenInfo?>> ReadTokenAsync(Guid refreshToken, CancellationToken cancellationToken = default)
        {
            var refreshTokenInfo = await _refreshRepository.GetAsync(refreshToken, cancellationToken).ConfigureAwait(false);

            if (refreshTokenInfo.ErrorCode != SphynxErrorCode.SUCCESS)
            {
                if (refreshTokenInfo.ErrorCode == SphynxErrorCode.INVALID_TOKEN)
                    return refreshTokenInfo;

                return new SphynxErrorInfo<SphynxRefreshTokenInfo?>(refreshTokenInfo.ErrorCode.MaskServerError());
            }

            return refreshTokenInfo;
        }

        public bool VerifyToken(string jwt)
        {
            if (_jwtDecoder.TryDecode(jwt, out SphynxJwtPayload payload) != DecodeResult.Success)
                return false;

            if (payload.Issuer != _jwtOptions.Issuer || payload.Audience != _jwtOptions.Audience)
                return false;

            return true;
        }

        public async Task<bool> VerifyTokenAsync(Guid refreshToken, CancellationToken cancellationToken = default)
        {
            var refreshTokenInfo = await _refreshRepository.GetAsync(refreshToken, cancellationToken).ConfigureAwait(false);

            if (refreshTokenInfo.ErrorCode != SphynxErrorCode.SUCCESS)
                return false;

            if (!refreshTokenInfo.Data.HasValue)
                return false;

            var dbToken = refreshTokenInfo.Data.Value;

            return dbToken.ExpiryTime > DateTimeOffset.UtcNow;
        }

        public async Task<SphynxErrorInfo<SphynxRefreshTokenInfo?>> DeleteTokenAsync(Guid refreshToken, CancellationToken cancellationToken = default)
        {
            var deletedTokenInfo = await _refreshRepository.DeleteAsync(refreshToken, cancellationToken).ConfigureAwait(false);

            if (deletedTokenInfo.ErrorCode != SphynxErrorCode.SUCCESS)
            {
                if (deletedTokenInfo.ErrorCode == SphynxErrorCode.INVALID_TOKEN)
                    return deletedTokenInfo;

                return new SphynxErrorInfo<SphynxRefreshTokenInfo?>(deletedTokenInfo.ErrorCode.MaskServerError());
            }

            return deletedTokenInfo;
        }
    }
}
