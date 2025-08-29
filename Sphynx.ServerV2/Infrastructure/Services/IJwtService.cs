// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.ServerV2.Auth;

namespace Sphynx.ServerV2.Infrastructure.Services
{
    public interface IJwtService
    {
        Task<SphynxErrorInfo<SphynxJwtInfo?>> CreateTokenAsync(Guid userId, CancellationToken cancellationToken = default);

        SphynxErrorInfo<SphynxJwtPayload?> ReadToken(string jwt);
        Task<SphynxErrorInfo<SphynxRefreshTokenInfo?>> ReadTokenAsync(Guid refreshToken, CancellationToken cancellationToken = default);

        bool VerifyToken(string jwt);
        Task<bool> VerifyTokenAsync(Guid refreshToken, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<SphynxRefreshTokenInfo?>> DeleteTokenAsync(Guid refreshToken, CancellationToken cancellationToken = default);
    }
}
