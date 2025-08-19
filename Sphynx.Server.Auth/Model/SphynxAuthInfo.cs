// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.ServerV2.Auth;

namespace Sphynx.Server.Auth.Model
{
    public readonly record struct SphynxAuthInfo(SphynxAuthUser User, SphynxJwtInfo Jwt);
}
