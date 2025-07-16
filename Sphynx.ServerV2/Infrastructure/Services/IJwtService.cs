// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.ServerV2.Auth;

namespace Sphynx.ServerV2.Infrastructure.Services
{
    public interface IJwtService
    {
        Task<SphynxJwtInfo> GenerateTokenAsync(SnowflakeId userId, CancellationToken cancellationToken = default);
        Task<SnowflakeId?> VerifyTokenAsync(SphynxJwtInfo jwt, CancellationToken cancellationToken = default);
    }
}
