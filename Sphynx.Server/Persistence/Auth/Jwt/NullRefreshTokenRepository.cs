// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.Server.Auth.Jwt;
using Sphynx.Server.Auth;

namespace Sphynx.Server.Persistence.Auth.Jwt
{
    public class NullRefreshTokenRepository : IRefreshTokenRepository
    {
        public Task<SphynxErrorInfo> InsertAsync(SphynxRefreshTokenInfo refreshToken, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new SphynxErrorInfo(SphynxErrorCode.SUCCESS));
        }

        public Task<SphynxErrorInfo<SphynxRefreshTokenInfo?>> GetAsync(Guid refreshToken, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new SphynxErrorInfo<SphynxRefreshTokenInfo?>
            {
                ErrorCode = SphynxErrorCode.SUCCESS,
                Data = new SphynxRefreshTokenInfo
                {
                    AccessToken = "null-access-token",
                    RefreshToken = refreshToken,
                    User = Guid.NewGuid(),
                    CreatedAt = DateTimeOffset.UtcNow,
                    ExpiryTime = DateTimeOffset.UtcNow,
                }
            });
        }

        public Task<SphynxErrorInfo<bool>> ExistsAsync(Guid refreshToken, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new SphynxErrorInfo<bool>(true));
        }

        public Task<SphynxErrorInfo<SphynxRefreshTokenInfo?>> DeleteAsync(Guid refreshToken, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new SphynxErrorInfo<SphynxRefreshTokenInfo?>
            {
                ErrorCode = SphynxErrorCode.SUCCESS,
                Data = new SphynxRefreshTokenInfo
                {
                    AccessToken = "null-access-token",
                    RefreshToken = refreshToken,
                    User = Guid.NewGuid(),
                    CreatedAt = DateTimeOffset.UtcNow,
                    ExpiryTime = DateTimeOffset.UtcNow,
                }
            });
        }
    }
}
