// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.ServerV2.Auth;

namespace Sphynx.ServerV2.Infrastructure.Services
{
    public interface IJwtService
    {
        Task<SphynxErrorInfo<SphynxJwtInfo?>> CreateTokenAsync(SnowflakeId userId, CancellationToken cancellationToken = default);
        ValueTask<SphynxErrorInfo<SphynxJwtPayload?>> ReadTokenAsync(string jwt, CancellationToken cancellationToken = default);
        ValueTask<bool> VerifyTokenAsync(string jwt, CancellationToken cancellationToken = default);
    }
}
