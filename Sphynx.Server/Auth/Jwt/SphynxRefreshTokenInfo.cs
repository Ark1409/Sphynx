// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Server.Persistence.Auth.Jwt;

namespace Sphynx.Server.Auth.Jwt
{
    public readonly record struct SphynxRefreshTokenInfo(
        Guid RefreshToken,
        string AccessToken,
        Guid User,
        DateTimeOffset ExpiryTime,
        DateTimeOffset CreatedAt);

    internal static class SphynxRefreshTokenInfoExtensions
    {
        public static SphynxDbRefreshToken ToRecord(this SphynxRefreshTokenInfo domain)
        {
            return new SphynxDbRefreshToken
            {
                RefreshToken = domain.RefreshToken,
                AccessToken = domain.AccessToken,
                User = domain.User,
                CreatedAt = domain.CreatedAt,
                ExpiryTime = domain.ExpiryTime,
            };
        }

        public static SphynxRefreshTokenInfo ToDomain(this SphynxDbRefreshToken record)
        {
            return new SphynxRefreshTokenInfo
            {
                RefreshToken = record.RefreshToken,
                AccessToken = record.AccessToken,
                User = record.User,
                CreatedAt = record.CreatedAt,
                ExpiryTime = record.ExpiryTime,
            };
        }
    }
}
