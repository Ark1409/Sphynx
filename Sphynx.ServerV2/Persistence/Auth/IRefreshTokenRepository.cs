// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.ServerV2.Auth;

namespace Sphynx.ServerV2.Persistence.Auth
{
    public interface IRefreshTokenRepository
    {
        Task<SphynxErrorCode> InsertAsync(SphynxRefreshTokenInfo refreshToken, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<SphynxRefreshTokenInfo?>> GetAsync(Guid refreshToken, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<SphynxRefreshTokenInfo?>> DeleteAsync(Guid refreshToken, CancellationToken cancellationToken = default);
    }
}
