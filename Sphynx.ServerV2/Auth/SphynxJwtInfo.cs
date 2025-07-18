// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Sphynx.ServerV2.Auth
{
    public readonly record struct SphynxJwtInfo(string AccessToken, SphynxRefreshTokenInfo RefreshToken, DateTimeOffset ExpiryTime);
}
