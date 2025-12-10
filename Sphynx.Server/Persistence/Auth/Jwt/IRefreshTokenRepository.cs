// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.Server.Auth.Jwt;
using Sphynx.Server.Auth;

namespace Sphynx.Server.Persistence.Auth.Jwt
{
    public interface IRefreshTokenRepository
    {
        Task<SphynxErrorInfo> InsertAsync(SphynxRefreshTokenInfo refreshToken, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<SphynxRefreshTokenInfo?>> GetAsync(Guid refreshToken, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<bool>> ExistsAsync(Guid refreshToken, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<SphynxRefreshTokenInfo?>> DeleteAsync(Guid refreshToken, CancellationToken cancellationToken = default);
    }
}
